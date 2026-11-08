using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Reworks the vanilla Enchanted Sword. The item swings and shoots its beam exactly as before; true melee hits (the blade itself, not the
    /// beam) charge it, see EnchantedSwordPlayer. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class EnchantedSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.EnchantedSword;
        }

        // Only the blade's own hits come through here (the beam is a projectile and has its own hook), and only on the attacking player's client.
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            player.GetModPlayer<EnchantedSwordPlayer>().RegisterMeleeHit(item, target);
        }

        // Runs on every machine for every swing: ethereal flecks along the blade while it carries an outline.
        public override void MeleeEffects(Item item, Player player, Rectangle hitbox)
        {
            int stage = player.GetModPlayer<EnchantedSwordPlayer>().Stage;
            if (stage > 0)
                EnchantedSwordFx.SwingDust(hitbox, stage);
        }
    }
}