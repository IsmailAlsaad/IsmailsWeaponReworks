using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// A section of barbed vine released by the Amazon's third hit: it coils around the enemy (the same coil as the Blade of Grass vine that
    /// loops over an enemy: the smooth vine turns spiny, a head on each end), pricks ONLY that enemy every LatchPrickTicks, poisons it for as
    /// long as it sits there, and unwinds when its life ends or the enemy dies.
    /// ai[0] = index of the enemy, ai[1] = the section's length in pixels, ai[2] = the coil's start angle.
    /// MULTIPLAYER: spawned by the owner's client (the spawn data carries the three values). Every machine sticks it to its own copy of the
    /// enemy and draws the same coil from those values; damage and poison only run on the owner's client (OnHitNPC / local immunity), the
    /// visual pricks run on every machine from a local tick counter. Nothing is synced beyond the spawn.
    /// </summary>
    public class AmazonBarbedCoilProjectile : ModProjectile
    {
        // The Thorn Hook head sprite as its texture so nothing is missing; the drawing picks the textures itself.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ThornHook;

        AmazonTuning tune;
        AmazonFx fx;
        int age;
        bool detached; // the enemy is gone: stop drawing
        Vector2[] coil;
        Asset<Texture2D> smoothChainAsset;
        Asset<Texture2D> spinyChainAsset;
        Asset<Texture2D> smoothHeadAsset;
        Asset<Texture2D> spinyHeadAsset;

        int EnemyIndex => (int)Projectile.ai[0];
        float SectionLength => Projectile.ai[1];
        float CoilSeed => Projectile.ai[2];

        public override void SetDefaults()
        {
            tune = new AmazonTuning();
            fx = new AmazonFx();
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = tune.LatchLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = tune.LatchPrickTicks;
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

            if (age % Math.Max(1, tune.LatchPrickTicks) == 0)
                fx.LatchTick(npc.Center);
            Lighting.AddLight(npc.Center, 0.1f, 0.3f, 0.08f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            fx.LatchHit(Projectile.Center + Main.rand.NextVector2Circular(target.width * 0.4f, target.height * 0.4f));

            // Poisoned for as long as a vine is latched on: this runs on the owner's client on every damage tick and tops the buff up whenever it
            // would run out before the next tick (so several vines on one enemy do not each send a buff packet; vanilla AddBuff syncs itself).
            int poison = target.FindBuffIndex(BuffID.Poisoned);
            if (poison < 0 || target.buffTime[poison] <= tune.LatchPrickTicks + 2)
                target.AddBuff(BuffID.Poisoned, tune.LatchPoisonTicks);
        }

        public override void OnKill(int timeLeft)
        {
            if (detached || Main.dedServ)
                return;
            int index = EnemyIndex;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                fx.Unwind(Main.npc[index].Center, Main.npc[index].width, Main.npc[index].height);
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

            smoothChainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + tune.SmoothChainName);
            spinyChainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + tune.SpineChainName);
            smoothHeadAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.IvyWhip);
            spinyHeadAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.ThornHook);

            // How much of the section is visible: it winds on over CoilGrowTicks and unwinds over the last LatchFadeTicks.
            float grown = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / (float)Math.Max(1, tune.CoilGrowTicks), 0f, 1f));
            float remaining = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(Projectile.timeLeft / (float)Math.Max(1, tune.LatchFadeTicks), 0f, 1f));
            float visible = grown * remaining;
            if (visible <= 0.01f)
                return false;
            float spines = MathHelper.Clamp(age / (float)Math.Max(1, tune.SpineGrowTicks), 0f, 1f);

            VineCoilStyle style = new VineCoilStyle
            {
                SmoothChain = smoothChainAsset.Value,
                SpinyChain = spinyChainAsset.Value,
                SmoothHead = smoothHeadAsset.Value,
                SpinyHead = spinyHeadAsset.Value,
                SmoothScale = tune.SmoothScale,
                SpinyScale = tune.SpineScale,
                HeadScale = tune.HeadScale,
                HeadForwardPx = tune.HeadForwardPx,
                ChainEndTrimPx = tune.ChainEndTrimPx,
                RadiusScale = tune.CoilRadiusScale,
                RadiusPad = tune.CoilRadiusPad,
                MaxLoops = tune.MaxLoops,
                Drift = tune.CoilDrift,
                Samples = tune.CoilSamples
            };
            VineCoil.Draw(npc, style, ref coil, SectionLength, CoilSeed, age, visible, spines);
            return false;
        }
    }
}