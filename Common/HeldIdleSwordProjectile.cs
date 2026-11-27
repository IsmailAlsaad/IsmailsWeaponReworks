using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The sword held out while nothing is swinging, for weapons whose swing is its own short-lived projectile (Ice Blade, Starfury, Bee
    /// Keeper, Phaseblades). One of these lives for as long as the weapon is held (HeldIdleSwordPlayer spawns it); it poses the arm in the
    /// ready pose (HeldSwordIdle: eased, bobbing, following the walking bob) and draws the held item's sprite. While the weapon's swing
    /// projectile exists (or Hidden says so) it stays out of the way: no pose, no drawing; it remembers the blade's angle so the sword eases
    /// from where the swing left it into the ready pose. A subclass only says which item and swing it belongs to, the ready angles, the
    /// scale and how the sprite is drawn (so the idle look matches the weapon's own steady glow). It never deals damage.
    /// MULTIPLAYER: the owner's client spawns it (a normal synced projectile); every machine runs the same pose from the owner's synced
    /// direction, body frame and projectiles. Nothing is sent by hand.
    /// </summary>
    public abstract class HeldIdleSwordProjectile : ModProjectile
    {
        // A placeholder so the mod loads; the sprite drawn is the held item's.
        public override string Texture => "Terraria/Images/Item_" + ItemID.WoodenSword;

        public abstract bool Holds(int itemType);
        protected abstract int SwingType { get; }
        protected abstract float ReadyArmDeg { get; }
        protected abstract float ReadyTiltDeg { get; }
        protected abstract float SwordScale { get; }
        protected abstract void DrawBlade(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color lightColor);

        protected virtual int IdleBobTicks => 90;
        protected virtual float IdleBobDeg => 2f;
        protected virtual float IdleEase => 0.15f;
        protected virtual bool Hidden(Player owner) { return false; }
        // False while the swing projectile still exists but no longer shows the sword (its trail is still fading): the idle sword takes over then.
        protected virtual bool SwingOwnsSword(Projectile swing) { return true; }

        int ticks;
        bool hidden = true;
        bool haveBlade;
        float lastBlade;
        float armLocal;
        float tiltRad;
        float walkBob;
        float swayAmount = 1f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
        }

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        Projectile FindSwing(Player owner)
        {
            int type = SwingType;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == owner.whoAmI && other.type == type)
                    return other;
            }
            return null;
        }

        // The pose is worked out in Tick, called by HeldIdleSwordSystem after every projectile has updated.
        public override void AI()
        {
            Projectile.timeLeft = 10000;
        }

        public void Tick()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || !Holds(owner.HeldItem.type))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.scale = SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            ticks++;

            Projectile swing = FindSwing(owner);
            bool swingHasSword = swing != null && SwingOwnsSword(swing);
            if (swingHasSword || Hidden(owner))
            {
                hidden = true;
                walkBob = 0f;
                if (swingHasSword)
                {
                    lastBlade = owner.gravDir == -1f ? -swing.rotation : swing.rotation;
                    haveBlade = true;
                }
                Projectile.Center = owner.MountedCenter;
                return;
            }

            int facing = owner.direction >= 0 ? 1 : -1;
            if (hidden)
            {
                hidden = false;
                if (haveBlade)
                {
                    // Start from the blade angle the swing left (no arm/tilt split known: all of it in the arm), then ease into the ready pose.
                    armLocal = HeldSwordPose.ToWorld(facing, lastBlade);
                    tiltRad = 0f;
                }
                else
                {
                    armLocal = Rad(ReadyArmDeg);
                    tiltRad = Rad(ReadyTiltDeg);
                }
            }

            HeldSwordIdle.Step(owner, ticks, Rad(ReadyArmDeg), Rad(ReadyTiltDeg), IdleBobTicks, Rad(IdleBobDeg), IdleEase, ref armLocal, ref tiltRad, ref swayAmount);
            float worldArm = HeldSwordPose.ToWorld(facing, armLocal);
            float worldBlade = HeldSwordPose.ToWorld(facing, armLocal + tiltRad);

            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Projectile.Center = HeldSwordPose.HandWorld(owner, worldArm) + HeldSwordIdle.WalkShift(owner, ref walkBob);
            Projectile.rotation = owner.gravDir == -1f ? -worldBlade : worldBlade;
            Projectile.spriteDirection = facing;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (hidden || ticks == 0)
                return false;
            int itemType = Main.player[Projectile.owner].HeldItem.type;
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            DrawBlade(texture, Projectile.Center - Main.screenPosition, Projectile.rotation + rotationOffset, origin, Projectile.scale, effects, lightColor);
            return false;
        }
    }
}