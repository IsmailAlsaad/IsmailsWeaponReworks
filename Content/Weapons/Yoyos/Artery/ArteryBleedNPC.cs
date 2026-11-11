using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// Vanilla Bleeding does nothing to an enemy (it only stops a player's regeneration), so the Artery's blood explosion needs this:
    /// an enemy with the Bleeding buff loses life every second (ArteryTuning.BleedDamagePerSecond) and drips blood. The buff itself is
    /// put on by the owner's client (NPC.AddBuff syncs it); the life loss runs where an enemy's life is decided (the server and
    /// singleplayer) like every other damage over time, and the drip is visual on every machine.
    /// </summary>
    public class ArteryBleedNPC : GlobalNPC
    {
        readonly ArteryTuning tuning = new ArteryTuning();

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!npc.HasBuff(BuffID.Bleeding))
                return;
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;
            int perSecond = Math.Max(1, (int)tuning.BleedDamagePerSecond);
            npc.lifeRegen -= perSecond * 2;   // lifeRegen counts half-life per second
            if (damage < perSecond)
                damage = perSecond;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!npc.HasBuff(BuffID.Bleeding) || !Main.rand.NextBool(4))
                return;
            Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0.5f, 0, default, Main.rand.NextFloat(0.9f, 1.3f));
            dust.velocity *= 0.4f;
        }
    }
}