using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Common.ChargedBlade
{
    /// <summary>
    /// Where the held charged blade is this tick, handed to a weapon's visual hooks (TickVisuals, DrawAfterBlade): the sprite's pose in the
    /// world, the hand and tip, the blade's motion and the charge looks. ToWorld turns a pixel of the sword's texture (or of a mask PNG with the
    /// same canvas) into a world position, exactly as the sprite is drawn, so effects can sit on chosen parts of the sprite.
    /// </summary>
    public struct ChargedBladePose
    {
        public Vector2 SpritePosition;  // world position of the sprite's origin (where it is drawn)
        public float SpriteRotation;
        public Vector2 SpriteOrigin;    // in texture pixels
        public Vector2 SpriteSize;      // the texture's size; zero while the texture is not loaded yet (skip the effect then)
        public float Scale;
        public SpriteEffects Effects;
        public Vector2 Hand;
        public Vector2 Tip;
        public Vector2 TipMotion;       // how far the tip moved this tick
        public Vector2 Direction;       // unit vector along the blade
        public float Charge;            // the drawn charge 0..1
        public float Outline;           // 0..1: how visible the full-charge outline is (0 = not charged, rises and falls smoothly)
        public bool Striking;           // the blade is in the damaging part of a swing
        public int Ticks;               // ticks since the blade formed

        // A texture pixel (x right, y down, in texture pixels) as a world position.
        public Vector2 ToWorld(Vector2 spritePixel)
        {
            bool flipped = (Effects & SpriteEffects.FlipHorizontally) != 0;
            float x = flipped ? SpriteSize.X - spritePixel.X - SpriteOrigin.X : spritePixel.X - SpriteOrigin.X;
            float y = spritePixel.Y - SpriteOrigin.Y;
            return SpritePosition + new Vector2(x, y).RotatedBy(SpriteRotation) * Scale;
        }
    }
}