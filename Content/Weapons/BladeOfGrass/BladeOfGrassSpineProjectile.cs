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
    /// The section of vine that broke off and coiled around an enemy. It sits on the enemy, grows spines (the smooth Ivy Whip vine turns into the
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
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ThornHook;

        int age;
        bool detached; // the enemy is gone: stop drawing
        Vector2[] coil;
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> thornChainAsset;
        Asset<Texture2D> vineHeadAsset;
        Asset<Texture2D> thornHeadAsset;
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
            thornChainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + BladeOfGrassTuning.SpineChainName);
            vineHeadAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.IvyWhip);
            thornHeadAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.ThornHook);
            Texture2D chain = chainAsset.Value;
            Texture2D thornChain = thornChainAsset.Value;
            Texture2D vineHead = vineHeadAsset.Value;
            Texture2D thornHead = thornHeadAsset.Value;

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

            // The coil has a head on EACH end: the Ivy Whip head while it is still smooth, the Thorn Hook head once it is spiny.
            Vector2 coilStart = coil[0];
            Vector2 coilEnd = coil[samples - 1];
            bool hasStart = Vector2.DistanceSquared(coilStart, coil[2]) > 0.01f;
            bool hasEnd = Vector2.DistanceSquared(coilEnd, coil[samples - 3]) > 0.01f;
            float startAngle = (coilStart - coil[2]).ToRotation(); // pointing out of the coil at its start
            float endAngle = (coilEnd - coil[samples - 3]).ToRotation();
            Color startLight = Lighting.GetColor(coilStart.ToTileCoordinates());
            Color endLight = Lighting.GetColor(coilEnd.ToTileCoordinates());
            float trim = BladeOfGrassTuning.ChainEndTrimPx;

            // The smooth vine (Ivy Whip chain) fades out while the spiny one (Thorn Hook chain) fades in; each is one mesh, see DrawStrip.
            BladeOfGrassVineDraw.BeginMesh();
            BladeOfGrassVineDraw.DrawStrip(chain, coil, samples, BladeOfGrassTuning.VineScale, BladeOfGrassTuning.VineScale, 1f - spines, trim, trim);
            BladeOfGrassVineDraw.DrawStrip(thornChain, coil, samples, BladeOfGrassTuning.SpineScale, BladeOfGrassTuning.SpineScale, spines, trim, trim);
            BladeOfGrassVineDraw.EndMesh();

            if (spines < 1f)
            {
                if (hasStart)
                    BladeOfGrassVineDraw.DrawHead(vineHead, coilStart, startAngle, BladeOfGrassTuning.HeadScale, startLight * (1f - spines));
                if (hasEnd)
                    BladeOfGrassVineDraw.DrawHead(vineHead, coilEnd, endAngle, BladeOfGrassTuning.HeadScale, endLight * (1f - spines));
            }
            if (spines > 0f)
            {
                if (hasStart)
                    BladeOfGrassVineDraw.DrawHead(thornHead, coilStart, startAngle, BladeOfGrassTuning.HeadScale, startLight * spines);
                if (hasEnd)
                    BladeOfGrassVineDraw.DrawHead(thornHead, coilEnd, endAngle, BladeOfGrassTuning.HeadScale, endLight * spines);
            }
            return false;
        }
    }
}