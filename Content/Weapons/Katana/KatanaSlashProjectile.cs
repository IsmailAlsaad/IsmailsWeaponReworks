using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The cut of the Katana: a straight line from where the player stood when he let go to where he landed. It is spawned right after
    /// the teleport, so everything on the line is hit after he arrived: every enemy once, through blocks, for SlashHitTicks ticks; then
    /// the drawn line fades out. The projectile sits at the line's start for its whole life; the end is start + offset.
    /// MULTIPLAYER: the owner's client spawns it (KatanaBladeProjectile.Release): Center = the line's start, ai[0..1] = the offset to the
    /// end, so ordinary projectile sync carries it. Damage is dealt by the owner's client only; every other machine shows the line and
    /// spawns the hit sparks itself (RemoteHitTracker). No custom packets.
    /// </summary>
    public class KatanaSlashProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (never drawn, but a projectile needs a texture).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;

        Vector2 Start => Projectile.Center;
        Vector2 Offset => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        Vector2 End => Projectile.Center + Offset;
        bool Damaging => ticks < KatanaTuning.SlashHitTicks;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        readonly RemoteHitTracker remote = new RemoteHitTracker();
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
            if (ticks == 0)
            {
                KatanaFx.Teleport(Start, End);
                KatanaFx.SlashLine(Start, End);
                CameraShake.Punch(End, KatanaTuning.SlashShake, 8);
            }

            if (!IsOwner && Damaging)
                remote.Update(Projectile, 0, npc =>
                {
                    KatanaFx.HitSpark(npc.Center);
                    CameraShake.Punch(npc.Center, KatanaTuning.SlashHitShake, 8);
                });

            if (ticks >= KatanaTuning.SlashHitTicks + Math.Max(1, KatanaTuning.SlashLingerTicks))
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
            KatanaFx.HitSpark(target.Center);
            CameraShake.Punch(target.Center, KatanaTuning.SlashHitShake, 8);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            // The slash line: thick and white at the landing end, thinner toward where he stood; after the hit ticks the whole line thins and fades.
            float grid = KatanaTuning.PixelSize;
            float linger = Math.Max(1, KatanaTuning.SlashLingerTicks);
            float fade = Damaging ? 1f : MathHelper.Clamp(1f - (ticks - KatanaTuning.SlashHitTicks + 1) / linger, 0f, 1f);
            fade *= fade;
            float width = KatanaTuning.SlashLineWidth * (0.3f + 0.7f * fade);
            Vector2 start = Start;
            Vector2 end = End;
            int count = linePoints.Length;
            for (int i = 0; i < count; i++)
            {
                lineTs[i] = i / (float)(count - 1);
                linePoints[i] = Vector2.Lerp(start, end, lineTs[i]);
            }
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * 2f * (0.3f + 0.7f * t), t => KatanaTuning.EdgeColor * (0.5f * fade * (0.3f + 0.7f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => width * (0.3f + 0.7f * t), t => KatanaTuning.GlowColor * (fade * (0.4f + 0.6f * t)));
            PixelLine.Draw(linePoints, lineTs, count, grid, t => Math.Max(grid, width * 0.4f * (0.3f + 0.7f * t)), t => KatanaTuning.CoreColor * (fade * (0.5f + 0.5f * t)));
            return false;
        }
    }
}