using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// What the Blood Butcherer does to an enemy it has gripped. The swing projectile (on every machine, the server included) refreshes
    /// HoldTick every tick while the enemy is pinned in the jaws, so a projectile that dies for any reason releases the enemy by itself a
    /// moment later. While held the enemy's AI is off and it cannot hurt the player; after the throw (LaunchTick) its AI stays off for
    /// BloodButchererTuning.LaunchAiOffTicks, with gravity applied here by hand because the AI that normally does it is not running.
    /// Normal tile collision and movement still run, and a tile hit during the flight costs the enemy health from the speed it loses
    /// (patch 3, see CheckImpact; only the thrower's machine applies the damage). Purely a function of the synced projectile, so it needs no packets; the server
    /// also flags netUpdate while the enemy is held so its copy stays the authority.
    /// </summary>
    public class BloodButchererGripNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;    // Main.GameUpdateCount stamp refreshed by the projectile while the enemy is pinned
        public long LaunchTick = -1000;  // Main.GameUpdateCount stamp of the throw

        // The throw's collision damage: who threw it (only that machine applies the damage) and the projectile's damage at the throw.
        public int ImpactOwner = -1;
        public int ImpactBaseDamage;
        bool tracking;           // a thrown enemy whose tile hits count
        bool lastKnown;          // lastVelocity and lastPosition are from the previous tick
        Vector2 lastVelocity;    // the velocity this enemy went into the last tick's movement with
        Vector2 lastPosition;
        long lastImpactTick = -1000;

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;
        public bool Launched => Main.GameUpdateCount - LaunchTick < BloodButchererTuning.LaunchAiOffTicks;

        // Called by the swing projectile at the throw, on every machine: from now on a tile hit costs the enemy health.
        public void BeginImpact(int owner, int baseDamage)
        {
            ImpactOwner = owner;
            ImpactBaseDamage = baseDamage;
            tracking = true;
            lastKnown = false;
        }

        public override bool PreAI(NPC npc)
        {
            if (Held)
            {
                npc.velocity = Vector2.Zero;
                tracking = false;
                lastKnown = false;
                return false;
            }

            // What the last tick's movement did to the thrown enemy (this also runs once more on the tick the AI-off window has ended).
            if (tracking && lastKnown)
                CheckImpact(npc);

            if (Launched)
            {
                if (!npc.noGravity)
                    npc.velocity.Y = System.Math.Min(npc.velocity.Y + BloodButchererTuning.LaunchGravity, BloodButchererTuning.LaunchMaxFall);
                lastVelocity = npc.velocity;
                lastPosition = npc.position;
                lastKnown = true;
                return false;
            }
            tracking = false;
            lastKnown = false;
            return true;
        }

        // The tile collision that ran after the last PreAI took speed away from this enemy: vanilla flags the blocked axes (collideX /
        // collideY) and the position check makes sure it really was stopped (and not just moved by a network correction). The speed it
        // lost (the length of the velocity that was cancelled) is the impact: a wall or the floor stops it dead, and the damage scales with
        // that speed. Every machine plays the effects; only the thrower's machine strikes the enemy (SimpleStrikeNPC syncs it).
        void CheckImpact(NPC npc)
        {
            Vector2 moved = npc.position - lastPosition;
            bool hitX = npc.collideX && System.Math.Abs(lastVelocity.X) > 0.5f && System.Math.Abs(moved.X) < System.Math.Abs(lastVelocity.X) - 0.25f;
            bool hitY = npc.collideY && System.Math.Abs(lastVelocity.Y) > 0.5f && System.Math.Abs(moved.Y) < System.Math.Abs(lastVelocity.Y) - 0.25f;
            if (!hitX && !hitY)
                return;

            Vector2 lost = new Vector2(hitX ? lastVelocity.X : 0f, hitY ? lastVelocity.Y : 0f);
            if (hitX)
                npc.velocity.X = 0f;
            if (hitY)
                npc.velocity.Y = 0f;

            float impact = lost.Length();
            if (impact < BloodButchererTuning.ImpactMinSpeed || Main.GameUpdateCount - lastImpactTick < BloodButchererTuning.ImpactCooldownTicks)
                return;
            lastImpactTick = Main.GameUpdateCount;

            Vector2 normal = -Vector2.Normalize(lost);
            Vector2 point = npc.Center + new Vector2(hitX ? System.Math.Sign(lastVelocity.X) * npc.width * 0.5f : 0f, hitY ? System.Math.Sign(lastVelocity.Y) * npc.height * 0.5f : 0f);
            BloodButchererFx.ImpactBurst(point, normal, impact);
            BloodButchererFx.PlayImpact(point, impact);
            CameraShake.Punch(point, BloodButchererTuning.ImpactShake * MathHelper.Clamp(impact / System.Math.Max(1f, BloodButchererTuning.LaunchSpeed), 0.3f, 1.5f), 16, "BloodButchererImpact");

            if (Main.myPlayer != ImpactOwner || npc.life <= 0 || npc.dontTakeDamage || npc.immortal)
                return;
            float multiplier = System.Math.Min(BloodButchererTuning.ImpactMaxMultiplier, BloodButchererTuning.ImpactDamageMultiplier * impact / System.Math.Max(1f, BloodButchererTuning.LaunchSpeed));
            int damage = System.Math.Max(1, (int)(ImpactBaseDamage * multiplier));
            npc.SimpleStrikeNPC(damage, lastVelocity.X >= 0f ? 1 : -1, false, 0f, DamageClass.Melee);
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            return !(Held || Launched);
        }
    }
}