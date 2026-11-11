using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// Overhauls all seven vanilla Phaseblades (blue, red, green, purple, white, yellow, orange): the item itself fires nothing. Left click
    /// starts a PhasebladeSwingProjectile that runs one swing of a combo: swing down, then the arm loops over the head into a second swing
    /// down with a spinning blade, then a swing up with a spinning blade, then it starts over. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class PhasebladeGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return PhasebladeTuning.IsPhaseblade(entity.type);
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<PhasebladeSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the blade
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep swinging
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // One swing at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<PhasebladeSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            PhasebladePlayer state = player.GetModPlayer<PhasebladePlayer>();
            bool combo = state.LastPhase >= 0 && Main.GameUpdateCount - state.LastEndTick <= (uint)PhasebladeTuning.ComboTicks;

            // The combo: swing down -> loop + swing down (spin) -> swing up (spin) -> swing down ... A fresh combo starts with the first swing down.
            PhasebladeSwingProjectile.Phase phase = PhasebladeSwingProjectile.Phase.SwingDown;
            if (combo)
            {
                if (state.LastPhase == (int)PhasebladeSwingProjectile.Phase.SwingDown)
                    phase = PhasebladeSwingProjectile.Phase.SpinDown;
                else if (state.LastPhase == (int)PhasebladeSwingProjectile.Phase.SpinDown)
                    phase = PhasebladeSwingProjectile.Phase.SpinUp;
            }
            float startArm = combo ? state.LastEndArm : MathHelper.ToRadians(PhasebladeTuning.RestArmDeg); // relative to the aim

            // The aim is captured here, once, and travels in the spawn packet (ai[2]) so every machine poses the same swing. It is kept in the
            // player's own frame (mirrored when gravity is flipped), like the arm angles; the whole swing is relative to it.
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            if (player.gravDir == -1f)
                aim = -aim;

            bool spin = phase != PhasebladeSwingProjectile.Phase.SwingDown;
            int scaledDamage = (int)(damage * (spin ? PhasebladeTuning.SpinDamageMultiplier : PhasebladeTuning.SwingDownDamageMultiplier));
            float scaledKnockback = knockback * (spin ? PhasebladeTuning.SpinKnockbackMultiplier : PhasebladeTuning.SwingDownKnockbackMultiplier);

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, scaledDamage, scaledKnockback, player.whoAmI, (float)phase, startArm, aim);
            return false;
        }
    }
}