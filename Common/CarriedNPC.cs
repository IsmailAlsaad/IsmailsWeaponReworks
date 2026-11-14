using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// An enemy that a projectile is carrying (the Rotted Fork javelin's shish kebab). The carrier projectile calls CarriedNPC.Hold on
    /// EVERY machine (the server included) every tick, which places the enemy and refreshes a stamp; while the stamp is fresh the
    /// enemy's AI is off, it cannot hurt a player and it does not move by itself. A carrier that dies for any reason therefore releases
    /// the enemy by itself a moment later (the stamp runs out), with nothing to clean up. The server also flags netUpdate every few
    /// ticks while the enemy is held so its copy stays the authority. Purely a function of the synced carrier, so it needs no packets.
    /// The generalized form of BloodButchererGripNPC; the Blood Butcherer keeps its own copy until a cleanup patch moves it over.
    /// </summary>
    public class CarriedNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;  // Main.GameUpdateCount stamp refreshed by the carrier every tick
        public int DrawFirstProjectile = -1;   // the carrier that wants to be drawn behind this enemy (IDrawnBehindCarried), handed over in Hold every tick; -1 = none

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;

        // An enemy that can be carried: a real, hittable, ordinary enemy (not a boss, not a worm or other multi-part body, not immovable)
        // that is not bigger than maxSize pixels (width or height).
        public static bool CanCarry(NPC npc, float maxSize)
        {
            return npc.CanBeChasedBy() && !npc.boss && npc.knockBackResist > 0f && npc.realLife < 0 && Math.Max(npc.width, npc.height) <= maxSize;
        }

        // The carrier calls this every tick on every machine for each carried enemy.
        // 'drawFirst' (optional): a carrier that wants the enemy drawn IN FRONT of it (IDrawnBehindCarried) hands itself over here every tick; the enemy's own
        // draw then draws the carrier right before the enemy's sprite (see PreDraw below).
        public static void Hold(NPC npc, Vector2 center, Projectile drawFirst = null)
        {
            npc.Center = center;
            npc.velocity = Vector2.Zero;
            CarriedNPC state = npc.GetGlobalNPC<CarriedNPC>();
            state.HoldTick = (long)Main.GameUpdateCount;
            state.DrawFirstProjectile = drawFirst != null ? drawFirst.whoAmI : -1;
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 3 == 0)
                npc.netUpdate = true; // the server's copy is the authority
        }

        // Ends the carry at once (the stamp would run out in a moment anyway): the enemy's AI and own movement work again from this tick,
        // so a velocity given right after this is kept.
        public static void Release(NPC npc)
        {
            npc.GetGlobalNPC<CarriedNPC>().HoldTick = -1000;
        }

        // Walks from 'from' toward 'target' and stops before the first spot where the enemy's box would be inside tiles, so a carried
        // enemy is never dragged into a wall.
        public static Vector2 ClampToOpenAir(Vector2 from, Vector2 target, NPC npc)
        {
            Vector2 delta = target - from;
            float length = delta.Length();
            if (length < 1f)
                return target;
            Vector2 direction = delta / length;
            Vector2 half = new Vector2(npc.width, npc.height) * 0.5f;
            Vector2 safe = from;
            for (float d = 8f; ; d += 8f)
            {
                float at = Math.Min(d, length);
                Vector2 p = from + direction * at;
                if (Collision.SolidCollision(p - half, npc.width, npc.height))
                    return safe;
                safe = p;
                if (at >= length)
                    return safe;
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (Held)
            {
                npc.velocity = Vector2.Zero;
                return false;
            }
            return true;
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            return !Held;
        }

        // A carrier that handed itself over in Hold is drawn HERE, inside the enemy's own draw, right before the enemy's sprite: the sprite lands on top, so
        // the enemy covers the carrier (and everything the carrier draws with it). It does not depend on any draw layer list, so no ordering can put the
        // carrier in front. The carrier skips its own draw for that tick.
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Held && DrawFirstProjectile >= 0 && DrawFirstProjectile < Main.maxProjectiles)
            {
                Projectile carrier = Main.projectile[DrawFirstProjectile];
                if (carrier.active && carrier.ModProjectile is IDrawnBehindCarried behind)
                    behind.DrawBehindCarried(npc);
            }
            return true;
        }
    }

    /// <summary>A projectile that carries an enemy (CarriedNPC.Hold with itself as 'drawFirst') and wants the enemy drawn in FRONT of it.</summary>
    public interface IDrawnBehindCarried
    {
        /// <summary>Called from the carried enemy's draw (a spritebatch is running), right before the enemy's sprite. Draw the whole carrier here; the carrier skips its own normal draw for this tick.</summary>
        void DrawBehindCarried(NPC npc);
    }
}