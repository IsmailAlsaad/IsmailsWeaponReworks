using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>The Sunfury's effects for the shared flail loop: every call forwards to SunfuryFx (the Sunfury's dust, sounds and fire metaballs).</summary>
    public class SunfuryFlailFx : FlailFx
    {
        public override void ChargeGather(Vector2 center, int tier, float progress) { SunfuryFx.ChargeGather(center, tier, progress); }
        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress) { SunfuryFx.SpinEmbers(head, tangent, tier, progress); }
        public override void TierComplete(Vector2 head, int tier) { SunfuryFx.TierComplete(head, tier); }
        public override void ThrowWhoosh(Vector2 head, int tier) { SunfuryFx.ThrowWhoosh(head, tier); }
        public override void ApexJingle(Vector2 head, int tier) { SunfuryFx.ApexJingle(head, tier); }
        public override void Caught(Vector2 hand, int nextTier) { SunfuryFx.Caught(hand, nextTier); }
        public override void Fizzle(Vector2 head) { SunfuryFx.Fizzle(head); }
        public override void LockedChain(Vector2 head) { SunfuryFx.LockedChain(head); }
        public override void Mistimed(Vector2 head) { SunfuryFx.Mistimed(head); }
        public override void CatchHead(Vector2 hand) { SunfuryFx.CatchHead(hand); }
        public override void FlightDust(Vector2 position, Vector2 velocity, int tier) { SunfuryFx.FlightDust(position, velocity, tier); }
        public override void SpecialLaunch(Vector2 head) { SunfuryFx.SpecialLaunch(head); }
        public override void HitSparks(Vector2 position, int tier) { SunfuryFx.HitSparks(position, tier); }
        public override void DrawLayers() { SunfuryFx.DrawLayers(); }
    }
}