using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// The right click swing of the Blade of Grass: the same drive animation as the Phaseblade's plant (the arm lifts with the blade pointing up, comes
    /// down accelerating while the blade rotates to point down, drives it into the ground in front of the player, then settles back and the sword comes
    /// out of the ground again), but ONLY on the ground (there is no airborne plant, nothing is shot downward) and the sword does not stay: at the impact
    /// tick the vines the player has grown are planted there (BladeOfGrassPlantedVinesProjectile) and the player starts a new set.
    /// While it runs it replaces the held blade: it ends the held BladeOfGrassBladeProjectile at once, BladeOfGrassPlayer does not spawn a new one until it is
    /// over, so it alone poses the arm and draws the sword. Its last pose is the held blade's ready pose, so the next held blade continues without a jump.
    /// MULTIPLAYER: the owner's client decides (the press, the ground). The spawn packet carries only the facing (ai[0] = +1 / -1); every machine then runs the
    /// same timers for the pose, the sounds and the dust on its own copy, and ends its own copy of the held blade. The owner's client alone spawns the planted
    /// vines at the impact tick (normal spawn sync). Never damages. No custom packets.
    /// </summary>
    public class BladeOfGrassPlantSwingProjectile : ModProjectile
    {
        // The swing draws the item sprite; this is only a placeholder.
        public override string Texture => "Terraria/Images/Item_" + ItemID.BladeofGrass;

        int Facing => Projectile.ai[0] >= 0f ? 1 : -1;
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        int ticks;
        float timer;
        float armLocal;        // facing-local arm angle (positive = down)
        float tiltRad;         // blade angle relative to the arm; arm + tilt = the blade's facing-local angle
        float plantStabArm;    // the arm angle at the end of the drive, solved from PlantReachPx when the swing starts
        Vector2 tipPosition;
        BladeOfGrassVineSet vines; // the player's vines, taken over from the held blade; they ungrow while the swing runs up to the impact

        int ImpactTick => BladeOfGrassTuning.PlantLiftTicks + BladeOfGrassTuning.PlantStabTicks;
        int TotalTicks => ImpactTick + BladeOfGrassTuning.PlantRecoverTicks;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // it never damages
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // ---------------------------------------------------------------- start (owner) and helpers

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        // True while this player's plant swing runs (every machine).
        public static bool Active(int owner)
        {
            int type = ModContent.ProjectileType<BladeOfGrassPlantSwingProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == owner && other.type == type)
                    return true;
            }
            return false;
        }

        // Standing: the ground below x, from a little above the feet down PlantGroundedDropPx below them.
        static bool FindGround(Player player, float x, out Vector2 ground)
        {
            return TileProbe.FindTileBelow(x, player.Bottom.Y - 10f, 10f + BladeOfGrassTuning.PlantGroundedDropPx, out ground);
        }

        // Owner only: starts the swing toward the cursor's side. Nothing happens in the air, with upside-down gravity or while a swing already runs.
        public static bool TryStart(Player player, IEntitySource source, int damage, float knockback)
        {
            if (player.gravDir != 1f || Active(player.whoAmI))
                return false;
            int side = Main.MouseWorld.X >= player.Center.X ? 1 : -1;
            if (!FindGround(player, player.Center.X + side * BladeOfGrassTuning.PlantReachPx, out _))
                return false;
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<BladeOfGrassPlantSwingProjectile>(), damage, knockback, player.whoAmI, side);
            return true;
        }

        // ---------------------------------------------------------------- the pose (the Phaseblade's plant, with the Blade of Grass's ready pose at the end)

        float RestArm => Rad(BladeOfGrassTuning.ReadyArmDeg);
        float RestBlade => Rad(BladeOfGrassTuning.ReadyArmDeg + BladeOfGrassTuning.ReadyTiltDeg);

        // The arm: it lifts with a cubic ease-out, comes down accelerating (the drive), then settles to the ready pose.
        float ArmAt(float time)
        {
            float lift = Math.Max(1, BladeOfGrassTuning.PlantLiftTicks);
            float drive = Math.Max(1, BladeOfGrassTuning.PlantStabTicks);
            float raised = Rad(BladeOfGrassTuning.PlantRaisedArmDeg);
            if (time < lift)
            {
                float k = MathHelper.Clamp(time / lift, 0f, 1f);
                return MathHelper.Lerp(RestArm, raised, 1f - (float)Math.Pow(1f - k, 3));
            }
            if (time < ImpactTick)
            {
                float k = MathHelper.Clamp((time - lift) / drive, 0f, 1f);
                return MathHelper.Lerp(raised, plantStabArm, k * k);
            }
            float settle = MathHelper.SmoothStep(0f, 1f, (time - ImpactTick) / Math.Max(1, BladeOfGrassTuning.PlantRecoverTicks));
            return MathHelper.Lerp(plantStabArm, RestArm, settle);
        }

        // The blade's own facing-local angle: point-up while the arm lifts, rotating to point-down during the drive, then back to the ready angle.
        float BladeAt(float time)
        {
            float lift = Math.Max(1, BladeOfGrassTuning.PlantLiftTicks);
            float drive = Math.Max(1, BladeOfGrassTuning.PlantStabTicks);
            if (time < lift)
                return -MathHelper.PiOver2;
            if (time < ImpactTick)
            {
                float k = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((time - lift) / drive / 0.9f, 0f, 1f));
                return MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, k);
            }
            float settle = MathHelper.SmoothStep(0f, 1f, (time - ImpactTick) / Math.Max(1, BladeOfGrassTuning.PlantRecoverTicks));
            return MathHelper.Lerp(MathHelper.PiOver2, RestBlade, settle);
        }

        // The arm angle at the end of the drive: the one whose hand is PlantReachPx in front of the player (the blade hangs straight down from it).
        float SolveStabArm()
        {
            float best = Rad(BladeOfGrassTuning.PlantRaisedArmDeg);
            float bestError = float.MaxValue;
            for (float degrees = BladeOfGrassTuning.PlantRaisedArmDeg; degrees <= 60f; degrees += 1f)
            {
                Vector2 hand = HeldSwordPose.Hand(Owner, Facing, Rad(degrees));
                float error = Math.Abs((hand.X - Owner.Center.X) * Facing - BladeOfGrassTuning.PlantReachPx);
                if (error < bestError)
                {
                    bestError = error;
                    best = Rad(degrees);
                }
            }
            return best;
        }

        // ---------------------------------------------------------------- AI

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || !ReworkToggle.Holds(owner, ItemID.BladeofGrass))
            {
                Projectile.Kill();
                return;
            }
            if (timer >= TotalTicks)
            {
                Projectile.Kill();
                return;
            }

            // The player faces the way the sword is driven in for the whole swing: the cursor and the keys cannot turn him around.
            if (IsOwner)
                owner.ChangeDir(Facing);
            else
                owner.direction = Facing;
            if (ticks == 0)
            {
                plantStabArm = SolveStabArm();
                BladeOfGrassFx.PlantStart(owner.MountedCenter);
            }
            EndHeldBlade(owner);
            TickVines(owner);

            Projectile.scale = BladeOfGrassTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            armLocal = ArmAt(timer);
            tiltRad = BladeAt(timer) - armLocal; // arm + tilt = the blade's facing-local angle
            ApplyPose(owner);

            if ((int)timer < ImpactTick)
                Lighting.AddLight(tipPosition, 0.15f, 0.4f, 0.1f);
            if ((int)timer == ImpactTick)
                Impact(owner);

            ticks++;
            timer++;
        }

        // The held blade is gone for as long as the swing runs (every machine ends its own copy; BladeOfGrassPlayer does not make a new one meanwhile).
        void EndHeldBlade(Player owner)
        {
            int type = ModContent.ProjectileType<BladeOfGrassBladeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == owner.whoAmI && other.type == type)
                {
                    if (vines == null && other.ModProjectile is BladeOfGrassBladeProjectile held)
                        vines = held.TakeVines();
                    other.Kill();
                }
            }
        }

        // The sword hits the ground: the owner plants the vines there. Without ground (the player left it during the swing) nothing is planted and the vines stay with the player.
        void Impact(Player owner)
        {
            if (!FindGround(owner, owner.Center.X + Facing * BladeOfGrassTuning.PlantReachPx, out Vector2 ground))
                return;
            BladeOfGrassFx.PlantImpact(ground);
            CameraShake.Punch(ground, BladeOfGrassTuning.PlantShake, 10);
            if (IsOwner)
                BladeOfGrassPlantedVinesProjectile.Plant(owner, ground, Projectile.GetSource_FromThis(), Projectile.damage, Projectile.knockBack);
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
            tipPosition = hand + blade.ToRotationVector2() * BladeOfGrassTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---------------------------------------------------------------- drawing

        // The player's vines do not vanish: from the start of the swing up to the impact they ungrow (shrink toward the player and brown); the planted ones then come out of the ground.
        void TickVines(Player owner)
        {
            if (vines == null || Main.dedServ)
                return;
            float ungrow = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(timer / (float)Math.Max(1, ImpactTick), 0f, 1f));
            vines.LengthScale = 1f - ungrow;
            vines.Disabled = true; // no new targets or break-offs while they go
            vines.DrawTint = Color.Lerp(Color.White, new Color(170, 130, 80), ungrow);
            vines.StartTrimPx = BladeOfGrassTuning.ChainStartTrimPx;
            vines.Tick(Projectile, owner.MountedCenter, _ => owner.MountedCenter, 0f, IsOwner ? BladeOfGrassPlayer.LengthsOf(owner) : null);
        }

        // Called by BladeOfGrassVineLayer (behind the player's body).
        public void AddVineDrawData(ref PlayerDrawSet drawInfo)
        {
            vines?.AddDrawData(ref drawInfo);
        }

        public override void OnKill(int timeLeft)
        {
            vines?.Release();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0)
                return false; // not posed yet
            Main.instance.LoadItem(ItemID.BladeofGrass);
            Texture2D texture = TextureAssets.Item[ItemID.BladeofGrass].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, BladeOfGrassTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.3f), Projectile.rotation + rotationOffset, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}