using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// The held Blade of Grass: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing machinery: the
    /// arm and sword pose, the chained up and down swings at the cursor, the swept hit test, the trail) with the Blade of Grass's numbers,
    /// sounds and trail plugged in. Its "charge" is the length of the vines (BladeOfGrassPlayer): the first enemy each swing touches (a true
    /// melee hit) adds BladeOfGrassTuning.LengthPerHitPx to every vine. There is no stab and no speed-up.
    /// The three vines are a BladeOfGrassVineSet that grows from the blade's edge and floats around the player, searching; it loops
    /// enemies in reach and the looped sections break off as BladeOfGrassSpineProjectile. The vines are drawn BEFORE the base draws the
    /// trail and the sword, so where they start on the blade the sword is in front of them.
    /// MULTIPLAYER: the owner's client decides everything (the click, the hits, the lengths, the targets, the break-offs, the damage). The
    /// base syncs the swing; after its data SendExtraAI adds, per vine, the target enemy and the length (sent whenever a target or a length
    /// changes). Every machine runs the same searching motion from the projectile's identity and a clock (visual, never synced) and
    /// the rope physics itself is visual too. A section that breaks off is a normal projectile spawned by the owner. No custom packets.
    /// </summary>
    public class BladeOfGrassBladeProjectile : ChargedBladeProjectile
    {
        // The vanilla item sprite is the sword (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BladeofGrass;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new BladeOfGrassBladeTune());

        protected override int ItemType => ItemID.BladeofGrass;

        readonly BladeOfGrassVineSet vines = new BladeOfGrassVineSet();

        protected override float OwnerCharge(Player owner)
        {
            return BladeOfGrassPlayer.ChargeOf(owner);
        }

        protected override void AddOwnerCharge(Player owner, float amount)
        {
            BladeOfGrassPlayer.AddCharge(owner, amount);
        }

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new BladeOfGrassSweepTrailStyle();
        }

        // No stab: the blade never counts as fully charged (ChargedAt is above 1), these are only here because the shared blade asks for them.
        protected override bool StabAvailable(Player owner)
        {
            return false;
        }

        protected override void OnStabImpacted(Player owner) { }

        protected override void FxPlaySwing(Vector2 position, float charge) { BladeOfGrassFx.PlaySwing(position, charge); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { BladeOfGrassFx.SwingDust(tip, motion, charge); }
        protected override void FxHitSpark(Vector2 position, float charge) { BladeOfGrassFx.HitSpark(position, charge); }
        protected override void FxChargeHit(Vector2 tip) { BladeOfGrassFx.ChargeHit(tip); }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { }
        protected override void FxPlayStabWindup(Vector2 position) { }
        protected override void FxStabImpact(Vector2 chest) { }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { }

        // ---------------------------------------------------------------- sync

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            bool mine = Projectile.owner == Main.myPlayer;
            vines.Write(writer, mine ? BladeOfGrassPlayer.LengthsOf(Main.player[Projectile.owner]) : null);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            vines.Read(reader);
        }

        // ---------------------------------------------------------------- the vines

        // Every machine, every tick (not on a dedicated server), right after the pose is set: the vines grow from the blade's edge.
        protected override void TickVisuals(ChargedBladePose pose)
        {
            Player owner = Main.player[Projectile.owner];
            bool mine = Projectile.owner == Main.myPlayer;
            Vector2 hand = pose.Hand;
            Vector2 direction = pose.Direction;
            float bladeLength = BladeOfGrassTuning.BladeLength * pose.Scale;

            // The roots sit in the middle of the player, not on the blade: the vines grow from the body and float around it.
            Vector2 Root(int index)
            {
                return owner.MountedCenter;
            }

            vines.Tick(Projectile, owner.MountedCenter, Root, pose.Charge, mine ? BladeOfGrassPlayer.LengthsOf(owner) : null);
            if (mine && vines.TakeSync())
                Projectile.netUpdate = true;

            // Leaves fly off a blade with long vines.
            if (pose.Charge > 0.2f && Main.rand.NextFloat() < pose.Charge * BladeOfGrassTuning.ChargeLeafChance)
                BladeOfGrassFx.ChargeLeaf(hand + direction * (bladeLength * Main.rand.NextFloat()), pose.Charge);
            Lighting.AddLight(hand + direction * (bladeLength * 0.5f), 0.1f + 0.2f * pose.Charge, 0.3f + 0.3f * pose.Charge, 0.08f);
        }

        // ---------------------------------------------------------------- drawing

        // The vines are drawn by BladeOfGrassVineLayer (a player draw layer before the body, so they are behind the player); this projectile
        // draws the trail and the sword. The layer asks for the sprites here.
        public void AddVineDrawData(ref PlayerDrawSet drawInfo)
        {
            vines.AddDrawData(ref drawInfo);
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            vines.Release();
        }
    }
}