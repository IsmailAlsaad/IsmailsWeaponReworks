using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The player's side of the iaido (patch 3): when the draw window closes with a real path, Launch spawns one MuramasaSlashProjectile per
    /// stroke and this held projectile. The arm whips from the hip pose to the aim in IaidoDrawTicks, the sword flashes and vanishes (the
    /// phantom does the cutting), the empty arm holds the aim until the last slash has arrived, then the sword is back in the hand and
    /// snaps to the ready pose; the owner then starts a new MuramasaStanceProjectile from it, so the sword sheathes again from the start.
    /// MULTIPLAYER: Launch runs on the owner's client only. ai[0] = the aim (radians, the player's own frame, toward the path's start),
    /// ai[1] = the tick at which the last slash has arrived. The start pose is always the sheathed pose (the window only opens on a fully
    /// sheathed sword), so nothing else has to travel; every machine runs the same timers. No custom packets.
    /// </summary>
    public class MuramasaIaidoProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

        float Aim => Projectile.ai[0];
        int Hold => Math.Max(MuramasaTuning.IaidoDrawTicks + 1, (int)Projectile.ai[1]);
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        float flash;
        float arm;      // the arm's world angle (player's frame)
        float blade;    // the blade's world angle
        bool shown = true;
        Vector2 tipPosition;

        // Called on the owner's client when the draw window closes with a path: spawns the slashes and the pose projectile, ends the stance.
        public static void Launch(Player owner, IList<Vector2> strokes)
        {
            Item item = owner.HeldItem;
            IEntitySource source = owner.GetSource_ItemUse(item);
            int damage = (int)(owner.GetWeaponDamage(item) * MuramasaTuning.SlashDamageMultiplier);
            float knockback = owner.GetWeaponKnockback(item, item.knockBack);
            int slashType = ModContent.ProjectileType<MuramasaSlashProjectile>();
            int gap = Math.Max(1, MuramasaTuning.SlashGapTicks);

            int end = 0;
            for (int i = 0; i < strokes.Count - 1; i++)
            {
                Vector2 offset = strokes[i + 1] - strokes[i];
                int delay = MuramasaTuning.IaidoDrawTicks + i * gap;
                Projectile.NewProjectile(source, strokes[i], Vector2.Zero, slashType, damage, knockback, owner.whoAmI, offset.X, offset.Y, delay);
                end = Math.Max(end, delay + MuramasaSlashProjectile.TravelTicks(offset.Length()));
            }

            // The arm points toward where the path begins (or where it ends, if that is right on top of the player).
            Vector2 toStart = strokes[0] - owner.MountedCenter;
            Vector2 toEnd = strokes[strokes.Count - 1] - owner.MountedCenter;
            float aim = (toStart.LengthSquared() > 400f ? toStart : toEnd).ToRotation();
            if (owner.gravDir == -1f)
                aim = -aim;

            Projectile stance = MuramasaPlayer.Find(owner.whoAmI, ModContent.ProjectileType<MuramasaStanceProjectile>());
            stance?.Kill();
            Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero, ModContent.ProjectileType<MuramasaIaidoProjectile>(), 0, 0f, owner.whoAmI, aim, end);
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

        static float LerpAngle(float from, float to, float amount)
        {
            return from + MathHelper.WrapAngle(to - from) * amount;
        }

        bool LostOwner(Player owner)
        {
            return !owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Muramasa;
        }

        public override void AI()
        {
            Player owner = Owner;
            if (LostOwner(owner))
            {
                Projectile.Kill();
                return;
            }

            int recover = Math.Max(1, MuramasaTuning.IaidoRecoverTicks);
            if (ticks >= Hold + recover)
            {
                owner.itemAnimation = 0; // the next use may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (ticks == 0 && IsOwner)
                owner.ChangeDir(Facing);

            Projectile.scale = MuramasaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            float sheathArm = HeldSwordPose.ToWorld(Facing, Rad(MuramasaTuning.SheathArmDeg));
            float sheathBlade = HeldSwordPose.ToWorld(Facing, Rad(MuramasaTuning.SheathArmDeg + MuramasaTuning.SheathTiltDeg));
            float readyArm = HeldSwordPose.ToWorld(Facing, Rad(MuramasaTuning.ReadyArmDeg));
            float readyBlade = HeldSwordPose.ToWorld(Facing, Rad(MuramasaTuning.ReadyArmDeg + MuramasaTuning.ReadyTiltDeg));
            int draw = Math.Max(1, MuramasaTuning.IaidoDrawTicks);

            if (ticks < draw)
            {
                // The whip: fast at the start, decelerating into the aim.
                float t = (ticks + 1f) / draw;
                float eased = 1f - (1f - t) * (1f - t);
                arm = LerpAngle(sheathArm, Aim, eased);
                blade = LerpAngle(sheathBlade, Aim, eased);
                shown = true;
            }
            else if (ticks < Hold)
            {
                arm = Aim;
                blade = Aim;
                shown = false; // the phantom has the sword
            }
            else
            {
                // The sword is back: a snap from the aim to the ready pose.
                float eased = MathHelper.SmoothStep(0f, 1f, (ticks - Hold + 1f) / recover);
                arm = LerpAngle(Aim, readyArm, eased);
                blade = LerpAngle(Aim, readyBlade, eased);
                shown = true;
            }

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the iaido lasts
            owner.itemTime = 2;
            ApplyPose(owner);

            if (ticks == draw)
            {
                flash = 1f;
                MuramasaFx.IaidoDraw(Projectile.Center, tipPosition);
            }
            if (ticks == Hold)
            {
                flash = 1f;
                MuramasaFx.Click(Projectile.Center, tipPosition, false);
                CameraShake.Punch(Projectile.Center, MuramasaTuning.ClickShake, 8);
            }
            if (shown)
                Lighting.AddLight(tipPosition, 0.2f, 0.4f, 0.9f);

            flash = Math.Max(0f, flash - 1f / Math.Max(1, MuramasaTuning.FlashTicks));
            ticks++;
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, arm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, arm);
            float bladeAngle = owner.gravDir == -1f ? -blade : blade;
            Projectile.rotation = bladeAngle;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + bladeAngle.ToRotationVector2() * MuramasaTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // The owner starts the sheathing again from the ready pose.
        public override void OnKill(int timeLeft)
        {
            if (!IsOwner)
                return;
            Player owner = Owner;
            if (LostOwner(owner))
                return;
            MuramasaStanceProjectile.Spawn(Projectile.GetSource_FromThis(), owner, Rad(MuramasaTuning.ReadyArmDeg), Rad(MuramasaTuning.ReadyTiltDeg));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!shown)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, MuramasaTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            float glow = 0.25f + flash;
            Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.DeepColor * (0.5f * glow), rotation, origin, scale * (1.2f + 0.2f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.BlueColor * (0.6f * glow), rotation, origin, scale * (1.08f + 0.1f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f + 0.5f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.CoreColor * (0.8f * flash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}