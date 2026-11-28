using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// The held Candy Cane Sword: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing machinery), used as the
    /// plain swing of the Light's Bane WITHOUT any charge, speed-up or stab, exactly as the wooden and ore swords use it (the swing numbers come from
    /// WoodenSwordTuning through CandyCaneSwordBladeTune), with a faint red trail. The sprite is the vanilla item sprite and the length is measured from it.
    /// Every true melee hit rolls CandyCaneSwordTuning.SpawnChance (1/4) for a candy of the sword's current color (CandyCaneSwordPlayer.NextCandy; after it spawns the next color is rolled and the sword, its trail and its sparks change to it) that pops out of the enemy; a hit that KILLS the enemy shoots a
    /// fan of candies upward instead (CandyProjectile.SpawnOne / SpawnBurst).
    /// MULTIPLAYER: the owner's client decides everything (the click, the hits, the roll, what spawns); the base syncs the swing. The candies are normal
    /// projectiles (see CandyProjectile for their sync and the pickup packets).
    /// </summary>
    public class CandyCaneSwordBladeProjectile : ChargedBladeProjectile
    {
        // A placeholder so the mod loads; the real sprite is the held item's (SpriteAsset).
        public override string Texture => "Terraria/Images/Item_" + ItemID.CandyCaneSword;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new CandyCaneSwordBladeTune(ItemID.CandyCaneSword));

        protected override int ItemType => ItemID.CandyCaneSword;

        protected override Asset<Texture2D> SpriteAsset => TextureAssets.Item[ItemID.CandyCaneSword];

        // Ismail's CandyCaneSword.png: the sword in the color of the next candy (the vanilla sprite until the file exists).
        protected override Texture2D SpriteOverride => CandyCaneSwordSprites.Cell(NextKind);

        // The owner's next candy color (every machine mirrors it, see CandyCaneSwordPlayer).
        CandyKind NextKind => Main.player[Projectile.owner].GetModPlayer<CandyCaneSwordPlayer>().NextCandy;

        public override void OnSpawn(IEntitySource source)
        {
            tune = null;
            if (!Main.dedServ)
                Main.instance.LoadItem(ItemID.CandyCaneSword);
        }

        protected override float OwnerCharge(Player owner) { return 0f; }

        protected override void AddOwnerCharge(Player owner, float amount) { }

        protected override bool DrawsTrail => true;

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new CandyCaneSwordSweepTrailStyle(() => NextKind);
        }

        // No stab: the blade never counts as fully charged (ChargedAt is above 1), these are only here because the shared blade asks for them.
        protected override bool StabAvailable(Player owner)
        {
            return false;
        }

        protected override void OnStabImpacted(Player owner) { }

        protected override void FxPlaySwing(Vector2 position, float charge) { CandyCaneSwordFx.PlaySwing(position); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { }
        protected override void FxChargeHit(Vector2 tip) { }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { }
        protected override void FxPlayStabWindup(Vector2 position) { }
        protected override void FxStabImpact(Vector2 chest) { }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { }

        // Every machine, for every hit it sees: a few sparks in the sword's color.
        protected override void FxHitSpark(Vector2 position, float charge) { CandyCaneSwordFx.HitSpark(position, NextKind); }

        // Owner's client, for every true melee hit (the damage is already done): a kill shoots candies up, any other hit has a 1/4 chance for one.
        protected override void OnOwnerHit(NPC target, NPC.HitInfo hit, bool fullBefore)
        {
            Player owner = Main.player[Projectile.owner];
            int damage = owner.GetWeaponDamage(owner.HeldItem);
            bool killed = !target.active || target.life <= 0;

            if (killed)
            {
                CandyProjectile.SpawnBurst(Projectile.GetSource_FromThis(), owner, target.Center, damage);
                return;
            }
            if (Main.rand.NextFloat() >= CandyCaneSwordTuning.SpawnChance)
                return;

            Vector2 hand = Projectile.Center;
            Vector2 tip = hand + Projectile.rotation.ToRotationVector2() * (Tune.BladeLength * Projectile.scale * Tune.HitLengthScale);
            Vector2 point = Contact.EntryPoint(hand, tip, target.Hitbox);
            CandyCaneSwordPlayer state = owner.GetModPlayer<CandyCaneSwordPlayer>();
            CandyProjectile.SpawnOne(Projectile.GetSource_FromThis(), owner, point, damage, state.NextCandy, target.whoAmI);
            state.NextCandy = CandyKinds.Pick(owner); // rolls again: the sword, its trail and its sparks take the new color
        }
    }
}