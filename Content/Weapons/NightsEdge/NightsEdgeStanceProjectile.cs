using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The held Night's Edge while it is not swinging: the player holds the sword out in an idle pose (the hand in front of the body, the
    /// blade pointing up and forward) with a slow breathing bob, like the Katana's and the Muramasa's stances. It ends the moment a swing
    /// starts (the swing draws the sword itself) and the owner's NightsEdgePlayer starts a new one when the swing is over, easing from the
    /// pose the swing ended in. A fully charged sword keeps its pulsing outline and its edge flames in the stance for as long as the charge
    /// lasts (ChargedLingerTicks after the last swing), so the charge can be seen between swings.
    /// MULTIPLAYER: the owner's client spawns it (a normal projectile): ai[0] = the arm's start angle and ai[1] = the blade's start angle
    /// (WORLD angles, where the swing ended), ai[2] = how many ticks of charge are left. The pose is a pure function of the ticks this machine
    /// has run it and the owner's facing, so every machine runs the same animation without any packet. Each machine ends it on its own when
    /// the owner dies, swaps items, is crowd-controlled or a swing exists. Nothing is saved.
    /// </summary>
    public class NightsEdgeStanceProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.NightsEdge;

        Player Owner => Main.player[Projectile.owner];
        int Facing => Owner.direction >= 0 ? 1 : -1;
        float StartArm => Projectile.ai[0];
        float StartBlade => Projectile.ai[1];
        int ChargeTicksAtSpawn => Math.Max(0, (int)Projectile.ai[2]);

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        float armWorld;
        float bladeWorld;
        Vector2 tipPosition;
        float chargedShown;           // 0..1, the outline's and the flames' strength
        NightsEdgeFlameRig flames;    // created on first use (projectile instances are copies of one prototype)

        // Where the arm and the blade are after 'time' ticks of this stance, for the owner's current facing (world angles).
        public float WorldArmNow => PoseAt(ticks).X;
        public float WorldBladeNow => PoseAt(ticks).Y;

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        public static float IdleWorldArm(int facing)
        {
            return HeldSwordPose.ToWorld(facing, Rad(NightsEdgeTuning.IdleArmDeg));
        }

        public static float IdleWorldBlade(int facing)
        {
            return HeldSwordPose.ToWorld(facing, Rad(NightsEdgeTuning.IdleArmDeg + NightsEdgeTuning.IdleTiltDeg));
        }

        public static void Spawn(IEntitySource source, Player owner, float startWorldArm, float startWorldBlade, int chargeTicksLeft)
        {
            Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero, ModContent.ProjectileType<NightsEdgeStanceProjectile>(), 0, 0f, owner.whoAmI, startWorldArm, startWorldBlade, Math.Max(0, chargeTicksLeft));
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

        // X = the arm, Y = the blade (world angles): eased from where the swing ended to the idle pose, with a small breathing bob.
        Vector2 PoseAt(int time)
        {
            float eased = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(time / (float)Math.Max(1, NightsEdgeTuning.IdleEaseTicks), 0f, 1f));
            float bob = Rad(NightsEdgeTuning.IdleBobDeg) * (float)Math.Sin(MathHelper.TwoPi * (Main.GameUpdateCount % (uint)Math.Max(1, NightsEdgeTuning.IdleBobTicks)) / Math.Max(1, NightsEdgeTuning.IdleBobTicks));
            float targetArm = HeldSwordPose.ToWorld(Facing, Rad(NightsEdgeTuning.IdleArmDeg) + bob);
            float targetBlade = HeldSwordPose.ToWorld(Facing, Rad(NightsEdgeTuning.IdleArmDeg + NightsEdgeTuning.IdleTiltDeg) + bob * 0.5f);
            return new Vector2(
                StartArm + MathHelper.WrapAngle(targetArm - StartArm) * eased,
                StartBlade + MathHelper.WrapAngle(targetBlade - StartBlade) * eased);
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, when they get crowd-controlled, or when a swing takes over.
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.NightsEdge
                || NightsEdgePlayer.Find(Projectile.owner, ModContent.ProjectileType<NightsEdgeSwingProjectile>()) != null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.scale = NightsEdgeTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            Vector2 pose = PoseAt(ticks);
            armWorld = pose.X;
            bladeWorld = pose.Y;
            ApplyPose(owner);

            // The charge the sword had when it was put down lasts a while (counted on every machine from the spawn), then fades.
            float target = ticks < ChargeTicksAtSpawn ? 1f : 0f;
            chargedShown += (target - chargedShown) * MathHelper.Clamp(NightsEdgeTuning.OutlineFade, 0.01f, 1f);
            if (chargedShown < 0.01f)
                chargedShown = 0f;

            if (!Main.dedServ)
            {
                flames ??= new NightsEdgeFlameRig();
                flames.Tick(Projectile, false, chargedShown, tipPosition);
                if (chargedShown > 0.3f && Main.rand.NextFloat() < 0.12f * chargedShown)
                    NightsEdgeFx.ChargeSparkle(Projectile.Center, tipPosition);
            }
            ticks++;
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armWorld - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, armWorld);
            float blade = bladeWorld;
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * NightsEdgeTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // One copy of the sword sprite (the same as the swing's, never mirrored here): centerScale above 0 grows the copy around the middle.
        void DrawSword(Texture2D texture, float scale, Color color, float centerScale = 0f, Vector2 offset = default)
        {
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float rotation = Projectile.rotation + rotationOffset;
            Vector2 position = Projectile.Center + offset + HeldSwordPose.GripShift(Projectile.rotation, NightsEdgeTuning.GripOffset, Projectile.scale);
            if (centerScale > 0f && centerScale != scale)
            {
                Vector2 toCenter = texture.Size() * 0.5f - origin;
                position += toCenter.RotatedBy(rotation) * (centerScale - scale);
            }
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;
            // A swing that has just started draws the sword itself.
            if (NightsEdgePlayer.Find(Projectile.owner, ModContent.ProjectileType<NightsEdgeSwingProjectile>()) != null)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float scale = Projectile.scale;

            if (chargedShown > 0.01f)
            {
                float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * (Main.GameUpdateCount % (uint)Math.Max(1, NightsEdgeTuning.OutlinePulseTicks)) / Math.Max(1, NightsEdgeTuning.OutlinePulseTicks));
                float strength = chargedShown * MathHelper.Lerp(NightsEdgeTuning.OutlineMin, NightsEdgeTuning.OutlineMax, wave);
                SpriteOutline.Draw(strength, NightsEdgeTuning.OutlineWidth, NightsEdgeTuning.OutlineGlowScale, scale, NightsEdgeTuning.OutlineColor, NightsEdgeTuning.OutlineCoreColor,
                    (color, copyScale, centerScale, offset) => DrawSword(texture, copyScale, color, centerScale, offset));
            }

            DrawSword(texture, scale, Color.Lerp(lightColor, Color.White, 0.25f));

            // The edge flames, in front of the sword.
            MetaballLayers.Of<NightsEdgeEdgeFlameStyle>(Projectile.owner).DrawInBatch();
            return false;
        }
    }
}