using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The ethereal outline of the swinging blade. It does not redraw the sword itself: right after the vanilla held-item layer it finds the
    /// sword's own draw entry and puts eight copies of it (a pixel or two to every side, additive blue) just BEHIND it, so the outline
    /// follows the vanilla swing exactly. The stage (1 or 2, synced) decides how wide and how bright it is; it pulses. Visual only: every
    /// machine draws the outline of every player that swings an Enchanted Sword with a stage.
    /// </summary>
    public class EnchantedSwordOutlineLayer : PlayerDrawLayer
    {
        readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };

        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.HeldItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (drawInfo.shadow != 0f || player.dead || player.itemAnimation <= 0 || player.HeldItem.type != ItemID.EnchantedSword)
                return false;
            return player.GetModPlayer<EnchantedSwordPlayer>().Stage > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            int stage = drawInfo.drawPlayer.GetModPlayer<EnchantedSwordPlayer>().Stage;
            Texture2D texture = TextureAssets.Item[ItemID.EnchantedSword].Value;
            List<DrawData> cache = drawInfo.DrawDataCache;

            // The sword's draw entry is one of the last ones the held-item layer added.
            int index = -1;
            for (int i = cache.Count - 1; i >= Math.Max(0, cache.Count - 8); i--)
            {
                if (cache[i].texture == texture)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
                return;

            DrawData sword = cache[index];
            float strength = EnchantedSwordTuning.OutlineStrengthAt(stage)
                * PixelOutline.Pulse((int)Main.GameUpdateCount, EnchantedSwordTuning.OutlinePulseTicks, EnchantedSwordTuning.OutlinePulseMin, 1f);
            float width = EnchantedSwordTuning.OutlineWidthAt(stage);

            // Every copy is inserted before the sword, so the sword stays on top and only the rim shows.
            for (int i = 0; i < Directions.Length; i++)
            {
                DrawData copy = sword;
                copy.position += Directions[i] * width;
                copy.color = EnchantedSwordTuning.BlueColor * (0.9f * strength);
                copy.shader = 0;
                cache.Insert(index + i, copy);
            }
            for (int i = 0; i < 4; i++)
            {
                DrawData copy = sword;
                copy.position += Directions[i] * (width * 0.5f);
                copy.color = EnchantedSwordTuning.CoreColor * (0.5f * strength);
                copy.shader = 0;
                cache.Insert(index + Directions.Length + i, copy);
            }
        }
    }
}