using System.IO;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick, and carries
    /// the two numbers Man on trapeze needs on every machine (the lasso's phase and the looped enemy's index) in the existing extra sync.
    /// The vanilla sprite is used until the yoyo's own art exists (its path is in ArteryTuning).
    /// </summary>
    public class ArteryProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrimsonYoyo;

        public override int YoyoItemType => ItemID.CrimsonYoyo;

        public LoopPhase Phase;      // where the lasso is (the owner decides, synced)
        public int LoopNpc = -1;     // the enemy the string looped (the owner decides, synced)

        protected override YoyoTuning CreateTuning()
        {
            return new ArteryTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new ArteryFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new ManOnTrapezeTrick(tuning);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Phase);
            writer.Write((short)LoopNpc);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            Phase = (LoopPhase)reader.ReadByte();
            LoopNpc = reader.ReadInt16();
        }
    }
}