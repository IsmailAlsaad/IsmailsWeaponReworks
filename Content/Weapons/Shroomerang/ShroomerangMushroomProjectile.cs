using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>
    /// A glowing mushroom the Shroomerang drops. It spins, slows down and sits there until its time runs out, an enemy walks into
    /// it, or the owner has more than MaxMushrooms (the oldest goes first). Then it becomes a ShroomerangBurstProjectile.
    /// It never damages by itself: friendly is false, its damage field only carries the burst's damage.
    /// MULTIPLAYER: the owner's client decides the trigger and spawns the burst (so the damage goes through the normal owner-client
    /// hit path). Spin, drag and lifetime are deterministic, so every machine runs them on its own copy.
    /// ai[0]: 1 = explode now (set by the owner, synced through netUpdate; every machine then removes its own copy next tick)
    /// ai[1]: spin direction (+1 or -1, set at spawn)
    /// localAI[0]: age in ticks
    /// </summary>
    public class ShroomerangMushroomProjectile : ModProjectile
    {
        // The vanilla glowing mushroom sprite.
        public override string Texture => "Terraria/Images/Item_" + ItemID.GlowingMushroom;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ShroomerangTuning.MushroomLifeTicks;
        }

        // Called by the boomerang (owner's client) right before it spawns a new mushroom: flags the oldest ones until only
        // MaxMushrooms - 1 are left. The oldest is the one with the least time left. Flagged ones explode on their next tick.
        public static void EvictForNewMushroom(int ownerIndex)
        {
            int type = ModContent.ProjectileType<ShroomerangMushroomProjectile>();
            for (int guard = 0; guard < 32; guard++)
            {
                int count = 0;
                Projectile oldest = null;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.type != type || p.owner != ownerIndex || p.ai[0] == 1f)
                        continue;
                    count++;
                    if (oldest == null || p.timeLeft < oldest.timeLeft)
                        oldest = p;
                }
                if (count < ShroomerangTuning.MaxMushrooms || oldest == null)
                    return;
                oldest.ai[0] = 1f;
                oldest.netUpdate = true;
            }
        }

        public override void AI()
        {
            if (!Main.player[Projectile.owner].active || Projectile.ai[0] == 1f)
            {
                Projectile.Kill(); // explode now (OnKill makes the burst on the owner's client)
                return;
            }

            float age = Projectile.localAI[0]++;
            if (age == 0f)
            {
                ShroomerangFx.PlayPop(Projectile.Center);
                ShroomerangFx.SporePuff(Projectile.Center, 6, 2f, 1f);
            }

            // Gradual stop; the spin goes on.
            Projectile.velocity *= ShroomerangTuning.Drag;
            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = Vector2.Zero;
            Projectile.rotation += ShroomerangTuning.MushroomSpinSpeed * (Projectile.ai[1] >= 0f ? 1f : -1f);

            bool warning = Projectile.timeLeft < ShroomerangTuning.WarnTicks;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(age * 0.15f);
            Lighting.AddLight(Projectile.Center, 0.1f + 0.1f * pulse, 0.3f + 0.2f * pulse, 0.7f + 0.2f * pulse);

            if (Projectile.velocity.LengthSquared() > 0.5f && Main.rand.NextBool(2))
                ShroomerangFx.Trail(Projectile.Center, Projectile.velocity, 1f); // spores behind it while it still moves
            if (Main.rand.NextBool(warning ? 3 : 8))
                ShroomerangFx.SporeCloud(Projectile.Center, 10f, 1); // spores drifting up, more of them right before it goes off

            if (Projectile.owner == Main.myPlayer && age >= ShroomerangTuning.ArmTicks && (int)age % ShroomerangTuning.TriggerCheckInterval == 0)
                TriggerOnEnemy();
        }

        // An enemy touching the mushroom sets it off. Owner's client only; the flag is synced and every machine removes its copy.
        void TriggerOnEnemy()
        {
            Rectangle box = Projectile.Hitbox;
            int pad = (int)ShroomerangTuning.TriggerPadding;
            box.Inflate(pad, pad);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.CanBeChasedBy(Projectile) && npc.Hitbox.Intersects(box))
                {
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                    return;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false; // it just stops against a tile
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer || Main.gameMenu)
                return;
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShroomerangBurstProjectile>(),
                Math.Max(1, Projectile.damage), Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float age = Projectile.localAI[0];

            // Pops in with a little overshoot.
            float scale = 1f;
            if (age < ShroomerangTuning.PopTicks)
            {
                float t = age / ShroomerangTuning.PopTicks - 1f;
                scale = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
            }

            // The glow pulses; in the last stretch it blinks white, faster and faster.
            float pulse = 0.5f + 0.5f * (float)Math.Sin(age * 0.15f);
            float warn = 0f;
            if (Projectile.timeLeft < ShroomerangTuning.WarnTicks)
            {
                float into = 1f - Projectile.timeLeft / (float)ShroomerangTuning.WarnTicks;
                warn = 0.5f + 0.5f * (float)Math.Sin(into * into * 40f);
                scale *= 1f + 0.12f * warn;
            }

            Color glow = Color.Lerp(ShroomerangTuning.GlowColor, new Color(230, 245, 255, 0), warn);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, glow * (0.3f + 0.2f * pulse), Projectile.rotation, origin, scale * 1.7f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPosition, null, glow * (0.5f + 0.2f * pulse), Projectile.rotation, origin, scale * 1.3f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(lightColor, Color.White, 0.8f), Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}