using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The Katana's hit-stop: an enemy the cut touches freezes in place for KatanaTuning.HitStopTicks (its AI is off, it does not move) and
    /// flashes white, then it is released with the velocity it had plus any knockback that arrived while it was frozen, so the hit
    /// "lands" and the knockback springs out of the freeze. The slash projectile calls Apply on EVERY machine (the server included), from
    /// its own hit test against its own copy of the enemies, so the server's copy (the authority) and every client's copy freeze together
    /// with no packets. The server also flags netUpdate when the freeze starts and ends. Worm and other multi-part bodies (realLife)
    /// are skipped.
    /// </summary>
    public class KatanaHitStopNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        long freezeUntil = -1000;   // Main.GameUpdateCount at which the freeze ends
        int freezeTotal = 1;
        bool holding;               // the enemy's own velocity is kept in 'pending' while it is frozen
        Vector2 pending;

        bool Frozen => (long)Main.GameUpdateCount < freezeUntil;

        public static void Apply(NPC npc, int ticks)
        {
            if (ticks <= 0 || !npc.active || npc.realLife >= 0)
                return;
            KatanaHitStopNPC stop = npc.GetGlobalNPC<KatanaHitStopNPC>();
            long until = (long)Main.GameUpdateCount + ticks;
            if (until <= stop.freezeUntil)
                return;
            stop.freezeUntil = until;
            stop.freezeTotal = ticks;
        }

        public override bool PreAI(NPC npc)
        {
            if (Frozen)
            {
                // The first frozen tick keeps the enemy's velocity; later ticks add whatever pushed it (the knockback) meanwhile.
                pending = holding ? pending + npc.velocity : npc.velocity;
                if (!holding && Main.netMode == NetmodeID.Server)
                    npc.netUpdate = true;
                holding = true;
                npc.velocity = Vector2.Zero;
                return false;
            }
            if (holding)
            {
                holding = false;
                npc.velocity = pending;
                if (Main.netMode == NetmodeID.Server)
                    npc.netUpdate = true;
            }
            return true;
        }

        // The frozen enemy flashes white, fading as the freeze runs out.
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!Frozen)
                return;
            float left = (freezeUntil - (long)Main.GameUpdateCount) / (float)System.Math.Max(1, freezeTotal);
            drawColor = Color.Lerp(drawColor, Color.White, MathHelper.Clamp(KatanaTuning.HitStopFlash * left, 0f, 1f));
        }
    }
}