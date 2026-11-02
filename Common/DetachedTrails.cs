using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Keeps PixelTrails and AfterimageTrails alive after their projectile has died, so they go on fading (and on running their style's Update) instead of
    /// vanishing with it. A projectile calls DetachedTrails.Hand(trail) from OnKill, on every machine. Client only, nothing is synced.
    /// The leftover trails are drawn after the tiles, so they sit behind enemies and projectiles.
    /// </summary>
    public class DetachedTrails : ModSystem
    {
        readonly List<PixelTrail> trails = new List<PixelTrail>();
        readonly List<AfterimageTrail> afterimages = new List<AfterimageTrail>();
        readonly List<SweepTrail> sweeps = new List<SweepTrail>();

        static int MaxTrails => 64;

        public static void Hand(PixelTrail trail)
        {
            if (Main.dedServ || trail == null || trail.IsEmpty)
                return;

            DetachedTrails system = ModContent.GetInstance<DetachedTrails>();
            system.trails.Add(trail);
            if (system.trails.Count > MaxTrails)
                system.trails.RemoveAt(0);
        }

        // Same for a held sword's sweep trail (the expanding fan): it keeps fading on its own after the swing projectile has died.
        public static void Hand(SweepTrail trail)
        {
            if (Main.dedServ || trail == null || trail.IsEmpty)
                return;

            DetachedTrails system = ModContent.GetInstance<DetachedTrails>();
            system.sweeps.Add(trail);
            if (system.sweeps.Count > MaxTrails)
                system.sweeps.RemoveAt(0);
        }

        // Same for a projectile's afterimages: they keep fading on their own after it has died.
        public static void Hand(AfterimageTrail trail)
        {
            if (Main.dedServ || trail == null || trail.IsEmpty)
                return;

            DetachedTrails system = ModContent.GetInstance<DetachedTrails>();
            system.afterimages.Add(trail);
            if (system.afterimages.Count > MaxTrails)
                system.afterimages.RemoveAt(0);
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu || Main.gamePaused)
                return;

            for (int i = trails.Count - 1; i >= 0; i--)
            {
                trails[i].Tick();
                if (trails[i].IsEmpty)
                    trails.RemoveAt(i);
            }

            for (int i = sweeps.Count - 1; i >= 0; i--)
            {
                sweeps[i].Tick();
                if (sweeps[i].IsEmpty)
                    sweeps.RemoveAt(i);
            }

            for (int i = afterimages.Count - 1; i >= 0; i--)
            {
                afterimages[i].Tick();
                if (afterimages[i].IsEmpty)
                    afterimages.RemoveAt(i);
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ || (trails.Count == 0 && afterimages.Count == 0 && sweeps.Count == 0))
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < afterimages.Count; i++)
                afterimages[i].Draw();
            for (int i = 0; i < sweeps.Count; i++)
                sweeps[i].Draw();
            for (int i = 0; i < trails.Count; i++)
                trails[i].Draw();
            Main.spriteBatch.End();
        }

        public override void OnWorldUnload()
        {
            trails.Clear();
            afterimages.Clear();
            sweeps.Clear();
        }

        public override void Unload()
        {
            trails.Clear();
            afterimages.Clear();
            sweeps.Clear();
        }
    }
}