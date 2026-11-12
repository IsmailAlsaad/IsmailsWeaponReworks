using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common.Flail
{
    /// <summary>
    /// Every dust, sound and drawing call the shared flail loop makes (FlailProjectile), so each flail can look and sound its own way
    /// (vanilla dust and sounds only). Every method runs on every machine and must return at once on a dedicated server.
    /// </summary>
    public abstract class FlailFx
    {
        public abstract void ChargeGather(Vector2 center, int tier, float progress);   // gathering toward the hand while charging; progress 0 to 1
        public abstract void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress);  // flung off the spinning head
        public abstract void TierComplete(Vector2 head, int tier);                      // the tier's charge is complete
        public abstract void ThrowWhoosh(Vector2 head, int tier);
        public abstract void ApexJingle(Vector2 head, int tier);                         // the head reached the apex of the ellipse
        public abstract void Caught(Vector2 hand, int nextTier);                         // a press while the head is out
        public abstract void Fizzle(Vector2 head);                                       // released before the charge was complete
        public abstract void LockedChain(Vector2 head);
        public abstract void Mistimed(Vector2 head);                                     // too late, or let go early after a good catch
        public abstract void CatchHead(Vector2 hand);                                    // the head got home
        public abstract void FlightDust(Vector2 position, Vector2 velocity, int tier);
        public abstract void SpecialLaunch(Vector2 head);                                // the tier 3 release
        public abstract void HitSparks(Vector2 position, int tier);
        public abstract void DrawLayers();                                               // metaball layers drawn behind the head (nothing if the flail has none)
    }
}