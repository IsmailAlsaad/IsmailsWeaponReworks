using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Per held blade, visual only (every machine keeps its own). Strength (0..1) is how strong the dark flame look is: it switches on once the
    /// sword has been fully charged, then FOLLOWS the charge while it drains (full at full charge, thinner and smaller as it falls, gone at
    /// NightsEdgeTuning.FlameDieCharge) instead of cutting out at one threshold. Update also returns the blade motion the flames may inherit:
    /// zero for a few ticks after the sword flips to the other side, because the flip is instant and the tip's one-tick jump across the
    /// player would otherwise shoot every new particle sideways at high speed.
    /// </summary>
    public class NightsEdgeFlameState
    {
        public float Strength;
        bool latched;
        bool flipKnown;
        bool lastFlipped;
        int calm;

        public Vector2 Update(ChargedBladePose pose)
        {
            bool flipped = (pose.Effects & SpriteEffects.FlipHorizontally) != 0;
            if (flipKnown && flipped != lastFlipped)
                calm = NightsEdgeTuning.FlipCalmTicks;
            flipKnown = true;
            lastFlipped = flipped;

            if (pose.Outline > 0.5f)
                latched = true; // reached full charge: from now on the charge decides the strength until it is gone
            float target = 0f;
            if (latched)
            {
                float die = MathHelper.Clamp(NightsEdgeTuning.FlameDieCharge, 0f, 0.99f);
                target = MathHelper.Clamp((pose.Charge - die) / (1f - die), 0f, 1f);
            }
            Strength += (target - Strength) * MathHelper.Clamp(NightsEdgeTuning.FlameFollow, 0.01f, 1f);
            if (target <= 0f && Strength < 0.02f)
            {
                Strength = 0f;
                latched = false;
            }

            if (calm > 0)
            {
                calm--;
                return Vector2.Zero;
            }
            return pose.TipMotion;
        }
    }
}