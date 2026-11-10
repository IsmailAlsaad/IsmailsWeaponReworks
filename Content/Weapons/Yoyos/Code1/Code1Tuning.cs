using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Code1
{
    /// <summary>
    /// Every tunable number of the Code1 (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The shared defaults are in
    /// YoyoTuning (rope, idle, return, body, juice); override any of them here. The numbers of its trick come from IForwardPassTuning.
    /// </summary>
    public class Code1Tuning : YoyoTuning, IForwardPassTuning
    {
        // ---- juice ----
        public override float HitShake => 0.5f;                   // very subtle camera punch per enemy hit (0 = none)
        public override bool Afterimages => true;
        public override int AfterimageTicks => 10;                // how long one afterimage lives
        public override float AfterimageMinScale => 0.7f;         // it shrinks to this fraction while it fades
        public override float AfterimageMinSpeed => 3f;           // none are laid while the yoyo is slower than this
        public override Color AfterimageColor => Color.White * 0.6f; // Color.White = the same sprite, Color.White * 0.5f = slightly faded, a color tints it

        // ---- Forward Pass (left click) ----
        public int ForwardPassTicks => 32;                        // one whole pass (out along the ellipse and back to the hand)
        public float ForwardPassWidthRatio => 0.42f;              // the ellipse's half width as a fraction of its half length
        public float ForwardPassReachMargin => 30f;               // the far end is the string's full length minus this
        public float ForwardPassEase => 0.35f;                    // 0 = constant speed, 1 = slow at both ends
        public bool ForwardPassThroughPlatforms => true;          // platforms and other top-solid blocks do not stop it
    }
}