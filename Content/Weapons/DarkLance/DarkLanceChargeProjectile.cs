using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The held Dark Lance while the right button is held: the lance points straight up from the raised hand and trembles harder with
    /// every orb, dark energy streams into its tip, and every DarkLanceTuning.ChargeIntervalTicks it lets one dark flame orb go
    /// (DarkLanceOrbProjectile) until MaxOrbs are alive. Releasing ends the charge; the orbs stay.
    /// SPAWN VALUES (the GlobalItem's Shoot passes them): ai[0] = 0, ai[1] = 0 (the owner writes the end age + 1 here when the button
    /// is released).
    /// MULTIPLAYER: the owner's client decides everything (the click, the release, every orb spawn). The pose, tremble, dust and glow are
    /// pure functions of the projectile's age and the synced end age, so every machine shows the same charge with no per-tick packets;
    /// only the release travels (ai[1] + netUpdate). Each orb is a normal spawned projectile (synced by tModLoader). The charge itself
    /// deals no damage.
    /// </summary>
    public class DarkLanceChargeProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite.
        public override string Texture => "Terraria/Images/Item_" + ItemID.DarkLance;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int EndAge => (int)Projectile.ai[1] - 1;   // -1 = no release yet

        // Per-machine state: counters and visuals, never synced.
        int age;
        float charge;      // 0 to 1: how charged the lance looks
        Vector2 butt;
        Vector2 tip;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;               // kept alive by the AI, ended by the AI
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.DarkLance)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            if (IsOwner)
                owner.ChangeDir(Main.MouseWorld.X >= owner.Center.X ? 1 : -1); // the player faces the cursor while charging

            int orbs = owner.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceOrbProjectile>()];
            int interval = Math.Max(1, DarkLanceTuning.ChargeIntervalTicks);
            float cycle = orbs >= DarkLanceTuning.MaxOrbs ? 1f : (age % interval) / (float)interval;
            charge = MathHelper.Clamp((orbs + cycle) / Math.Max(1, DarkLanceTuning.MaxOrbs), 0f, 1f);
            float wind = 1f;
            if (EndAge >= 0)
                wind = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((EndAge - age) / (float)Math.Max(1, DarkLanceTuning.ChargeWindDownTicks), 0f, 1f));

            // The pose: the lance points up from the raised hand, leaning a little toward the facing side, and trembles with the charge.
            Projectile.scale = DarkLanceTuning.LanceScale * owner.GetAdjustedItemScale(owner.HeldItem);
            Item sample = ContentSamples.ItemsByType[ItemID.DarkLance];
            float shaft = new Vector2(sample.width, sample.height).Length() * Projectile.scale;
            float aim = -MathHelper.PiOver2 + DarkLanceTuning.LanceTilt * owner.direction; // in the player's own frame
            Vector2 direction = HeldAim.ToWorldDirection(owner, aim);
            Vector2 hand = owner.RotatedRelativePoint(owner.MountedCenter, false, true);
            Vector2 tremble = Main.dedServ ? Vector2.Zero : Main.rand.NextVector2Circular(1f, 1f) * (DarkLanceTuning.TremblePerOrb * orbs * wind);
            butt = hand + new Vector2(owner.direction * DarkLanceTuning.HandOffsetX, DarkLanceTuning.HandOffsetY * owner.gravDir) + tremble;
            tip = butt + direction * shaft;
            Projectile.Center = owner.MountedCenter;
            Projectile.rotation = direction.ToRotation();
            Projectile.spriteDirection = owner.direction;
            owner.heldProj = Projectile.whoAmI;     // also ends the projectile when the player swaps items
            owner.SetDummyItemTime(2);              // keeps the item "in use" for as long as the charge lasts (the same way the Rotted Fork spin does)
            owner.itemRotation = -DarkLanceTuning.ArmRaise * owner.direction; // vanilla picks the raised arm frame from this

            if (IsOwner)
            {
                if (EndAge < 0 && !owner.controlUseTile)
                {
                    Projectile.ai[1] = age + DarkLanceTuning.ChargeWindDownTicks + 1f; // the release: every machine ends at this age
                    Projectile.netUpdate = true;
                }
                else if (EndAge < 0 && age > 0 && age % interval == 0 && orbs < DarkLanceTuning.MaxOrbs)
                {
                    // ai[1] = the spawn serial: it gives the orb its stable place in the ring on every machine.
                    DarkLancePlayer plr = owner.GetModPlayer<DarkLancePlayer>();
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, Vector2.Zero, ModContent.ProjectileType<DarkLanceOrbProjectile>(), 0, 0f, owner.whoAmI, 0f, plr.Serial++);
                }
            }

            if (EndAge >= 0 && age >= EndAge)
            {
                owner.itemAnimation = 0; // the next use may start as soon as the cooldown is over
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ)
            {
                if (age == 0)
                    DarkLanceFx.ChargeStart(owner.Center);
                if (EndAge < 0 || wind > 0.3f)
                    DarkLanceFx.Gather(tip, charge * wind);
                Lighting.AddLight(tip, DarkLanceTuning.Palette.Mid.ToVector3() * (0.4f + 0.8f * charge) * wind);
            }

            age++;
        }

        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
                Owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemID.DarkLance, DarkLanceTuning.ChargeCooldownTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = DarkLanceTuning.Palette;
            float glow = 0.25f + 0.75f * charge;
            float scale = Projectile.scale;

            SpearPose.DrawSprite(texture, butt, Projectile.rotation, Projectile.spriteDirection, scale * 1.18f, palette.Halo * (0.45f * glow));
            SpearPose.DrawSprite(texture, butt, Projectile.rotation, Projectile.spriteDirection, scale * 1.07f, palette.Mid * (0.5f * glow));
            SpearPose.DrawSprite(texture, butt, Projectile.rotation, Projectile.spriteDirection, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow));

            // A dark glow gathering at the tip: it swells with the charge and pulses, with a smooth falloff (no hard squares).
            float pulse = 1f + 0.15f * (float)Math.Sin(age * 0.4f);
            float radius = MathHelper.Lerp(DarkLanceTuning.TipGlowMinRadius, DarkLanceTuning.TipGlowMaxRadius, charge) * pulse;
            DarkLanceFx.DrawGlow(tip, radius, DarkLanceTuning.TipGlowStrength * (0.6f + 0.4f * charge));
            return false;
        }
    }
}