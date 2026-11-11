using Microsoft.Xna.Framework;
using System.IO;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The shared part of a Man on Trapeze yoyo's projectile (the Artery, the Malaise): the machinery is in YoyoProjectile, this carries the
    /// loop's synced data (which enemy, where the loop began, whether it closed). Each yoyo's own projectile names its item, tuning, fx
    /// (a LoopYoyoFx subclass) and trick (a ManOnTrapezeTrick subclass).
    /// MULTIPLAYER: the owner's client decides everything about the loop (which enemy, when it starts, closes or breaks). The data travels
    /// in the extra sync (after the base's): a serial that changes when a loop starts, the enemy's index, where the yoyo was and how fast
    /// it moved at that moment, the closed flag and where it closed. Every machine then runs the same curve from that data
    /// (ManOnTrapezeTrick), so nothing else is synced.
    /// </summary>
    public abstract class LoopYoyoProjectile : YoyoProjectile
    {
        // ---- the loop's data (synced) ----
        public int LoopTarget = -1;       // the enemy's index, -1 = none
        public Vector2 LoopStart;         // where the yoyo was when the string caught the enemy
        public Vector2 LoopStartVel;      // and how fast it moved (the curve starts with this speed, so it does not turn harshly)
        public bool LoopClosed;           // the loop closed on its enemy (the owner decides)
        public Vector2 LoopPoint;         // where it closed
        public int LoopMode;              // 0 = the noose (Man on Trapeze), 1 = Rock the Baby
        public int LoopTarget2 = -1;      // Rock the Baby: the second enemy's index, -1 = none
        public int LoopPhase;             // Rock the Baby: 0 seek, 1 hook, 2 swing, 3 done (the owner decides)
        public int LoopTurn = 1;          // Rock the Baby: which way the string turns round the corners, +1 or -1

        int loopSerial;                   // 0..255, one more for every loop that starts
        int handledSerial;                // the serial this machine has started
        bool loopSynced;
        bool pendingClose;

        // Another machine's loop that this machine has not started yet.
        public bool PendingStart { get; private set; }

        LoopYoyoFx LFx => (LoopYoyoFx)Fx;

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
            writer.Write((byte)LoopMode);
            writer.Write((short)LoopTarget2);
            writer.Write((byte)LoopPhase);
            writer.Write((sbyte)LoopTurn);
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
            LoopMode = reader.ReadByte();
            LoopTarget2 = reader.ReadInt16();
            LoopPhase = reader.ReadByte();
            LoopTurn = reader.ReadSByte();
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
            LoopMode = 0;
            LoopTarget2 = -1;
            LoopPhase = 0;
            LoopTurn = 1;
        }

        // Owner's client: the string caught an enemy.
        public void StartLoop(int target, Vector2 start, Vector2 startVel, int mode = 0, int target2 = -1, int turn = 1)
        {
            LoopTarget = target;
            LoopMode = mode;
            LoopTarget2 = target2;
            LoopTurn = turn;
            LoopPhase = 0;
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
            LFx.LoopClose(at);
            Projectile.netUpdate = true;
        }

        // Owner's client: Rock the Baby moves on to a phase (the other machines follow from the sync).
        public void SetPhase(int phase)
        {
            LoopPhase = phase;
            Projectile.netUpdate = true;
        }

        // Owner's client: Rock the Baby has a new second enemy (the first was lost on the way).
        public void SetSecond(int target2, int turn)
        {
            LoopTarget2 = target2;
            LoopTurn = turn;
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
            LFx.LoopClose(LoopPoint);   // the other machines play what the owner already played
        }
    }
}