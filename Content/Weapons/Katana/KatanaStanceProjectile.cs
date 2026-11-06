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
    /// MULTIPLAYER: the owner's client spawns it; ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle.
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

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        bool clicked;
        float clickFlash;      // 1 right after the click, fades to 0
        float armLocal;        // facing-local arm angle (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;

        float Progress => MathHelper.Clamp(ticks / (float)Math.Max(1, KatanaTuning.SheathEaseTicks), 0f, 1f);
        bool Settled => ticks >= KatanaTuning.SheathEaseTicks;

        public static void Spawn(IEntitySource source, Player owner, float startArmLocal, float startTilt)
        {
            Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero, ModContent.ProjectileType<KatanaStanceProjectile>(), 0, 0f, owner.whoAmI, startArmLocal, startTilt);
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

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // The sword itself, brightened a little (the click flares it white).
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.2f + 0.6f * clickFlash), rotation, origin, scale, effects, 0);
            if (clickFlash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, KatanaTuning.CoreColor * (0.8f * clickFlash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}