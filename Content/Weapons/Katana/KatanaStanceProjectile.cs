using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The held Katana while it is not thrown. It draws the sword and poses the owner's arm: the arm and blade ease from where they
    /// started (the throw pose, or the ready pose when the sword is first taken out) to the sheathed pose at the hip, then the sword
    /// CLICKS shut (flash, kick, sparks, sound). There is no sheath sprite: the player simply holds the sword in the sheathed pose.
    /// MULTIPLAYER: the owner's client spawns it; ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle,
    /// ai[2] = how many ticks of the throw cooldown are left (the cooldown itself lives on the owner only). While that runs the sword is
    /// plain; when it is over the sword wears a white pulsing outline (the "ready" glow). Every machine counts the ticks itself.
    /// The pose is a pure function of the ticks this machine has run it, so every machine runs the same animation and click without
    /// any packet. Each machine ends it on its own when the owner dies, swaps items or throws the blade.
    /// </summary>
    public class KatanaStanceProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;

        Player Owner => Main.player[Projectile.owner];
        int Facing => Owner.direction >= 0 ? 1 : -1;
        float StartArm => MathHelper.WrapAngle(Projectile.ai[0]);
        float StartTilt => Projectile.ai[1];
        int CooldownLeft => Math.Max(0, (int)Projectile.ai[2]);

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        bool clicked;
        float clickFlash;      // 1 right after the click, fades to 0
        bool readyAnnounced;   // the cooldown ran out (or there was none)
        float readyShown;      // 0..1, how visible the white ready glow is
        float readyFlash;      // 1 right after the sword is ready again, fades to 0
        float armLocal;        // facing-local arm angle (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;

        float Progress => MathHelper.Clamp(ticks / (float)Math.Max(1, KatanaTuning.SheathEaseTicks), 0f, 1f);
        bool Settled => ticks >= KatanaTuning.SheathEaseTicks;

        public static void Spawn(IEntitySource source, Player owner, float startArmLocal, float startTilt, int cooldownTicks)
        {
            Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero, ModContent.ProjectileType<KatanaStanceProjectile>(), 0, 0f, owner.whoAmI, startArmLocal, startTilt, Math.Max(0, cooldownTicks));
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // it never hurts anything
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
        }

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, when they get crowd-controlled, or when a thrown blade takes over.
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Katana || KatanaPlayer.BladeFlying(Projectile.owner))
            {
                Projectile.Kill();
                return;
            }

            Projectile.scale = KatanaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (!clicked && Settled)
            {
                clicked = true;
                clickFlash = 1f;
            }

            // The settling: a smoothstep from the start pose to the sheathed pose (the arm takes the short way round).
            float eased = MathHelper.SmoothStep(0f, 1f, Progress);
            armLocal = StartArm + MathHelper.WrapAngle(Rad(KatanaTuning.SheathArmDeg) - StartArm) * eased;
            tiltRad = StartTilt + (Rad(KatanaTuning.SheathTiltDeg) - StartTilt) * eased;
            armLocal -= Rad(KatanaTuning.ClickKickDeg) * clickFlash; // the hand kicks when the sword clicks shut

            ApplyPose(owner);

            if (clickFlash >= 1f)
            {
                KatanaFx.Click(Projectile.Center, tipPosition);
                CameraShake.Punch(Projectile.Center, KatanaTuning.ClickShake, 8);
            }
            clickFlash = Math.Max(0f, clickFlash - 1f / Math.Max(1, KatanaTuning.ClickFlashTicks));

            // The cooldown counted on this machine from the spawn: when it is over the sword glows white (a chime only if it had to wait).
            bool ready = clicked && ticks >= CooldownLeft; // only once the sword has clicked shut (fully sheathed): no glow, chime or flash before
            if (ready && !readyAnnounced)
            {
                readyAnnounced = true;
                if (CooldownLeft > 0)
                {
                    readyFlash = 1f;
                    KatanaFx.Ready(Projectile.Center, tipPosition);
                }
            }
            readyShown += ((ready ? 1f : 0f) - readyShown) * MathHelper.Clamp(KatanaTuning.ReadyFade, 0.01f, 1f);
            if (readyShown > 0.05f)
            {
                if (Main.rand.NextFloat() < KatanaTuning.ReadySparkleChance * readyShown)
                    KatanaFx.ReadySparkle(Vector2.Lerp(Projectile.Center, tipPosition, Main.rand.NextFloat()));
                Lighting.AddLight(tipPosition, 0.35f * readyShown, 0.4f * readyShown, 0.5f * readyShown);
            }
            readyFlash = Math.Max(0f, readyFlash - 1f / Math.Max(1, KatanaTuning.ReadyFlashTicks));
            ticks++;
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            float worldArm = HeldSwordPose.ToWorld(Facing, armLocal);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = HeldSwordPose.ToWorld(Facing, armLocal + tiltRad);
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * KatanaTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override bool? CanDamage()
        {
            return false;
        }

        readonly Vector2[] OutlineDirections =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // The ready glow: a soft white copy behind the sword (drawn around the CENTER of the blade sprite, so it swells outward evenly)
            // and the sprite repeated in the eight directions around it, a pixel-style outline. Both pulse; the colors have alpha 0 = additive.
            if (readyShown > 0.01f)
            {
                float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * ticks / Math.Max(1, KatanaTuning.ReadyPulseTicks));
                float strength = readyShown * MathHelper.Lerp(KatanaTuning.ReadyOutlineMin, KatanaTuning.ReadyOutlineMax, wave);

                Vector2 hiltToCenter = new Vector2(Projectile.spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
                Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
                Vector2 centerOrigin = texture.Size() * 0.5f;
                Main.EntitySpriteDraw(texture, centerPosition, null, KatanaTuning.ReadyOutlineColor * (0.4f * strength), rotation, centerOrigin, scale * KatanaTuning.ReadyGlowScale, effects, 0);

                float width = KatanaTuning.ReadyOutlineWidth;
                for (int i = 0; i < OutlineDirections.Length; i++)
                    Main.EntitySpriteDraw(texture, position + OutlineDirections[i] * width, null, KatanaTuning.ReadyOutlineColor * (0.9f * strength), rotation, origin, scale, effects, 0);
                for (int i = 0; i < 4; i++)
                    Main.EntitySpriteDraw(texture, position + OutlineDirections[i] * (width * 0.5f), null, KatanaTuning.ReadyCoreColor * (0.5f * strength), rotation, origin, scale, effects, 0);
            }

            // The sword itself, brightened a little (the click flares it white, and so does the moment it is ready again).
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.2f + 0.6f * Math.Max(clickFlash, readyFlash)), rotation, origin, scale, effects, 0);
            if (clickFlash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, KatanaTuning.CoreColor * (0.8f * clickFlash), rotation, origin, scale, effects, 0);
            if (readyFlash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, KatanaTuning.ReadyCoreColor * (0.8f * readyFlash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}