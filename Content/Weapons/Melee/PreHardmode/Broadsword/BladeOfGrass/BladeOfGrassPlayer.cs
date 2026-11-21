using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// The length of each of the Blade of Grass's vines lives here, on the owner's machine (one number per vine, in pixels). A true melee
    /// hit adds to all of them (BladeOfGrassBladeProjectile calls AddCharge), a section that breaks off a vine takes its length away for
    /// good (the vine set writes it back), and they cool toward MinLength on their own once no hit has counted for DecayHoldTicks. They are
    /// lost (reset to MinLength) when the player swaps to another item or dies; they are never saved.
    /// It also keeps the held blade alive: whenever the sword is held and no BladeOfGrassBladeProjectile exists, it spawns one. The other
    /// machines learn the lengths from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class BladeOfGrassPlayer : ModPlayer
    {
        public float[] Lengths = new float[0];
        int sinceHit;
        bool rightWasDown; // the right button on the previous tick (a plant needs a fresh press)

        void EnsureLengths()
        {
            int count = Math.Max(1, BladeOfGrassTuning.VineCount);
            if (Lengths.Length == count)
                return;
            Lengths = new float[count];
            for (int i = 0; i < count; i++)
                Lengths[i] = BladeOfGrassTuning.MinLength;
        }

        void ResetLengths()
        {
            EnsureLengths();
            for (int i = 0; i < Lengths.Length; i++)
                Lengths[i] = BladeOfGrassTuning.MinLength;
            sinceHit = 0;
        }

        // The array itself (the owner's vine set reads it every tick and writes back what a break-off took).
        public static float[] LengthsOf(Player player)
        {
            BladeOfGrassPlayer state = player.GetModPlayer<BladeOfGrassPlayer>();
            state.EnsureLengths();
            return state.Lengths;
        }

        // 0..1: how far the vines are on average between MinLength and MaxLength (drives the glow of the blade).
        public static float ChargeOf(Player player)
        {
            float[] lengths = LengthsOf(player);
            float sum = 0f;
            for (int i = 0; i < lengths.Length; i++)
                sum += lengths[i];
            float mean = sum / lengths.Length;
            return MathHelper.Clamp((mean - BladeOfGrassTuning.MinLength) / Math.Max(1f, BladeOfGrassTuning.MaxLength - BladeOfGrassTuning.MinLength), 0f, 1f);
        }

        // 'fraction' is the shared blade's charge step (BladeOfGrassTuning.ChargePerHit): every vine gets that share of the span from MinLength to MaxLength.
        public static void AddCharge(Player player, float fraction)
        {
            BladeOfGrassPlayer state = player.GetModPlayer<BladeOfGrassPlayer>();
            state.EnsureLengths();
            float pixels = fraction * Math.Max(1f, BladeOfGrassTuning.MaxLength - BladeOfGrassTuning.MinLength);
            for (int i = 0; i < state.Lengths.Length; i++)
                state.Lengths[i] = Math.Min(BladeOfGrassTuning.MaxLength, state.Lengths[i] + pixels);
            state.sinceHit = 0;
        }

        static Projectile Find(int owner, int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type)
                    return projectile;
            }
            return null;
        }

        // Owner only: right click drives the sword into the ground. The vines it has grown stay there as a planted set; the player starts a new set.
        void TryPlant()
        {
            if (BladeOfGrassPlantedProjectile.HidesSword(Player.whoAmI))
                return; // the sword is not in the hand
            if (Find(Player.whoAmI, ModContent.ProjectileType<BladeOfGrassBladeProjectile>()) == null)
                return; // the held blade has not formed yet
            Item item = Player.HeldItem;
            if (!BladeOfGrassPlantedProjectile.TryPlant(Player, Lengths, Player.GetSource_ItemUse(item), Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack)))
                return; // no ground in reach
            for (int i = 0; i < Lengths.Length; i++)
                Lengths[i] = BladeOfGrassTuning.MinLength;
            sinceHit = 0;
        }

        public override void OnRespawn()
        {
            ResetLengths();
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            EnsureLengths();
            bool pressed = Player.controlUseTile && !rightWasDown;
            rightWasDown = Player.controlUseTile;

            if (Player.dead || !ReworkToggle.Holds(Player, ItemID.BladeofGrass))
            {
                ResetLengths();
                return;
            }

            if (pressed && !Player.noItems && !Player.CCed && Player.gravDir == 1f)
                TryPlant();

            sinceHit++;
            if (sinceHit > BladeOfGrassTuning.DecayHoldTicks)
            {
                for (int i = 0; i < Lengths.Length; i++)
                {
                    if (Lengths[i] > BladeOfGrassTuning.MinLength)
                        Lengths[i] = Math.Max(BladeOfGrassTuning.MinLength, Lengths[i] - BladeOfGrassTuning.DecayPerTick);
                }
            }

            if (Player.noItems || Player.CCed)
                return;
            int type = ModContent.ProjectileType<BladeOfGrassBladeProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Item item = Player.HeldItem;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}