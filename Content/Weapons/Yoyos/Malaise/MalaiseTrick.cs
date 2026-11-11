using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Malaise
{
    /// <summary>
    /// The Malaise's trick. Patch M1: the shared Man on Trapeze (ManOnTrapezeTrick) with the Malaise's ending: when the loop is let go
    /// (it closed on the enemy, or broke because the enemy got out of reach) the enemy is Confused for ConfusedTicks. Rock the Baby (the
    /// second enemy, the triangle, the pendulum) comes in M2 on top of this; Man on Trapeze is its fallback when no second enemy is in range.
    /// MULTIPLAYER: the owner's client applies the buff (and sends it to the server, which tells everyone); the loop itself is synced by
    /// LoopYoyoProjectile as for the Artery.
    /// </summary>
    public class MalaiseTrick : ManOnTrapezeTrick
    {
        public MalaiseTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        MalaiseTuning Tune => (MalaiseTuning)T;

        protected override void OnLoopEnd(YoyoProjectile yoyo, NPC target, Vector2 at, bool closed)
        {
            if (target == null || !target.active)
                return;
            int ticks = Tune.ConfusedTicks;
            target.AddBuff(BuffID.Confused, ticks);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.AddNPCBuff, -1, -1, null, target.whoAmI, BuffID.Confused, ticks);
        }
    }
}