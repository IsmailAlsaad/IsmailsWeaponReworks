using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The cut of the Katana: a straight line from where the player stood when the dash began to where he is, growing as he flies. It is
    /// spawned together with the dash and hits everything on the line, every enemy once, through blocks, for as long as the dash lasts
    /// and SlashHitTicks more; then the drawn line fades out. The projectile sits at the line's start for its whole life.
    /// MULTIPLAYER: the owner's client spawns it (KatanaBladeProjectile.Release): Center = the line's start, ai[0..1] = the offset to
    /// where the dash was aimed, ai[2] = how many ticks the dash lasts. The line's end is the owner's synced position on every machine,
    /// so ordinary projectile sync plus vanilla player sync carry it. Damage is dealt by the owner's client only; every other machine
    /// shows the line and spawns the hit sparks itself. Patch 2 adds, all computed from that same synced data on every machine with no
    /// packets: copies of the player left along the dash (AfterimageTrail + Main.PlayerRenderer), hit-stop on every enemy the line
    /// touches (KatanaHitStopNPC, applied by every machine including the SERVER from its own hit test, so the authoritative copy of the
    /// enemy freezes too) and hit marks (a streak across each enemy, about 90 degrees to the cut with some random variation).
    /// </summary>
    public class KatanaSlashProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (never drawn, but a projectile needs a texture).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;

        Vector2 Start => Projectile.Center;
        Vector2 Offset => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        int DashTicks => Math.Max(1, (int)Projectile.ai[2]);
        int HitEnd => DashTicks + KatanaTuning.SlashHitTicks;
        int LifeEnd => Math.Max(HitEnd, DashTicks + Math.Max(KatanaTuning.TrailFollowTicks, Math.Max(1, KatanaTuning.SlashLingerTicks)));
        Vector2 End => tracked ? endPoint : Start + Offset;
        bool Damaging => ticks < HitEnd;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        Vector2 endPoint;
        bool tracked;
        readonly HashSet<int> seen = new HashSet<int>();      // enemies this machine has already frozen and marked
        readonly HitMarkSet marks = new HitMarkSet();         // the streaks across hit enemies (shared with the Muramasa)
        AfterimageTrail playerImages;
        Vector2 lastImagePosition;
        bool haveLastImage;
        readonly Vector2[] linePoints = new Vector2[14];
        readonly float[] lineTs = new float[14];

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
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (ticks == 0)
            {
                KatanaFx.DashStart(Start, Offset);
                CameraShake.Punch(Start, KatanaTuning.SlashShake, 8);
            }

            // The head of the line follows the player during the dash and for TrailFollowTicks after it (the tail catches up meanwhile).
            if (ticks <= DashTicks + Math.Max(0, KatanaTuning.TrailFollowTicks))
            {
                endPoint = owner.Center;
                tracked = true;
                if (ticks <= DashTicks)
                    KatanaFx.DashTrail(endPoint, Offset);
            }
            if (KatanaTuning.VanillaShadow && ticks < DashTicks + KatanaTuning.DashShadowTicks)
                owner.armorEffectDrawShadow = true; // the vanilla afterimages of the player trail behind him

            // Copies of the player along the dash (the trail ticks first, then lays this tick's copies).
            if (!Main.dedServ)
            {
                if (ticks <= DashTicks)
                    EnsurePlayerImages();
                playerImages?.Tick();
                if (ticks <= DashTicks && playerImages != null)
                    LayImages(owner);
            }
            marks.Tick(KatanaTuning.HitMarks);

            // Hit-stop (every machine, the server too) and, on the machines that do not own the cut, the hit marks and sparks.
            if (Damaging)
                ScanHits();

            if (ticks >= LifeEnd)
            {
                Projectile.Kill();
                return;
            }
            Lighting.AddLight(End, 0.5f, 0.55f, 0.7f);
            ticks++;
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        // Everything on the line.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Start, End, KatanaTuning.SlashWidth, ref point);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Offset.X >= 0f ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            AddMarks(target);
            KatanaFx.HitSpark(target.Center);
            CameraShake.Punch(target.Center, KatanaTuning.SlashHitShake, 8);
        }

        // Every machine tests the line against its own copy of the enemies once per enemy: the enemy freezes (hit-stop) and the machines that
        // do not own the cut also show the sparks and marks (the owner's client does that in OnHitNPC, on a real hit).
        void ScanHits()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || seen.Contains(i) || !npc.CanBeChasedBy(Projectile))
                    continue;
                if (Colliding(Projectile.Hitbox, npc.Hitbox) != true)
                    continue;
                seen.Add(i);
                KatanaHitStopNPC.Apply(npc, KatanaTuning.HitStopTicks);
                if (!IsOwner && !Main.dedServ)
                {
                    KatanaFx.HitSpark(npc.Center);
                    CameraShake.Punch(npc.Center, KatanaTuning.SlashHitShake, 8);
                    AddMarks(npc);
                }
            }
        }

        // Streaks across the enemy at about 90 degrees to the cut; they grow with the enemy's hitbox (HitMarkSet, shared with the Muramasa).
        void AddMarks(NPC npc)
        {
            if (Main.dedServ)
                return;
            float angle = marks.Add(KatanaTuning.HitMarks, npc.Center, End - Start, npc.Hitbox);
            KatanaFx.HitMark(npc.Center, angle);
        }

        // The copies are the real player drawn at an old position (the pose of the moment they are drawn), more see-through the older they are.
        void EnsurePlayerImages()
        {
            if (playerImages != null)
                return;
            playerImages = new AfterimageTrail(KatanaTuning.PlayerAfterimageTicks, (image, age) =>
            {
                Player player = Main.player[Projectile.owner];
                if (!player.active || player.dead)
                    return;
                float shadow = MathHelper.Lerp(MathHelper.Clamp(KatanaTuning.AfterimageShadow, 0f, 1f), 1f, age);
                if (shadow >= 0.99f)
                    return;
                Main.PlayerRenderer.DrawPlayer(Main.Camera, player, image.Position, player.fullRotation, player.fullRotationOrigin, shadow, 1f);
            });
        }

        // One copy every AfterimageSpacing pixels along the path the player flew since the last tick.
        void LayImages(Player owner)
        {
            Vector2 current = owner.position;
            if (!haveLastImage)
            {
                haveLastImage = true;
                lastImagePosition = current;
                playerImages.Add(current, 0f, 1f, Color.White);
                return;
            }
            Vector2 delta = current - lastImagePosition;
            float distance = delta.Length();
            float spacing = Math.Max(4f, KatanaTuning.AfterimageSpacing);
            int count = (int)(distance / spacing);
            for (int i = 1; i <= count; i++)
                playerImages.Add(lastImagePosition + delta * (i * spacing / distance), 0f, 1f, Color.White);
            if (count > 0)
                lastImagePosition += delta * (count * spacing / distance);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            playerImages?.Draw();

            // The slash line: thick and white at the landing end, thinner toward where he stood; after the hit ticks the whole line thins and fades.
            float grid = KatanaTuning.PixelSize;
            float linger = Math.Max(1, KatanaTuning.SlashLingerTicks);
            // After the dash the tail of the line slides toward the head (the player) while the line thins and dims; when it arrives the line is gone.
            float catchUp = MathHelper.Clamp((ticks - DashTicks) / linger, 0f, 1f);
            float fade = 1f - catchUp * catchUp;
            float width = KatanaTuning.SlashLineWidth * (0.3f + 0.7f * fade);
            Vector2 end = End;
            Vector2 start = Vector2.Lerp(Start, end, catchUp);
            int count = linePoints.Length;
            for (int i = 0; i < count; i++)
            {
                lineTs[i] = i / (float)(count - 1);
                linePoints[i] = Vector2.Lerp(start, end, lineTs[i]);
            }
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * 2f * (0.3f + 0.7f * t), t => KatanaTuning.EdgeColor * (0.5f * fade * (0.3f + 0.7f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * (0.3f + 0.7f * t), t => KatanaTuning.GlowColor * (fade * (0.4f + 0.6f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => Math.Max(grid, width * 0.4f * (0.3f + 0.7f * t)), t => KatanaTuning.CoreColor * (fade * (0.5f + 0.5f * t)));
            marks.Draw(KatanaTuning.HitMarks);
            return false;
        }


    }
}