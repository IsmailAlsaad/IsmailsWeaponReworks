using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>
    /// The held ore broadsword: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing machinery), used
    /// exactly as the wooden swords use it (no charge, no speed-up, no stab, the same swing and idle pose through WoodenSwordTuning), with two
    /// differences: the swing leaves a faint trail in the metal's color, and every hit spawns a small flare (FlareParticleSet) tinted toward
    /// the metal's color at the point where the blade touched the enemy. One class serves all eight swords: the sword is whatever ore
    /// broadsword the owner holds when it spawns, its sprite is the vanilla item sprite and its length is measured from that sprite.
    /// MULTIPLAYER: the owner's client decides everything (the click, the hits, the damage); the base syncs the swing. The flare is visual only:
    /// the owner adds it from OnHitNPC, the other machines from the shared blade's remote hit tracker (both call FxHitSpark), each machine
    /// draws its own copy. No custom packets.
    /// </summary>
    public class OreSwordBladeProjectile : ChargedBladeProjectile
    {
        // A placeholder so the mod loads; the real sprite is the held item's (SpriteAsset).
        public override string Texture => "Terraria/Images/Item_" + ItemID.CopperBroadsword;

        int swordItem = ItemID.CopperBroadsword;
        FlareParticleSet flares;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new OreSwordBladeTune(swordItem));

        protected override int ItemType => swordItem;

        protected override Asset<Texture2D> SpriteAsset => TextureAssets.Item[swordItem];

        public override void OnSpawn(IEntitySource source)
        {
            int held = Main.player[Projectile.owner].HeldItem.type;
            if (OreSwordMetals.ColumnOf(held) >= 0)
                swordItem = held;
            tune = null;
            if (!Main.dedServ)
                Main.instance.LoadItem(swordItem);
        }

        protected override float OwnerCharge(Player owner) { return 0f; }

        protected override void AddOwnerCharge(Player owner, float amount) { }

        protected override bool DrawsTrail => true;

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new OreSwordSweepTrailStyle(OreSwordMetals.ColumnOf(swordItem));
        }

        // No stab: the blade never counts as fully charged (ChargedAt is above 1), these are only here because the shared blade asks for them.
        protected override bool StabAvailable(Player owner)
        {
            return false;
        }

        protected override void OnStabImpacted(Player owner) { }

        protected override void FxPlaySwing(Vector2 position, float charge) { WoodenSwordFx.PlaySwing(position); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { }
        protected override void FxChargeHit(Vector2 tip) { }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { }
        protected override void FxPlayStabWindup(Vector2 position) { }
        protected override void FxStabImpact(Vector2 chest) { }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { }

        // Every machine, for every hit it sees (the owner's real hit, the others' tracked hit): a flare where the blade touched the enemy.
        protected override void FxHitSpark(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;

            OreSpec metal = OreSwordMetals.Spec(OreSwordMetals.ColumnOf(swordItem));
            Color color = Color.Lerp(Color.White, metal.Tint, OreSwordTuning.FlareTint);
            if (flares == null)
                flares = new FlareParticleSet(new OreSwordFlareStyle());
            Vector2 contact = ContactPoint(position);
            flares.Add(contact, color);
            OreSwordFx.PlayHit(contact);
        }

        // The hit position the shared blade hands over is the enemy's center: find that enemy and take the point where the blade line enters its box.
        Vector2 ContactPoint(Vector2 enemyCenter)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.Center != enemyCenter)
                    continue;
                Vector2 hand = Projectile.Center;
                Vector2 tip = hand + Projectile.rotation.ToRotationVector2() * (Tune.BladeLength * Projectile.scale * Tune.HitLengthScale);
                return Contact.EntryPoint(hand, tip, npc.Hitbox);
            }
            return enemyCenter;
        }

        protected override void TickVisuals(ChargedBladePose pose)
        {
            flares?.Tick();
        }

        protected override void DrawAfterBlade(ChargedBladePose pose)
        {
            flares?.Draw();
        }

        // The flares outlive the blade (when the sword is put away): they finish their life on their own.
        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            DetachedFlares.Hand(flares);
            flares = null;
        }
    }
}