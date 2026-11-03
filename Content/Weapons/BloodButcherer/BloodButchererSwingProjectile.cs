using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// The held Blood Butcherer. It draws the jawed blade, poses the owner's arm and runs the bite (left click): WINDUP (pull back),
    /// THRUST (lunge along the aim, jaws shut so it looks like a normal sword), OPEN (the two halves pivot open on the handle and
    /// show their teeth), GAPE (hangs open), BITE (the jaws slam shut). The bite is the only part that damages. At the start of the
    /// bite the owner checks the catch wedge (the triangle between the pivot and the two open jaw tips): an enemy inside is CAUGHT
    /// (the jaws clamp on it, in patch 1 a held clamp; patch 2 spins it and throws it), otherwise it is a MISS. Both then RECOVER.
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the catch) and passes what the other machines need in
    /// the spawn packet: ai[2] = the aim angle (the player's own frame). The decision travels in ai[0] (0 undecided, 1 miss,
    /// 2 clamp) and ai[1] (caught enemy index + 1) with netUpdate. Every machine runs the same timers for the pose, jaws, sounds,
    /// dust and trails; at the end of the bite a non-owner waits in the shut pose for the decision (BloodButchererTuning.DecideWaitTicks)
    /// and assumes a miss if it never arrives. Hits are detected by the owner's client like any projectile. No custom packets.
    /// </summary>
    public class BloodButchererSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the fallback projectile sprite (until the three art files exist).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BloodButcherer;

        public enum Branch { Undecided = 0, Miss = 1, Clamp = 2 }

        static Asset<Texture2D> handleArt;
        static Asset<Texture2D> upperArt;
        static Asset<Texture2D> lowerArt;

        Branch Decision => (Branch)(int)Projectile.ai[0];
        int CaughtIndex => (int)Projectile.ai[1] - 1;
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1; // a bite faces the side of its aim on every machine

        // Per-machine state: counters and visuals, never synced.
        int ticks;               // ticks this machine has run this projectile
        float timer;             // ticks along the timeline (frozen while waiting for the owner's decision)
        int waited;              // ticks spent waiting for it
        int lastEventTick = -1;  // the timeline tick the one-shot effects last ran for
        bool decided;            // owner only: the catch decision has been made
        bool clampPlayed;        // the clamp effects have played
        float flash;             // 1 right after a bite or a clamp, fades to 0
        float jawOpen;           // 0..1: how far the jaws are open on this tick
        Vector2 handPos;         // the hand, pushed along the aim by the lunge (also Projectile.Center)
        Vector2 pivot;           // where the jaws pivot
        Vector2 bladeDir;        // the blade's direction (a unit vector)
        float jawLength;         // pivot to the jaw tips, in pixels (scaled)
        Vector2 lastOwnerCenter; // the trail interpolates the owner's movement from it
        PixelTrail jawA;         // the line each jaw tip leaves (null until the thrust starts)
        PixelTrail jawB;
        Vector2 lastTipA;
        Vector2 lastTipB;
        bool hasTips;

        // ---- timeline ----

        static int ThrustStart => BloodButchererTuning.WindupTicks;
        static int OpenStart => ThrustStart + BloodButchererTuning.ThrustTicks;
        static int GapeStart => OpenStart + BloodButchererTuning.OpenTicks;
        static int BiteStart => GapeStart + BloodButchererTuning.GapeTicks;
        static int BiteEnd => BiteStart + BloodButchererTuning.BiteTicks;

        // What this machine believes happened: a non-owner that never hears the decision assumes a miss.
        Branch Resolved
        {
            get
            {
                Branch branch = Decision;
                return branch == Branch.Undecided && waited >= BloodButchererTuning.DecideWaitTicks ? Branch.Miss : branch;
            }
        }

        int RecoverStart => Resolved == Branch.Clamp ? BiteEnd + BloodButchererTuning.ClampTicks : BiteEnd;
        int TotalTicks => RecoverStart + BloodButchererTuning.RecoverTicks;
        bool Damaging => timer >= BiteStart && timer < BiteEnd;

        // How far the blade is pushed along the aim from the hand (negative = pulled back).
        float OffsetAt(float time)
        {
            float pull = BloodButchererTuning.WindupPullback;
            float thrust = BloodButchererTuning.ThrustDistance;
            float held = thrust + BloodButchererTuning.BiteLungeDistance;
            if (time < ThrustStart)
                return MathHelper.Lerp(0f, -pull, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, ThrustStart)));
            if (time < OpenStart)
            {
                float u = (time - ThrustStart) / Math.Max(1, BloodButchererTuning.ThrustTicks);
                return MathHelper.Lerp(-pull, thrust, 1f - (float)Math.Pow(1f - u, 3)); // fast out, slowing at the end
            }
            if (time < BiteStart)
                return thrust;
            if (time < BiteEnd)
            {
                float u = (time - BiteStart) / Math.Max(1, BloodButchererTuning.BiteTicks);
                return MathHelper.Lerp(thrust, held, 1f - (float)Math.Pow(1f - u, 2));
            }
            if (time < RecoverStart)
                return held;
            return MathHelper.Lerp(held, 0f, MathHelper.SmoothStep(0f, 1f, (time - RecoverStart) / Math.Max(1, BloodButchererTuning.RecoverTicks)));
        }

        // 0 = shut, 1 = fully open: eases open, hangs, then slams shut with an ease-in so the bite lands hard.
        static float OpenAt(float time)
        {
            if (time < OpenStart)
                return 0f;
            if (time < GapeStart)
            {
                float u = (time - OpenStart) / Math.Max(1, BloodButchererTuning.OpenTicks);
                return 1f - (float)Math.Pow(1f - u, 3);
            }
            if (time < BiteStart)
                return 1f;
            if (time < BiteEnd)
            {
                float u = (time - BiteStart) / Math.Max(1, BloodButchererTuning.BiteTicks);
                return 1f - (float)Math.Pow(u, 2.5);
            }
            return 0f;
        }

        // ---- AI ----

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per bite
            Projectile.ownerHitCheck = true;
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererHandle", out handleArt);
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererJawUpper", out upperArt);
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererJawLower", out lowerArt);
        }

        public override void Unload()
        {
            handleArt = null;
            upperArt = null;
            lowerArt = null;
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.BloodButcherer)
            {
                Projectile.Kill();
                return;
            }

            if (timer >= TotalTicks)
            {
                owner.itemAnimation = 0; // the next use may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                if (IsOwner)
                    owner.ChangeDir(Facing);
            }

            Projectile.scale = BloodButchererTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            jawOpen = OpenAt(timer);

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the blade is out
            owner.itemTime = 2;
            ApplyPose(owner);

            if (IsOwner && !decided && (int)timer == BiteStart)
                Decide();

            Effects();
            LayTrail();

            flash = Math.Max(0f, flash - 1f / Math.Max(1, BloodButchererTuning.FlashTicks));
            jawA?.Tick();
            jawB?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;

            // At the end of the bite a machine that has not heard the owner's decision yet holds the shut pose for a moment.
            bool waiting = (int)timer >= BiteEnd && Decision == Branch.Undecided && waited < BloodButchererTuning.DecideWaitTicks;
            if (waiting)
                waited++;
            else
                timer++;
        }

        // Moves the owner's arm to the aim and puts the projectile in the hand, pushed along the aim by the lunge.
        void ApplyPose(Player owner)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Aim - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, Aim);
            float blade = owner.gravDir == -1f ? -Aim : Aim;
            bladeDir = blade.ToRotationVector2();

            Vector2 position = hand + bladeDir * OffsetAt(timer);
            // The clamp makes the whole blade tremble.
            if (Resolved == Branch.Clamp && timer >= BiteEnd && timer < BiteEnd + BloodButchererTuning.ClampTicks)
                position += Main.rand.NextVector2Circular(1.5f, 1.5f);

            Projectile.rotation = blade;
            Projectile.Center = position;
            Projectile.spriteDirection = Facing;
            handPos = position;
            pivot = position + bladeDir * (BloodButchererTuning.HandleLength * Projectile.scale);
            jawLength = (BloodButchererTuning.BladeLength - BloodButchererTuning.HandleLength) * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- the catch (owner only) ----

        static bool CanBite(NPC npc, Projectile projectile)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage && npc.CanBeChasedBy(projectile);
        }

        // At the start of the bite: the nearest enemy (to the middle of the mouth) inside the catch wedge is caught. The decision
        // reaches the other machines through ai[0], ai[1] and netUpdate.
        void Decide()
        {
            decided = true;
            Vector2 mouth = pivot + bladeDir * jawLength * 0.6f;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!CanBite(npc, Projectile))
                    continue;
                Rectangle box = npc.Hitbox;
                box.Inflate((int)BloodButchererTuning.CatchPadding, (int)BloodButchererTuning.CatchPadding);
                if (!WedgeTouches(box))
                    continue;
                float distance = Vector2.DistanceSquared(npc.Center, mouth);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            Projectile.ai[0] = (float)(best >= 0 ? Branch.Clamp : Branch.Miss);
            Projectile.ai[1] = best + 1;
            Projectile.netUpdate = true;
        }

        // The catch wedge: the triangle between the pivot and the two jaw tips at full gape. The bite is quick, so it is tested against
        // the full gape for the whole bite: forgiving on purpose.
        bool WedgeTouches(Rectangle box)
        {
            float spread = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg);
            Vector2 a = pivot;
            Vector2 b = pivot + bladeDir.RotatedBy(spread) * jawLength;
            Vector2 c = pivot + bladeDir.RotatedBy(-spread) * jawLength;

            if (InTriangle(box.Center.ToVector2(), a, b, c)
                || InTriangle(box.TopLeft(), a, b, c) || InTriangle(box.TopRight(), a, b, c)
                || InTriangle(box.BottomLeft(), a, b, c) || InTriangle(box.BottomRight(), a, b, c))
                return true;

            // A thin enemy can cross an edge without having a corner inside.
            float point = 0f;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), a, b, 4f, ref point)
                || Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), b, c, 4f, ref point)
                || Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), c, a, 4f, ref point);
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Side(p, a, b);
            float d2 = Side(p, b, c);
            float d3 = Side(p, c, a);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        static float Side(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);
        }

        // ---- effects and trails (every machine) ----

        Vector2 CaughtCenter(Vector2 fallback)
        {
            int index = CaughtIndex;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                return Main.npc[index].Center;
            return fallback;
        }

        void Effects()
        {
            int t = (int)timer;
            Vector2 mouth = pivot + bladeDir * jawLength * 0.6f;

            if (t != lastEventTick)
            {
                lastEventTick = t;
                if (t == ThrustStart)
                    BloodButchererFx.PlayLunge(handPos);
                if (t == OpenStart)
                {
                    BloodButchererFx.PlayOpen(pivot);
                    BloodButchererFx.MouthMist(pivot, bladeDir);
                }
                if (t == BiteStart)
                {
                    flash = 1f;
                    BloodButchererFx.PlayBite(mouth);
                    BloodButchererFx.BiteSnap(mouth);
                }
            }

            // The clamp lands once the bite is over and the decision says something was caught.
            if (!clampPlayed && t >= BiteEnd && Resolved == Branch.Clamp)
            {
                clampPlayed = true;
                flash = 1f;
                Vector2 where = CaughtCenter(mouth);
                BloodButchererFx.ClampBurst(where);
                BloodButchererFx.PlayClamp(where);
                CameraShake.Punch(where, BloodButchererTuning.ClampShake, 18, "BloodButchererClamp");
            }

            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * jawOpen;
            if (jawOpen > 0.5f && Main.rand.NextBool(4))
                BloodButchererFx.Drool(pivot + bladeDir.RotatedBy(Main.rand.NextBool() ? angle : -angle) * jawLength);

            Lighting.AddLight(mouth, 0.45f + 0.5f * flash, 0.05f + 0.1f * flash, 0.05f + 0.1f * flash);
        }

        // Both jaw tips lay a line from the start of the thrust to the end of the bite: one line while the jaws are shut (the tips
        // coincide), forking into two arcs when they open and meeting again in the bite. Several points per tick, laid between the
        // previous tick's pose and this one, so the pixel line follows a smooth arc.
        void LayTrail()
        {
            if (Main.dedServ)
                return;
            int t = (int)timer;
            if (t < ThrustStart || t >= BiteEnd)
                return;

            int samples = Math.Max(1, BloodButchererTuning.TrailSamplesPerTick);
            for (int k = 1; k <= samples; k++)
                AddJawSample(timer - 1f + k / (float)samples, k / (float)samples);
        }

        void AddJawSample(float time, float fraction)
        {
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, Aim) + shift + bladeDir * OffsetAt(time);
            Vector2 axis = hand + bladeDir * (BloodButchererTuning.HandleLength * Projectile.scale);
            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * OpenAt(time);
            Vector2 tipA = axis + bladeDir.RotatedBy(angle) * jawLength;
            Vector2 tipB = axis + bladeDir.RotatedBy(-angle) * jawLength;

            // Skip a sample that sits on the previous one (the gape holds still): a zero-length segment adds nothing.
            if (hasTips && Vector2.DistanceSquared(tipA, lastTipA) < 0.25f && Vector2.DistanceSquared(tipB, lastTipB) < 0.25f)
                return;

            jawA ??= new PixelTrail(new BloodButchererJawTrailStyle(), IsOwner);
            jawB ??= new PixelTrail(new BloodButchererJawTrailStyle(), IsOwner);
            jawA.Add(tipA);
            jawB.Add(tipB);
            lastTipA = tipA;
            lastTipB = tipB;
            hasTips = true;
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            return WedgeTouches(targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
            modifiers.SourceDamage *= target.whoAmI == CaughtIndex ? BloodButchererTuning.BiteDamageMultiplier : BloodButchererTuning.OtherDamageMultiplier;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BloodButchererFx.HitBlood(target.Center);
        }

        // The trails outlive the bite and keep fading on their own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(jawA);
            DetachedTrails.Hand(jawB);
            jawA = null;
            jawB = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            jawA?.Draw();
            jawB?.Draw();

            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * jawOpen;
            Color body = Color.Lerp(lightColor, Color.White, 0.25f + 0.4f * flash);
            if (handleArt?.IsLoaded == true && upperArt?.IsLoaded == true && lowerArt?.IsLoaded == true)
                DrawArt(body, angle);
            else
                DrawFallback(body, angle);
            return false;
        }

        // Your art: three sprites on one canvas, the sword horizontal and pointing right, sharing the pivot pixel. Facing left flips
        // them vertically (the rotation already turned them around) so the teeth stay on the right sides.
        void DrawArt(Color body, float angle)
        {
            Vector2 position = pivot - Main.screenPosition;
            SpriteEffects effects = Facing > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            float scale = Projectile.scale;

            DrawPiece(handleArt.Value, position, Projectile.rotation, scale, effects, body);
            DrawPiece(lowerArt.Value, position, Projectile.rotation + Facing * angle, scale, effects, body);
            DrawPiece(upperArt.Value, position, Projectile.rotation - Facing * angle, scale, effects, body);

            if (flash > 0.01f)
            {
                Color glow = BloodButchererTuning.CrimsonColor * (0.9f * flash);
                DrawPiece(lowerArt.Value, position, Projectile.rotation + Facing * angle, scale * 1.06f, effects, glow);
                DrawPiece(upperArt.Value, position, Projectile.rotation - Facing * angle, scale * 1.06f, effects, glow);
            }
        }

        void DrawPiece(Texture2D texture, Vector2 position, float rotation, float scale, SpriteEffects effects, Color color)
        {
            Vector2 pivotPixel = BloodButchererTuning.ArtPivot;
            Vector2 origin = new Vector2(pivotPixel.X, effects == SpriteEffects.None ? pivotPixel.Y : texture.Height - pivotPixel.Y);
            Main.EntitySpriteDraw(texture, position, null, color, rotation, origin, scale, effects, 0);
        }

        // Until the art exists: the vanilla sprite. Shut, it is one normal sword. Open, two copies of it splay out on the pivot and the
        // teeth are drawn as pixel spikes.
        void DrawFallback(Color body, float angle)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float scale = Projectile.scale;

            if (jawOpen < 0.01f)
            {
                DrawSword(texture, handPos, Projectile.rotation + rotationOffset, origin, scale, effects, body);
                if (flash > 0.01f)
                    DrawSword(texture, handPos, Projectile.rotation + rotationOffset, origin, scale * 1.06f, effects, BloodButchererTuning.CrimsonColor * (0.9f * flash));
                return;
            }

            for (int jaw = 0; jaw < 2; jaw++)
            {
                float a = jaw == 0 ? angle : -angle;
                Vector2 hilt = pivot + (handPos - pivot).RotatedBy(a);
                DrawSword(texture, hilt, Projectile.rotation + a + rotationOffset, origin, scale, effects, body);
                if (flash > 0.01f)
                    DrawSword(texture, hilt, Projectile.rotation + a + rotationOffset, origin, scale * 1.06f, effects, BloodButchererTuning.CrimsonColor * (0.9f * flash));

                Vector2 jawDirection = bladeDir.RotatedBy(a);
                Vector2 inward = jawDirection.RotatedBy(-Math.Sign(a) * MathHelper.PiOver2); // toward the blade's axis
                BloodButchererFx.DrawTeeth(pivot, jawDirection, jawLength, inward, jawOpen, scale, body);
            }
        }

        static void DrawSword(Texture2D texture, Vector2 worldPosition, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color color)
        {
            Main.EntitySpriteDraw(texture, worldPosition - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }
    }
}