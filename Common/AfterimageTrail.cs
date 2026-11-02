using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// One afterimage: where the projectile was and how it looked at that moment. Painters read these fields; for a sprite afterimage
    /// Scale is the sprite scale, for a custom shape they can mean whatever the painter needs (the Ice Blade wave stores the crescent's
    /// radius in Scale and its thickness in Width). A Color with alpha 0 draws additively (glow).
    /// </summary>
    public struct Afterimage
    {
        public Vector2 Position;
        public float Rotation;
        public float Scale;
        public float Width;
        public Color Color;
        public int Birth;
    }

    /// <summary>
    /// The shared afterimage renderer (never loop over Projectile.oldPos in a PreDraw again). A projectile keeps one AfterimageTrail,
    /// and every tick lays one afterimage with Add; each afterimage carries its OWN age and fades out over lifeTicks, so the trail does
    /// not depend on its projectile: in OnKill the projectile hands the trail to DetachedTrails.Hand, which keeps ticking and drawing
    /// it, and the leftover afterimages fade out on their own instead of vanishing with the projectile (same idea as PixelTrail).
    ///
    /// Use: create it lazily on the client (AfterimageTrail.Sprite for the projectile's own sprite, or the constructor with your own
    /// painter), then in PostAI: Tick() and then Add(...) (skip Add while the projectile is hidden); in PreDraw: Draw() before the
    /// body; in OnKill: DetachedTrails.Hand(trail) and set the field to null. Purely visual, never synced, client only.
    /// </summary>
    public class AfterimageTrail
    {
        readonly List<Afterimage> images = new List<Afterimage>();
        readonly Action<Afterimage, float> painter;
        readonly int lifeTicks;
        int clock;

        // painter(image, age): age runs from 0 (just laid) to 1 (about to vanish).
        public AfterimageTrail(int lifeTicks, Action<Afterimage, float> painter)
        {
            this.lifeTicks = Math.Max(1, lifeTicks);
            this.painter = painter;
        }

        /// <summary>A trail of the projectile's own sprite. The color you Add is the brightest look; it fades to nothing with age, and
        /// the scale shrinks from its full value down to minScale (1 = no shrinking) over the lifetime.</summary>
        public static AfterimageTrail Sprite(Texture2D texture, int lifeTicks, float minScale)
        {
            Vector2 origin = texture.Size() * 0.5f;
            return new AfterimageTrail(lifeTicks, (image, age) =>
            {
                float fade = 1f - age;
                Main.EntitySpriteDraw(texture, image.Position - Main.screenPosition, null, image.Color * fade, image.Rotation, origin,
                    image.Scale * (minScale + (1f - minScale) * fade), SpriteEffects.None, 0);
            });
        }

        public bool IsEmpty => images.Count == 0;

        public void Add(Vector2 position, float rotation, float scale, Color color, float width = 0f)
        {
            images.Add(new Afterimage { Position = position, Rotation = rotation, Scale = scale, Width = width, Color = color, Birth = clock });
        }

        public void Tick()
        {
            clock++;
            int expired = 0;
            while (expired < images.Count && clock - images[expired].Birth > lifeTicks)
                expired++;
            if (expired > 0)
                images.RemoveRange(0, expired);
        }

        // Oldest first, so the newest ends up on top.
        public void Draw()
        {
            if (Main.dedServ || painter == null)
                return;
            for (int i = 0; i < images.Count; i++)
                painter(images[i], MathHelper.Clamp((clock - images[i].Birth) / (float)lifeTicks, 0f, 1f));
        }
    }
}