using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// One dark flame orb orbiting the player. It deals no damage: an enemy that walks into it pops it, and the pop puts an area
    /// Shadowflame debuff on every enemy near it. It lasts until the Dark Lance is put away, the player dies, or OrbLifeTicks (30 s) pass.
    /// SPAWN VALUES (the charge projectile passes them): ai[0] = 0 (1 = popped, decided by the owner), ai[1] = the spawn serial,
    /// ai[2] = 0 (patch 7 uses it for "collected").
    /// MULTIPLAYER: the owner's client decides the pop (it tests every enemy's box against the orb), applies the Shadowflame with
    /// AreaEffect.ApplyBuff (NPC.AddBuff syncs) and flags ai[0] = 1 with netUpdate; every machine then ends its own copy on its next
    /// tick and plays the pop in OnKill. The position is computed on every machine from the owner's position, the serial-based rank and
    /// the shared orbit clock (DarkLancePlayer.OrbTurn), so there are no per-tick packets. Visual state is never synced.
    /// </summary>
    public class DarkLanceOrbProjectile : ModProjectile
    {
        // Only a placeholder sprite: the orb is drawn by a pixel shader (no art needed).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        bool Popped => Projectile.ai[0] >= 1f;

        // Per-machine state: counters and visuals, never synced.
        int age;
        bool placed;
        float shown;          // the angle the orb is drawn at (glides to its slot when the ring changes)
        Vector2 startPos;     // where the orb left the lance
        PixelTrail trail;     // the shared pixel trail (Common), handed to DetachedTrails when the orb ends

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // it never damages anything
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1800;    // the real life is set in OnSpawn from DarkLanceTuning
        }

        public override void OnSpawn(IEntitySource source)
        {
            //Projectile.timeLeft = Math.Max(1, DarkLanceTuning.OrbLifeTicks);
        }

        public override void AI()
        {
            Player owner = Owner;

            // Popped (decided by the owner), or the lance was put away, or the player is gone: this copy ends. OnKill tells them apart.
            if (Popped || !owner.active || owner.dead || owner.HeldItem.type != ItemID.DarkLance)
            {
                Projectile.Kill();
                return;
            }

            // Its place in the ring: the rank among the owner's live orbs (by spawn serial) out of how many there are.
            int rank = OrbitRing.Rank(Projectile, out int count, other => other.ai[0] < 1f);
            DarkLancePlayer plr = owner.GetModPlayer<DarkLancePlayer>();
            float target = OrbitRing.Angle(plr.OrbTurn, rank, count, DarkLanceTuning.OrbSpin);
            if (!placed)
            {
                placed = true;
                shown = target;
                startPos = Projectile.Center;
            }
            else
            {
                // Turns with the ring and glides toward its slot, so a new or popped orb never makes the others jump.
                shown += DarkLanceTuning.OrbSpin + MathHelper.WrapAngle(target - shown) * DarkLanceTuning.OrbEase;
            }

            float grow = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / (float)Math.Max(1, DarkLanceTuning.OrbSpawnTicks), 0f, 1f));
            float radius = DarkLanceTuning.OrbRadius + DarkLanceTuning.OrbBob * (float)Math.Sin(age * 0.09f + rank * 1.3f);
            Vector2 orbit = OrbitRing.Position(owner.Center, radius, shown);
            Projectile.Center = Vector2.Lerp(startPos, orbit, grow);
            Projectile.velocity = Vector2.Zero;

            // The owner decides the pop: any enemy box touching the orb.
            if (IsOwner && age >= DarkLanceTuning.OrbPopGraceTicks)
            {
                Rectangle zone = Utils.CenteredRectangle(Projectile.Center, new Vector2(DarkLanceTuning.OrbHitSize));
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.CanBeChasedBy() && npc.Hitbox.Intersects(zone))
                    {
                        Pop();
                        break;
                    }
                }
            }

            if (!Main.dedServ)
            {
                if (age == 0)
                    DarkLanceFx.OrbBorn(Projectile.Center, count, DarkLanceTuning.MaxOrbs);
                if (age % 2 == 0)
                    DarkLanceFx.Shed(Projectile.Center);
                Color mid = DarkLanceTuning.Palette.Mid;
                Lighting.AddLight(Projectile.Center, mid.ToVector3() * 0.6f * grow);
            }

            age++;
        }

        // Owner only: no damage, only the area Shadowflame; the flag makes every machine end its copy.
        void Pop()
        {
            Projectile.ai[0] = 1f;
            Projectile.netUpdate = true;
            AreaEffect.ApplyBuff(Projectile.Center, DarkLanceTuning.PopRadius, BuffID.ShadowFlame, DarkLanceTuning.PopBuffTicks);
        }

        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            trail ??= new PixelTrail(new DarkLanceOrbTrailStyle(), IsOwner);
            trail.Tick();
            trail.Add(Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                if (Popped)
                {
                    DarkLanceFx.Pop(Projectile.Center, DarkLanceTuning.PopRadius);
                    CameraShake.Punch(Projectile.Center, DarkLanceTuning.PopShake, 12, "DarkLancePop");
                }
                else
                {
                    DarkLanceFx.Fizzle(Projectile.Center);
                }
            }
            DetachedTrails.Hand(trail); // the afterimages keep fading on their own
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            // Pops in with a little overshoot, then flickers; the shader (Effects/DarkFlameOrb.fx) draws the flame ball itself.
            float t = MathHelper.Clamp(age / (float)Math.Max(1, DarkLanceTuning.OrbSpawnTicks), 0f, 1f);
            float appear = t < 1f ? (float)Math.Sin(t * MathHelper.PiOver2) + 0.25f * (float)Math.Sin(t * MathHelper.Pi) : 1f;
            float flicker = 1f + 0.08f * (float)Math.Sin(age * 0.55f + Projectile.ai[1]);
            // The embers break off and stay in the world: the shader is told where the orb turns around, how fast, and how fast that point moves.
            // The values come from the owner's synced position and velocity, so every machine draws the same thing (visual only).
            float lag = DarkLanceTuning.EmberLag * MathHelper.SmoothStep(0f, 1f, t);
            DarkLanceFx.DrawOrb(Projectile.Center, age / 60f, Projectile.ai[1] % 64f, appear, flicker, Owner.Center - Projectile.Center, DarkLanceTuning.OrbSpin * 60f, Owner.velocity * 60f, lag); // the seed is the spawn serial: every machine knows it
            return false;
        }
    }
}