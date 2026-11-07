using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// One stroke of the iaido (patch 3): a phantom Muramasa zips from the stroke's start to its end, cutting everything on the line it
    /// has crossed (it passes through blocks, and every enemy is hit once per stroke). The projectile sits at the stroke's start for its
    /// whole life; the phantom's position is a pure function of the ticks this machine has run it.
    /// MULTIPLAYER: the owner's client spawns one per stroke in the same tick (MuramasaIaidoProjectile.Launch), so ordinary projectile sync
    /// carries it: Center = the stroke's start, ai[0..1] = the stroke's end offset, ai[2] = the delay in ticks before it starts (which also
    /// gives the stroke number for the rising pitch). Damage is dealt by the owner's client only; every other machine shows the slash and
    /// spawns the hit sparks itself (RemoteHitTracker). No custom packets.
    /// </summary>
    public class MuramasaSlashProjectile : ModProjectile
    {
        // The vanilla item sprite is the phantom's sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

        Vector2 Offset => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        int Delay => (int)Projectile.ai[2];
        int Index => (int)Math.Round((Delay - MuramasaTuning.IaidoDrawTicks) / (float)Math.Max(1, MuramasaTuning.SlashGapTicks));
        int Travel => TravelTicks(Offset.Length());
        int Local => ticks - Delay;
        bool Damaging => Local >= 0 && Local < Travel;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        Vector2 start;
        Vector2 head;
        Vector2 previousHead;
        AfterimageTrail afterimages;
        readonly RemoteHitTracker remote = new RemoteHitTracker();
        readonly HitMarkSet marks = new HitMarkSet();   // the blue streaks across hit enemies (shared with the Katana)
        readonly Vector2[] linePoints = new Vector2[14];
        readonly float[] lineTs = new float[14];

        // How many ticks the phantom needs for a stroke of this length.
        public static int TravelTicks(float length)
        {
            int ticks = (int)Math.Ceiling(length / Math.Max(1f, MuramasaTuning.SlashSpeed));
            return Math.Max(1, Math.Clamp(ticks, MuramasaTuning.SlashMinTravelTicks, Math.Max(MuramasaTuning.SlashMinTravelTicks, MuramasaTuning.SlashMaxTravelTicks)));
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // passes through blocks
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per stroke
        }

        // 0..1 along the stroke: fast at the start, decelerating toward the end.
        float Progress(int tick)
        {
            float t = MathHelper.Clamp(tick / (float)Math.Max(1, Travel), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, MuramasaTuning.SlashEasePower));
        }

        public override void AI()
        {
            if (!Main.player[Projectile.owner].active)
            {
                Projectile.Kill();
                return;
            }

            if (ticks == 0)
            {
                start = Projectile.Center;
                head = start;
                previousHead = start;
            }

            int local = Local;
            if (local >= 0)
            {
                afterimages?.Tick();
                if (local == 0)
                    Begin();

                if (local < Travel)
                {
                    previousHead = head;
                    head = start + Offset * Progress(local + 1);
                    MuramasaFx.SlashSweepDust(previousHead, head);
                    Lighting.AddLight(head, 0.3f, 0.55f, 1f);
                    float angle = Offset.ToRotation();
                    float scale = MuramasaTuning.SwordScale;
                    Vector2 hilt = head - angle.ToRotationVector2() * MuramasaTuning.BladeLength * scale * 0.5f; // the sword is centered on the head
                    afterimages?.Add(hilt, angle, scale, MuramasaTuning.BlueColor);
                }
                if (local == Travel - 1)
                    MuramasaFx.SlashEnd(head, Offset);

                if (!IsOwner && Damaging)
                    remote.Update(Projectile, 0, npc =>
                    {
                        MuramasaFx.HitSpark(npc.Center);
                        CameraShake.Punch(npc.Center, MuramasaTuning.SlashHitShake, 8);
                        AddMarks(npc);
                    });

                if (local >= Travel + Math.Max(1, MuramasaTuning.SlashLingerTicks))
                {
                    Projectile.Kill();
                    return;
                }
            }
            marks.Tick(MuramasaTuning.HitMarks);
            ticks++;
        }

        void Begin()
        {
            MuramasaFx.PlaySlash(start, Index);
            MuramasaFx.SlashStart(start, Offset);
            CameraShake.Punch(start, MuramasaTuning.SlashShake, 6);
            if (Main.dedServ)
                return;
            afterimages = new AfterimageTrail(MuramasaTuning.SlashAfterimageTicks, (image, age) =>
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                HeldSwordPose.Sprite(texture, 1, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
                Main.EntitySpriteDraw(texture, image.Position - Main.screenPosition, null, image.Color * (1f - age), image.Rotation + rotationOffset, origin, image.Scale, effects, 0);
            });
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        // Everything on the line from the stroke's start to where the phantom is now.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, head, MuramasaTuning.SlashWidth, ref point);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Offset.X >= 0f ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            MuramasaFx.HitSpark(target.Center);
            CameraShake.Punch(target.Center, MuramasaTuning.SlashHitShake, 8);
            AddMarks(target);
        }

        // Blue streaks across the enemy at about 90 degrees to the stroke; they grow with the enemy's hitbox (HitMarkSet).
        void AddMarks(NPC npc)
        {
            if (Main.dedServ)
                return;
            float angle = marks.Add(MuramasaTuning.HitMarks, npc.Center, Offset, npc.Hitbox);
            MuramasaFx.HitMark(npc.Center, angle);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            afterimages?.Draw();
            int local = Local;
            if (local < 0)
                return false;

            // The slash line: thick and bright at the phantom, thinner toward the start; after the arrival the whole line thins and fades.
            float grid = MuramasaTuning.PixelSize;
            float linger = Math.Max(1, MuramasaTuning.SlashLingerTicks);
            float fade = local < Travel ? 1f : MathHelper.Clamp(1f - (local - Travel + 1) / linger, 0f, 1f);
            fade *= fade;
            float width = MuramasaTuning.SlashLineWidth * (0.3f + 0.7f * fade);
            int count = linePoints.Length;
            for (int i = 0; i < count; i++)
            {
                lineTs[i] = i / (float)(count - 1);
                linePoints[i] = Vector2.Lerp(start, head, lineTs[i]);
            }
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * 2f * (0.3f + 0.7f * t), t => MuramasaTuning.DeepColor * (0.5f * fade * (0.3f + 0.7f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * (0.3f + 0.7f * t), t => MuramasaTuning.BlueColor * (fade * (0.4f + 0.6f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => Math.Max(grid, width * 0.4f * (0.3f + 0.7f * t)), t => MuramasaTuning.CoreColor * (fade * (0.5f + 0.5f * t)));
            marks.Draw(MuramasaTuning.HitMarks);

            // The phantom sword, centered on the head, only while it travels.
            if (local < Travel)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                HeldSwordPose.Sprite(texture, 1, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
                float angle = Offset.ToRotation();
                float scale = MuramasaTuning.SwordScale;
                Vector2 hilt = head - angle.ToRotationVector2() * MuramasaTuning.BladeLength * scale * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, hilt, null, MuramasaTuning.DeepColor * 0.6f, angle + rotationOffset, origin, scale * 1.2f, effects, 0);
                Main.EntitySpriteDraw(texture, hilt, null, MuramasaTuning.BlueColor * 0.7f, angle + rotationOffset, origin, scale * 1.08f, effects, 0);
                Main.EntitySpriteDraw(texture, hilt, null, Color.White, angle + rotationOffset, origin, scale, effects, 0);
                Main.EntitySpriteDraw(texture, hilt, null, MuramasaTuning.CoreColor * 0.8f, angle + rotationOffset, origin, scale, effects, 0);
            }
            return false;
        }
    }
}