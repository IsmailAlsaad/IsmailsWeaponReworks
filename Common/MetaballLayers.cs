using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Owns every MetaballLayer of the mod, ticks them and draws them after the tiles. A weapon never creates a layer by hand:
    ///     MetaballLayers.Of&lt;MyWeaponBlobStyle&gt;().Spawn(position, velocity, lifeTicks, size);   // a particle with a life of its own
    ///     MetaballLayers.Of&lt;MyWeaponBlobStyle&gt;().AddBlob(center, radius, heat);                  // a body for this tick (call every tick)
    /// where MyWeaponBlobStyle is a MetaballStyle subclass (one per look). Client only, nothing is synced.
    /// </summary>
    public class MetaballLayers : ModSystem
    {
        readonly Dictionary<Type, MetaballLayer> layers = new Dictionary<Type, MetaballLayer>();
        int tick;

        public static MetaballLayer Of<TStyle>() where TStyle : MetaballStyle, new()
        {
            MetaballLayers system = ModContent.GetInstance<MetaballLayers>();
            if (!system.layers.TryGetValue(typeof(TStyle), out MetaballLayer layer))
            {
                layer = new MetaballLayer(new TStyle());
                system.layers[typeof(TStyle)] = layer;
            }
            return layer;
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu || Main.gamePaused)
                return;

            tick++;
            foreach (MetaballLayer layer in layers.Values)
                layer.Tick(tick);
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ)
                return;

            foreach (MetaballLayer layer in layers.Values)
                layer.Draw();
        }

        public override void OnWorldUnload()
        {
            foreach (MetaballLayer layer in layers.Values)
                layer.Clear();
        }

        public override void Unload()
        {
            layers.Clear();
        }
    }
}