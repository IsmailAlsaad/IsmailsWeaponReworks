using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The held Muramasa while it is not attacking. It draws the sword, poses the owner's arm and runs the sheathing: the arm and blade
    /// ease slowly from where they started (the end of the last swing, or the ready pose) to the sheathed pose at the hip, then the sword
    /// CLICKS shut (flash, kick, sparks, sound) and from then on it wears a pulsing blue pixel outline. There is no sheath sprite: the
    /// player simply holds the sword in the sheathing position. A click (the swing) kills it; the swing starts a new one when it ends.
    /// MULTIPLAYER: the owner's client spawns it; ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle.
    /// Sheath progress is a pure function of the ticks this machine has run it, so every machine runs the same animation, click and
    /// pulse without any packet. Each machine ends it on its own when the owner dies, swaps items or starts a swing.
    /// </summary>
    public class MuramasaStanceProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

        Player Owner => Main.player[Projectile.owner];
        int Facing => Owner.direction >= 0 ? 1 : -1;
        float StartArm => MathHelper.WrapAngle(Projectile.ai[0]);
        float StartTilt => Projectile.ai[1];

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        bool scraped;
        bool clicked;
        float clickFlash;      // 1 right after the click, fades to 0
        float outlineShown;    // 0..1, how visible the blue outline is
        float armLocal;        // facing-local arm angle (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;
        MuramasaDrawState draw; // the path being drawn (owner's client only, never synced)
        bool wasHeld;          // the button on the previous tick (owner's client)

        public bool IsDrawing => draw != null;

        // What the swing starts from (read by MuramasaGlobalItem on the owner's client).
        // Before the first AI tick the pose is still the one the stance was spawned with (a held button can start the next swing that early).
        public float ArmLocal => ticks == 0 ? StartArm : armLocal;
        public float TiltRad => ticks == 0 ? StartTilt : tiltRad;
        public float Progress => MathHelper.Clamp(ticks / (float)Math.Max(1, MuramasaTuning.SheathTicks), 0f, 1f);
        public bool FullySheathed => ticks >= MuramasaTuning.SheathTicks;

        public static void Spawn(IEntitySource source, Player owner, float startArmLocal, float startTilt)
        {
            Projectile.NewProjectile(source, owner.MountedCenter, Vector2.Zero, ModContent.ProjectileType<MuramasaStanceProjectile>(), 0, 0f, owner.whoAmI, startArmLocal, startTilt);
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // it never hurts anything
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
        }

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, when they get crowd-controlled, or when a swing takes over.
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Muramasa
                || MuramasaPlayer.Find(Projectile.owner, ModContent.ProjectileType<MuramasaSwingProjectile>()) != null
                || MuramasaPlayer.Find(Projectile.owner, ModContent.ProjectileType<MuramasaIaidoProjectile>()) != null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.scale = MuramasaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (!clicked && FullySheathed)
            {
                clicked = true;
                clickFlash = 1f;
            }

            // The sheathing: a slow smoothstep from the start pose to the sheathed pose (the arm takes the short way round).
            float eased = MathHelper.SmoothStep(0f, 1f, Progress);
            armLocal = StartArm + MathHelper.WrapAngle(Rad(MuramasaTuning.SheathArmDeg) - StartArm) * eased;
            tiltRad = StartTilt + (Rad(MuramasaTuning.SheathTiltDeg) - StartTilt) * eased;
            armLocal -= Rad(MuramasaTuning.ClickKickDeg) * clickFlash; // the hand kicks when the sword clicks shut

            ApplyPose(owner);

            if (!scraped && Progress >= MuramasaTuning.ScrapeAt)
            {
                scraped = true;
                MuramasaFx.PlayScrape(Projectile.Center);
            }
            if (clickFlash >= 1f)
            {
                MuramasaFx.Click(Projectile.Center, tipPosition, true);
                CameraShake.Punch(Projectile.Center, MuramasaTuning.ClickShake, 8);
            }

            clickFlash = Math.Max(0f, clickFlash - 1f / Math.Max(1, MuramasaTuning.ClickFlashTicks));
            outlineShown += ((FullySheathed ? 1f : 0f) - outlineShown) * MathHelper.Clamp(MuramasaTuning.OutlineFade, 0.01f, 1f);
            if (outlineShown > 0.05f)
            {
                if (Main.rand.NextFloat() < 0.1f + 0.2f * outlineShown)
                    MuramasaFx.BladeSparkle(Projectile.Center, tipPosition);
                Lighting.AddLight(tipPosition, MuramasaTuning.TintLight(new Vector3(0.1f * outlineShown, 0.3f * outlineShown, 0.7f * outlineShown)));
            }

            if (Projectile.owner == Main.myPlayer && UpdateDraw(owner))
                return; // a swing or the iaido took over (this stance is dead)

            ticks++;
        }

        // The draw window (owner's client only). A press on a FULLY sheathed sword opens it; releasing the button or running out of time
        // closes it. A path under DrawMinPathPixels is a plain swing at the cursor; a longer one is simplified into at most DrawMaxStrokes
        // straight strokes and MuramasaIaidoProjectile.Launch turns them into the iaido (one slash per stroke). The path itself is never
        // synced: the slash projectiles carry everything. Returns true when this stance is dead (a swing or the iaido took over).
        bool UpdateDraw(Player owner)
        {
            bool held = owner.controlUseItem;
            if (ticks == 0)
                wasHeld = held; // a button that was already down when the stance began (the end of a swing) never opens the window
            bool pressed = held && !wasHeld;
            wasHeld = held;

            if (draw != null)
            {
                if (!draw.Tick(owner, held))
                    return false;

                MuramasaDrawState finished = draw;
                draw = null;
                if (finished.PathLength < MuramasaTuning.DrawMinPathPixels || finished.Strokes.Count < 2)
                {
                    Item item = owner.HeldItem;
                    MuramasaGlobalItem.StartSwing(owner, owner.GetSource_ItemUse(item), owner.GetWeaponDamage(item), owner.GetWeaponKnockback(item, item.knockBack));
                }
                else
                    MuramasaIaidoProjectile.Launch(owner, finished.Strokes);
                return true;
            }

            if (pressed && FullySheathed)
            {
                draw = new MuramasaDrawState();
                draw.Begin(owner);
                MuramasaFx.DrawStart(Projectile.Center);
            }
            return false;
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            float worldArm = HeldSwordPose.ToWorld(Facing, armLocal);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = HeldSwordPose.ToWorld(Facing, armLocal + tiltRad);
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * MuramasaTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            draw?.Draw();

            Texture2D texture = MuramasaGreen.Sprite(TextureAssets.Projectile[Type].Value);
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, MuramasaTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // The sheathed outline: a soft glow behind the sword (drawn around the CENTER of the blade sprite, so it swells outward evenly)
            // and the sprite repeated in the eight directions around it, a pixel-style outline. Both pulse; the colors have alpha 0 = additive.
            if (outlineShown > 0.01f)
            {
                float strength = outlineShown * PixelOutline.Pulse(ticks, MuramasaTuning.OutlinePulseTicks, MuramasaTuning.OutlineMin, MuramasaTuning.OutlineMax);

                PixelOutline.Draw(texture, position, rotation, origin, scale, effects, Projectile.spriteDirection, strength, MuramasaTuning.OutlineWidth, MuramasaTuning.GlowScale,
                    MuramasaTuning.OutlineColor, MuramasaTuning.OutlineCoreColor);
            }

            // The sword itself, brightened a little (the click flares it white).
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.3f + 0.5f * clickFlash), rotation, origin, scale, effects, 0);
            if (clickFlash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.CoreColor * (0.8f * clickFlash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}