using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// The honey the bees gather on an enemy, as the enemy shows it: a pulsing orange outline (the enemy's own sprite repeated around itself,
    /// drawn before the enemy so only its edge shows). Every latched bee, on every machine, calls Add(npc, its fill) once per tick from its
    /// own AI; the sum of one tick is the enemy's honey and the outline strength follows it smoothly (it grows as the bees fill up and fades
    /// when they let go). Purely visual and local: nothing here is synced, every machine works it out from the bees it already sees.
    /// The sprite position uses the vanilla bottom-anchored formula and the sprite direction rule of ordinary enemies; an enemy with its own
    /// drawing code may show the outline a little off (BeeKeeperTuning.HoneyOutlineOffsetY nudges it).
    /// </summary>
    public class BeeKeeperHoneyNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        float gathered;             // the sum of the fills of the bees latched on this enemy, for the tick stamped below
        long gatheredTick = -1000;  // Main.GameUpdateCount of the last Add
        float shown;                // the outline strength on screen (0..1), eased toward its target

        // Called by every latched bee every tick on every machine (on a server it is only a few additions).
        public static void Add(NPC npc, float fill)
        {
            BeeKeeperHoneyNPC state = npc.GetGlobalNPC<BeeKeeperHoneyNPC>();
            long now = Main.GameUpdateCount;
            if (state.gatheredTick != now)
            {
                state.gathered = 0f;
                state.gatheredTick = now;
            }
            state.gathered += fill;
        }

        public override void PostAI(NPC npc)
        {
            bool fresh = Main.GameUpdateCount - gatheredTick <= 2;
            if (!fresh && shown <= 0f)
                return;

            float target = fresh ? MathHelper.Clamp(gathered / Math.Max(0.1f, BeeKeeperTuning.HoneyOutlineFullAmount), 0f, 1f) : 0f;
            shown += (target - shown) * MathHelper.Clamp(BeeKeeperTuning.HoneyOutlineEase, 0.01f, 1f);
            if (target <= 0f && shown < 0.01f)
                shown = 0f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (shown <= 0.02f || npc.IsABestiaryIconDummy || npc.hide)
                return true;
            Rectangle frame = npc.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
                return true;

            Main.instance.LoadNPC(npc.type);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;

            float visible = 1f - npc.alpha / 255f;
            float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * Main.GameUpdateCount / Math.Max(1, BeeKeeperTuning.HoneyOutlinePulseTicks));
            float strength = shown * MathHelper.Lerp(BeeKeeperTuning.HoneyOutlineMin, BeeKeeperTuning.HoneyOutlineMax, wave) * visible;
            if (strength <= 0.01f)
                return true;
            float width = BeeKeeperTuning.HoneyOutlineWidth * (0.5f + 0.5f * shown);

            // The vanilla draw puts the frame's bottom on the hitbox's bottom (plus 4 px) and turns the sprite around when spriteDirection is 1.
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 position = new Vector2(npc.Center.X, npc.position.Y + npc.height + 4f - frame.Height * npc.scale * 0.5f + npc.gfxOffY + BeeKeeperTuning.HoneyOutlineOffsetY) - screenPos;
            SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // A soft glow copy a little bigger than the enemy, then the sprite repeated in eight directions (the pixel outline), then a thinner
            // brighter ring inside it. Colors with alpha 0 are additive.
            spriteBatch.Draw(texture, position, frame, BeeKeeperTuning.HoneyNpcOutlineColor * (0.3f * strength), npc.rotation, origin, npc.scale * BeeKeeperTuning.HoneyGlowScale, effects, 0f);
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.PiOver4 * i;
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                spriteBatch.Draw(texture, position + direction * width, frame, BeeKeeperTuning.HoneyNpcOutlineColor * (0.9f * strength), npc.rotation, origin, npc.scale, effects, 0f);
            }
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i;
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                spriteBatch.Draw(texture, position + direction * (width * 0.5f), frame, BeeKeeperTuning.HoneyNpcCoreColor * (0.5f * strength), npc.rotation, origin, npc.scale, effects, 0f);
            }
            return true;
        }
    }
}