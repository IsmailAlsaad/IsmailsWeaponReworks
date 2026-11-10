using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick and carries
    /// the loop's synced data (which enemy, where the loop began, whether it closed). The vanilla sprite is used until the yoyo's own art
    /// exists (Tuning.TexturePath).
    /// MULTIPLAYER: the owner's client decides everything about the loop (which enemy, when it starts, closes or breaks). The data travels
    /// in the extra sync (after the base's): a serial that changes when a loop starts, the enemy's index, where the yoyo was and how fast
    /// it moved at that moment, the closed flag and where it closed. Every machine then runs the same curve from that data
    /// (ManOnTrapezeTrick), so nothing else is synced.
    /// </summary>
    public class ArteryProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrimsonYoyo;

        public override int YoyoItemType => ItemID.CrimsonYoyo;

        // ---- the loop's data (synced) ----
        public int LoopTarget = -1;       // the enemy's index, -1 = none
        public Vector2 LoopStart;         // where the yoyo was when the string caught the enemy
        public Vector2 LoopStartVel;      // and how fast it moved (the curve starts with this speed, so it does not turn harshly)
        public bool LoopClosed;           // the loop closed on its enemy (the owner decides)
        public Vector2 LoopPoint;         // where it closed

        int loopSerial;                   // 0..255, one more for every loop that starts
        int handledSerial;                // the serial this machine has started
        bool loopSynced;
        bool pendingClose;

        // Another machine's loop that this machine has not started yet.
        public bool PendingStart { get; private set; }

        ArteryFx AFx => (ArteryFx)Fx;

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

        // ---- net ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)loopSerial);
            writer.Write((short)LoopTarget);
            writer.Write(LoopClosed);
            writer.Write(LoopStart.X);
            writer.Write(LoopStart.Y);
            writer.Write(LoopStartVel.X);
            writer.Write(LoopStartVel.Y);
            writer.Write(LoopPoint.X);
            writer.Write(LoopPoint.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            int serial = reader.ReadByte();
            LoopTarget = reader.ReadInt16();
            bool closed = reader.ReadBoolean();
            LoopStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            LoopStartVel = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            LoopPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (!loopSynced)
            {
                // A late joiner: take it as it is, no replay.
                loopSynced = true;
                loopSerial = serial;
                handledSerial = serial;
                LoopClosed = closed;
                return;
            }
            if (serial != handledSerial && LoopTarget >= 0)
            {
                loopSerial = serial;
                PendingStart = true;
            }
            if (closed && !LoopClosed)
                pendingClose = true;
            LoopClosed = closed;
        }

        // ---- the loop (called by the trick) ----

        // Owner's client: a new throw starts, no loop yet.
        public void ClearLoop()
        {
            LoopTarget = -1;
            LoopClosed = false;
            PendingStart = false;
        }

        // Owner's client: the string caught an enemy.
        public void StartLoop(int target, Vector2 start, Vector2 startVel)
        {
            LoopTarget = target;
            LoopStart = start;
            LoopStartVel = startVel;
            LoopClosed = false;
            loopSerial = (loopSerial + 1) & 255;
            handledSerial = loopSerial;
            Projectile.netUpdate = true;
        }

        // Another machine: the loop the owner started is now running here.
        public void TakePendingStart()
        {
            PendingStart = false;
            handledSerial = loopSerial;
        }

        // Owner's client: the loop closed on its enemy.
        public void CloseLoop(Vector2 at)
        {
            LoopClosed = true;
            LoopPoint = at;
            AFx.LoopClose(at);
            Projectile.netUpdate = true;
        }

        // Owner's client: the loop broke (the enemy got out of reach): nothing closes.
        public void BreakLoop()
        {
            LoopTarget = -1;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            base.AI();
            if (Main.dedServ || !pendingClose)
                return;
            pendingClose = false;
            AFx.LoopClose(LoopPoint);   // the other machines play what the owner already played
        }
    }
}