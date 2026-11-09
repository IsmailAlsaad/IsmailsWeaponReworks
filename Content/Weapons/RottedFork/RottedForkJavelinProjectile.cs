using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>
    /// The Rotted Fork thrown like a javelin (right click). It flies straight at the cursor and pierces everything (each enemy once); the
    /// first few ordinary enemies it hits are IMPALED and carried along the shaft behind the tip like a shish kebab (a boss, a worm or an
    /// immovable or huge enemy is only damaged). When it hits a tile or runs out of life it explodes in blood: every carried enemy and
    /// everything near the tip is struck, and the carried enemies are released. The item itself is never lost.
    /// SPAWN VALUES: velocity = the throw (direction times RottedForkTuning.JavelinSpeed), ai[] unused.
    /// MULTIPLAYER: the owner's client decides everything (the throw, every impale, the explosion damage). The flight is a normal synced
    /// projectile. The impaled enemies travel through SendExtraAI / ReceiveExtraAI (their npc index and type, sent when the owner flags
    /// netUpdate); EVERY machine (the server included) then places them along the shaft each tick through CarriedNPC.Hold, so the carry
    /// needs no per-tick packets and a javelin that dies releases them by itself. The explosion's damage is the owner's alone
    /// (NPC.SimpleStrikeNPC syncs it); every machine plays the blood burst from OnKill. Other players' machines find the hits themselves
    /// and only show sparks (RemoteHitTracker).
    /// </summary>
    public class RottedForkJavelinProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite.
        public override string Texture => "Terraria/Images/Item_" + ItemID.TheRottedFork;

        bool IsOwner => Projectile.owner == Main.myPlayer;

        // The impaled enemies, nearest the tip LAST: X = npc index, Y = npc type (so a slot reused by a new enemy is never dragged along).
        // Created on first use: a field initializer here would be shared by every projectile of the type.
        List<Point> impaled;
        List<Point> Impaled => impaled ??= new List<Point>();

        // Per-machine state: counters and visuals, never synced.
        int age;
        bool hasPose;
        Vector2 butt;
        Vector2 tip;
        Vector2 prevButt;
        Vector2 prevTip;
        float shaft;
        Vector2 flightDirection = Vector2.UnitX; // the last direction it flew in (a tile hit zeroes the velocity before OnKill)
        SweepTrail trail;
        RemoteHitTracker remoteHits;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;              // the real life is set in OnSpawn from RottedForkTuning
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // every enemy once
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = Math.Max(1, RottedForkTuning.JavelinLifeTicks);
            RottedForkFx.Throw(Projectile.Center);
        }

        public override void AI()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            flightDirection = direction;
            Projectile.scale = RottedForkTuning.SpearScale;
            Item sample = ContentSamples.ItemsByType[ItemID.TheRottedFork];
            shaft = new Vector2(sample.width, sample.height).Length() * Projectile.scale;

            Vector2 lastButt = butt;
            Vector2 lastTip = tip;
            tip = Projectile.Center;       // the tip leads: a tile is hit by the tip
            butt = tip - direction * shaft;
            Projectile.rotation = direction.ToRotation();
            prevButt = hasPose ? lastButt : butt;
            prevTip = hasPose ? lastTip : tip;
            hasPose = true;

            CarryStep(direction);

            if (!Main.dedServ)
                Visuals(direction);

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, 0, npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, npc.Center), RottedForkTuning.Palette));

            age++;
        }

        // The enemy an impaled entry stands for, or null when it is gone (dead, or its slot was reused by another kind of enemy).
        static NPC Carried(Point entry)
        {
            if (entry.X < 0 || entry.X >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[entry.X];
            return npc.active && npc.type == entry.Y ? npc : null;
        }

        // Every machine: place each impaled enemy along the shaft behind the tip (the newest nearest the tip), easing toward its slot.
        // The owner drops an enemy that died or whose slot was reused (and tells the others).
        void CarryStep(Vector2 direction)
        {
            List<Point> list = Impaled;
            float behind = RottedForkTuning.CarryFrontGap;
            bool changed = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                NPC npc = Carried(list[i]);
                if (npc == null)
                {
                    if (IsOwner)
                    {
                        list.RemoveAt(i);
                        changed = true;
                    }
                    continue;
                }
                float radius = Math.Max(npc.width, npc.height) * 0.5f;
                behind += radius;
                Vector2 slot = CarriedNPC.ClampToOpenAir(tip, tip - direction * behind, npc);
                CarriedNPC.Hold(npc, Vector2.Lerp(npc.Center, slot, RottedForkTuning.CarryFollow));
                behind += radius + RottedForkTuning.CarryGap;
                if (!Main.dedServ && Main.rand.NextBool(6))
                    RottedForkFx.Drip(npc.Center);
            }
            if (changed)
                Projectile.netUpdate = true;
        }

        // Every machine: a streak behind the flying fork, flecks and light.
        void Visuals(Vector2 direction)
        {
            ThrustPalette palette = RottedForkTuning.Palette;
            RottedForkFx.Flecks(tip, tip - prevTip, 1f, palette);
            Lighting.AddLight(tip, palette.Mid.ToVector3() * 0.6f);

            trail ??= new SweepTrail(new ThrustTrailStyle(palette, RottedForkTuning.TrailLifeTicks, RottedForkTuning.TrailRails, RottedForkTuning.PixelSize, RottedForkTuning.TrailOpacity, RottedForkTuning.JavelinTrailTaper), IsOwner);
            for (int i = 1; i <= 2; i++)
            {
                float f = i / 2f - RottedForkTuning.TrailLagTicks;
                Vector2 tipPoint = Vector2.Lerp(prevTip, tip, f);
                Vector2 root = Vector2.Lerp(prevButt, butt, f);
                float outer = RottedForkTuning.TrailOuter;
                trail.AddSegment(root, tipPoint, outer, outer * (1f - RottedForkTuning.TrailInnerFraction));
            }
            trail.Tick();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!hasPose)
                return null;
            return SegmentHit.Sweep(targetHitbox, prevButt, prevTip, butt, tip, RottedForkTuning.JavelinHitWidth * Projectile.scale, 2);
        }

        // Owner only: an ordinary enemy is impaled (up to MaxImpaled), anything else is just pierced and damaged.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 point = SegmentHit.ClosestPoint(butt, tip, target.Center);
            ThrustFx.HitSpark(point, RottedForkTuning.Palette);

            List<Point> list = Impaled;
            if (list.Count >= RottedForkTuning.MaxImpaled || !CarriedNPC.CanCarry(target, RottedForkTuning.MaxCarrySize))
                return;
            list.Add(new Point(target.whoAmI, target.type));
            Projectile.netUpdate = true; // the other machines hear about it through SendExtraAI
            RottedForkFx.Impale(point, Projectile.velocity.SafeNormalize(Vector2.UnitX));
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            List<Point> list = Impaled;
            writer.Write((byte)list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                writer.Write((short)list[i].X);
                writer.Write((short)list[i].Y);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            List<Point> list = Impaled;
            list.Clear();
            int count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                list.Add(new Point(reader.ReadInt16(), reader.ReadInt16()));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true; // a tile ends the flight: OnKill is the explosion
        }

        // The end of the flight, however it ends (a tile or the lifetime): every machine plays the blood burst; the owner strikes the
        // carried enemies and everything near the tip. The carried enemies are released a moment later (their stamp runs out).
        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            List<Point> list = Impaled;

            if (!Main.dedServ)
            {
                RottedForkFx.Explode(center);
                for (int i = 0; i < list.Count; i++)
                {
                    NPC npc = Carried(list[i]);
                    if (npc != null)
                        RottedForkFx.Explode(npc.Center, 0.6f);
                }
            }

            // The impaled enemies are released and flung the way the fork was flying. Server (or single player) only: it sets the velocity and
            // syncs it. The carry stamp is cleared first so the enemy's AI is back on this tick and the velocity is kept.
            for (int i = 0; i < list.Count; i++)
            {
                NPC npc = Carried(list[i]);
                if (npc == null)
                    continue;
                CarriedNPC.Release(npc);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = flightDirection * RottedForkTuning.CarriedKnockback * npc.knockBackResist;
                    npc.netUpdate = true;
                }
            }

            if (IsOwner)
            {
                Blast(center, list);
                Player owner = Main.player[Projectile.owner];
                owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemID.TheRottedFork, RottedForkTuning.JavelinCooldownTicks);
                owner.GetModPlayer<RottedForkPlayer>().AutoThrow = owner.controlUseTile; // right click still held: throw again once the cooldown is over
            }

            DetachedTrails.Hand(trail); // the streak keeps fading on its own
            trail = null;
        }

        // Owner's client only. Every enemy gets hit once: the carried ones for extra, everything else near the tip.
        void Blast(Vector2 center, List<Point> carried)
        {
            HashSet<int> done = new HashSet<int>();
            int damage = Math.Max(1, (int)(Projectile.damage * RottedForkTuning.BlastDamageMultiplier));
            int carriedDamage = Math.Max(1, (int)(damage * RottedForkTuning.CarriedBlastMultiplier));

            for (int i = 0; i < carried.Count; i++)
            {
                NPC npc = Carried(carried[i]);
                if (npc != null && done.Add(npc.whoAmI))
                    Strike(npc, flightDirection.X >= 0f ? 1 : -1, carriedDamage, 0f); // the knockback is the fling in OnKill
            }
            AreaEffect.ForEachNpc(center, RottedForkTuning.BlastRadius, npc =>
            {
                if (done.Add(npc.whoAmI))
                    Strike(npc, npc.Center.X >= center.X ? 1 : -1, damage, RottedForkTuning.BlastKnockback);
            });
        }

        void Strike(NPC npc, int direction, int damage, float knockback)
        {
            npc.SimpleStrikeNPC(damage, direction, false, knockback, DamageClass.Melee);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = RottedForkTuning.Palette;
            float scale = Projectile.scale;
            float rotation = Projectile.rotation;

            SpearPose.DrawSprite(texture, butt, rotation, 1, scale * 1.18f, palette.Halo * 0.45f, 0f, default, scale);
            SpearPose.DrawSprite(texture, butt, rotation, 1, scale * 1.07f, palette.Mid * 0.5f, 0f, default, scale);
            SpearPose.DrawSprite(texture, butt, rotation, 1, scale, Color.Lerp(lightColor, Color.White, 0.5f));
            return false;
        }
    }
}