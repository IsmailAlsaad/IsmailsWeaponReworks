using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// Every tunable number of the Rally (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The shared defaults are in
    /// YoyoTuning (rope, idle, return, body, juice); override any of them here. The numbers of its trick come from IForwardPassTuning.
    /// </summary>
    public class RallyTuning : YoyoTuning, IForwardPassTuning
    {
        // His own art, when it exists (until then: the vanilla sprite, and cracks drawn by code). Same size as the vanilla sprite.
        public override string TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Rally/RallyProjectile";
        public string Crack1TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Rally/RallyProjectileCrack1";   // the yoyo after the first hit
        public string Crack2TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Rally/RallyProjectileCrack2";   // after the second hit
        public string ShardTexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Rally/RallyShellShard";          // about 10x10; until it exists a quarter of the vanilla sprite

        // ---- juice (the Code 1 values) ----
        public override float HitShake => 0.5f;                   // very subtle camera punch per enemy hit (0 = none)
        public override bool Afterimages => true;
        public override int AfterimageTicks => 10;
        public override float AfterimageMinScale => 0.7f;
        public override float AfterimageMinSpeed => 3f;
        public override Color AfterimageColor => Color.White * 0.2f;

        // ---- Forward Pass (left click) ----
        public int ForwardPassTicks => 32;                        // one whole pass (out along the ellipse and back to the hand)
        public float ForwardPassWidthRatio => 0.42f;              // the ellipse's half width as a fraction of its half length
        public float ForwardPassReachMargin => 30f;               // the far end is the string's full length minus this
        public float ForwardPassEase => 0.35f;                    // 0 = constant speed, 1 = slow at both ends
        public bool ForwardPassThroughPlatforms => true;          // platforms and other top-solid blocks do not stop it

        // ---- the cracks ----
        public int CrackHitsToBreak => 3;                         // hits per cycle: the last one breaks the shell, the ones before crack it
        public float BreakShake => 1.8f;                          // the camera punch when the shell breaks (0 = none)

        // ---- the shell shards ----
        public int ShellShardCount => 4;                          // pieces the shell breaks into
        public float ShardDamageFactor => 0.5f;                   // a shard's damage as a fraction of the yoyo's
        public int ShardMaxHits => 3;                             // enemies one shard pierces before it breaks
        public float ShardLaunchSpeedMin => 2.5f;                 // how fast the pieces fly outward
        public float ShardLaunchSpeedMax => 5f;
        public float ShardLaunchUp => 2.5f;                       // extra kick away from the feet side, so they arc before falling
        public float ShardInheritVelocity => 0.3f;                // the part of the yoyo's own speed the pieces keep
        public float ShardGravity => 0.3f;
        public float ShardMaxFallSpeed => 11f;
        public float ShardAirDrag => 0.995f;                      // horizontal speed kept per tick in the air
        public float ShardBounceDamp => 0.38f;                    // speed kept on a bounce (low = subtle)
        public float ShardRestSpeed => 1.2f;                      // a landing slower than this settles instead of bouncing
        public float ShardGroundFriction => 0.8f;                 // horizontal speed kept per landing tick while it settles
        public float ShardSpin => 0.12f;                          // radians per tick, plus a little per pixel of speed
        public int ShardLifeTicks => 480;                         // a shard that hit nothing fades after this long
        public int ShardFadeTicks => 40;                          // ... fading over the last this-many ticks
        public int ShardSize => 12;                               // hitbox
    }
}