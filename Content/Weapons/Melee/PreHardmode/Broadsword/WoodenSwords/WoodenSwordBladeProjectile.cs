using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// The held wooden sword: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing machinery: the arm and
    /// sword pose, the chained up and down swings at the cursor, the swept hit test, the trail), used as the plain swing of the Light's Bane
    /// WITHOUT any charge, speed-up or stab. One class serves all eight swords: the sword is whatever wooden sword the owner holds when it
    /// spawns, its sprite is the vanilla item sprite and its length is measured from that sprite.
    /// Every true melee hit of a swing (the first enemy each swing touches counts, like every shared blade hit: OnHitNPC) lodges a splinter
    /// in the enemy at the point where the blade touched it (SplinterProjectile.Attach).
    /// MULTIPLAYER: the owner's client decides everything (the click, the hits, the splinters). The base syncs the swing; the sword in
    /// the hand is known to every machine from the owner's held item. No custom packets.
    /// </summary>
    public class WoodenSwordBladeProjectile : ChargedBladeProjectile
    {
        // A placeholder so the mod loads; the real sprite is the held item's (SpriteAsset).
        public override string Texture => "Terraria/Images/Item_" + ItemID.WoodenSword;

        int woodItem = ItemID.WoodenSword;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new WoodenSwordBladeTune(woodItem));

        protected override int ItemType => woodItem;

        protected override Asset<Texture2D> SpriteAsset => TextureAssets.Item[woodItem];

        public override void OnSpawn(IEntitySource source)
        {
            int held = Main.player[Projectile.owner].HeldItem.type;
            if (WoodenSwordWoods.ColumnOf(held) >= 0)
                woodItem = held;
            tune = null;
            if (!Main.dedServ)
                Main.instance.LoadItem(woodItem);
        }

        protected override float OwnerCharge(Player owner) { return 0f; }

        protected override void AddOwnerCharge(Player owner, float amount) { }

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new WoodenSwordSweepTrailStyle(WoodenSwordWoods.ColumnOf(woodItem));
        }

        // No stab: the blade never counts as fully charged (ChargedAt is above 1), these are only here because the shared blade asks for them.
        protected override bool StabAvailable(Player owner)
        {
            return false;
        }

        protected override void OnStabImpacted(Player owner) { }

        protected override void FxPlaySwing(Vector2 position, float charge) { WoodenSwordFx.PlaySwing(position); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { WoodenSwordFx.SwingDust(tip, motion); }
        protected override void FxHitSpark(Vector2 position, float charge) { WoodenSwordFx.HitSpark(position); }
        protected override void FxChargeHit(Vector2 tip) { }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { }
        protected override void FxPlayStabWindup(Vector2 position) { }
        protected override void FxStabImpact(Vector2 chest) { }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { }

        // Owner's client, for every true melee hit: a splinter lodges where the blade touched the enemy.
        protected override void OnOwnerHit(NPC target, NPC.HitInfo hit, bool fullBefore)
        {
            if (!target.active || target.life <= 0)
                return;
            Vector2 hand = Projectile.Center;
            Vector2 tip = hand + Projectile.rotation.ToRotationVector2() * (Tune.BladeLength * Projectile.scale * Tune.HitLengthScale);
            Vector2 point = Contact.EntryPoint(hand, tip, target.Hitbox);
            SplinterProjectile.Attach(Projectile.GetSource_FromThis(), Main.player[Projectile.owner], target, point, WoodenSwordWoods.ColumnOf(woodItem));
        }
    }
}