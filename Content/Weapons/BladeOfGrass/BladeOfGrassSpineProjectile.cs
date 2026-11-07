using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>
    /// The section of vine that broke off and coiled around an enemy. It sits on the enemy, grows spines (the smooth vine turns into the
    /// Snapthorn whip's spiny look) and hurts ONLY that enemy every HitIntervalTicks until its life ends, the enemy dies, or the
    /// player's cap of sections evicts it; then the coil unwinds.
    /// ai[0] = index of the enemy, ai[1] = the section's length in pixels, ai[2] = the coil's start angle (random).
    /// MULTIPLAYER: spawned by the owner's client (spawn data carries the three values). Every machine sticks it to its own copy of the
    /// enemy and draws the same coil from those values; damage only runs on the owner's client (OnHitNPC / local immunity), the visual
    /// pricks run on every machine from a local tick counter. Nothing is synced beyond the spawn.
    /// </summary>
    public class BladeOfGrassSpineProjectile : ModProjectile
    {
        // Uses the Snapthorn whip sheet as its texture so nothing is missing; the drawing picks the frames itself.
        public override string Texture => "Terraria/Images/Projectile_" + 841;

        int age;
        bool detached; // the enemy is gone: stop drawing
        Vector2[] coil;
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> whipAsset;
        readonly List<Vector2> samplePositions = new List<Vector2>();
        readonly List<float> sampleAngles = new List<float>();

        int EnemyIndex => (int)Projectile.ai[0];
        float SectionLength => Projectile.ai[1];
        float CoilSeed => Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = BladeOfGrassTuning.SegmentLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BladeOfGrassTuning.HitIntervalTicks;
        }

        // Hurts only the enemy it is coiled around.
        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == EnemyIndex ? (bool?)null : false;
        }

        public override void AI()
        {
            age++;
            bool isOwner = Projectile.owner == Main.myPlayer;
            int index = EnemyIndex;
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active || Main.npc[index].life <= 0)
            {
                detached = true;
                if (isOwner)
                    Projectile.Kill();
                return;
            }
            NPC npc = Main.npc[index];
            Projectile.Center = npc.Center;
            Projectile.velocity = Vector2.Zero;

            if (age % Math.Max(1, BladeOfGrassTuning.HitIntervalTicks) == 0)
                BladeOfGrassFx.SpineTick(npc.Center);
            Lighting.AddLight(npc.Center, 0.1f, 0.3f, 0.08f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BladeOfGrassFx.SpineHit(Projectile.Center + Main.rand.NextVector2Circular(target.width * 0.4f, target.height * 0.4f));
        }

        public override void OnKill(int timeLeft)
        {
            if (detached || Main.dedServ)
                return;
            int index = EnemyIndex;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                BladeOfGrassFx.Unwind(Main.npc[index].Center, Main.npc[index].width, Main.npc[index].height);
        }

        // ---------------------------------------------------------------- drawing

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || detached)
                return false;
            int index = EnemyIndex;
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active)
                return false;
            NPC npc = Main.npc[index];

            chainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + BladeOfGrassTuning.VineChainName);
            whipAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + BladeOfGrassTuning.SpineProjectileId);
            Texture2D chain = chainAsset.Value;
            Texture2D whip = whipAsset.Value;

            // How much of the section is visible: it winds on over CoilGrowTicks and unwinds over the last SegmentFadeTicks.
            float grown = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / (float)Math.Max(1, BladeOfGrassTuning.CoilGrowTicks), 0f, 1f));
            float remaining = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(Projectile.timeLeft / (float)Math.Max(1, BladeOfGrassTuning.SegmentFadeTicks), 0f, 1f));
            float visible = grown * remaining;
            if (visible <= 0.01f)
                return false;
            float spines = MathHelper.Clamp(age / (float)Math.Max(1, BladeOfGrassTuning.SpineGrowTicks), 0f, 1f);

            int samples = Math.Max(8, BladeOfGrassTuning.CoilSamples);
            if (coil == null || coil.Length != samples)
                coil = new Vector2[samples];
            float rx = npc.width * 0.5f * BladeOfGrassTuning.CoilRadiusScale + BladeOfGrassTuning.CoilRadiusPad;
            float ry = npc.height * 0.5f * BladeOfGrassTuning.CoilRadiusScale + BladeOfGrassTuning.CoilRadiusPad;
            float circumference = MathHelper.TwoPi * (rx + ry) * 0.5f;
            float loops = MathHelper.Clamp(SectionLength / Math.Max(1f, circumference), 1f, Math.Max(1f, BladeOfGrassTuning.MaxLoops));
            for (int j = 0; j < samples; j++)
            {
                float u = j / (float)(samples - 1) * visible;
                float angle = CoilSeed + u * loops * MathHelper.TwoPi;
                float wobble = (float)Math.Sin(age * 0.2f + u * 9f) * 2f;
                coil[j] = npc.Center + new Vector2((float)Math.Cos(angle) * (rx + wobble), (float)Math.Sin(angle) * (ry + wobble) * 0.85f + (u - 0.5f) * npc.height * BladeOfGrassTuning.CoilDrift);
            }

            // The smooth vine fades out while the spiny one fades in.
            if (spines < 1f)
            {
                BladeOfGrassVineDraw.Resample(coil, samples, chain.Height * BladeOfGrassTuning.ChainSpacing * BladeOfGrassTuning.VineScale, samplePositions, sampleAngles);
                Vector2 chainOrigin = chain.Size() * 0.5f;
                for (int s = 0; s < samplePositions.Count; s++)
                {
                    Color color = Lighting.GetColor(samplePositions[s].ToTileCoordinates()) * (1f - spines);
                    Main.EntitySpriteDraw(chain, samplePositions[s] - Main.screenPosition, null, color, sampleAngles[s] - MathHelper.PiOver2, chainOrigin, BladeOfGrassTuning.VineScale, SpriteEffects.None, 0);
                }
            }
            if (spines > 0f)
            {
                int frames = Math.Max(2, BladeOfGrassTuning.SpineSheetFrames);
                int frameHeight = whip.Height / frames;
                BladeOfGrassVineDraw.Resample(coil, samples, frameHeight * BladeOfGrassTuning.ChainSpacing * BladeOfGrassTuning.SpineScale, samplePositions, sampleAngles);
                int count = samplePositions.Count;
                for (int s = 0; s < count; s++)
                {
                    // The middle frames cycle along the coil, the last sprite is the tip frame.
                    int frame = s == count - 1 ? frames - 1 : 1 + s % Math.Max(1, frames - 2);
                    Rectangle source = new Rectangle(0, frame * frameHeight, whip.Width, frameHeight);
                    Color color = Lighting.GetColor(samplePositions[s].ToTileCoordinates()) * spines;
                    Main.EntitySpriteDraw(whip, samplePositions[s] - Main.screenPosition, source, color, sampleAngles[s] - MathHelper.PiOver2, new Vector2(source.Width * 0.5f, frameHeight * 0.5f), BladeOfGrassTuning.SpineScale, SpriteEffects.None, 0);
                }
            }
            return false;
        }
    }
}