using IsmailsWeaponReworks.Common;
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

            // Poisoned for as long as a coil is latched on: this runs on the owner's client on every damage tick and tops the buff up whenever it
            // would run out before the next tick (so several coils on one enemy do not each send a buff packet; vanilla AddBuff syncs itself).
            int poison = target.FindBuffIndex(BuffID.Poisoned);
            if (poison < 0 || target.buffTime[poison] <= BladeOfGrassTuning.HitIntervalTicks + 2)
                target.AddBuff(BuffID.Poisoned, BladeOfGrassTuning.PoisonTicks);
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

            VineCoilStyle style = new VineCoilStyle
            {
                SmoothChain = chain,
                SpinyChain = thornChain,
                SmoothHead = vineHead,
                SpinyHead = thornHead,
                SmoothScale = BladeOfGrassTuning.VineScale,
                SpinyScale = BladeOfGrassTuning.SpineScale,
                HeadScale = BladeOfGrassTuning.HeadScale,
                HeadForwardPx = BladeOfGrassTuning.HeadForwardPx,
                ChainEndTrimPx = BladeOfGrassTuning.ChainEndTrimPx,
                RadiusScale = BladeOfGrassTuning.CoilRadiusScale,
                RadiusPad = BladeOfGrassTuning.CoilRadiusPad,
                MaxLoops = BladeOfGrassTuning.MaxLoops,
                Drift = BladeOfGrassTuning.CoilDrift,
                Samples = BladeOfGrassTuning.CoilSamples
            };
            VineCoil.Draw(npc, style, ref coil, SectionLength, CoilSeed, age, visible, spines);
            return false;
        }
    }
}