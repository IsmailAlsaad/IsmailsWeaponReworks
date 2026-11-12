using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The Sunfury's flail head: one projectile that lives through the whole hold-charge rhythm loop (every flail will share it).
    /// CHARGE: hold to spin the head; the tier's charge must be held to its full threshold, releasing earlier throws nothing and ends the loop.
    /// THROW: a completed tier 1 or 2 charge, released, throws the head along an ellipse whose apex is the cursor; at the apex a chain jingle plays.
    /// CAUGHT: a fresh click/hold at ANY moment while the head is out keeps it returning and starts the next tier's charge (also needs a full hold).
    /// SPECIAL: a completed tier 3 charge, released, sends the head straight at the cursor where it detonates in a sun-like explosion.
    /// RETRACT: the head comes back after the explosion. If the button is held when it reaches the hand (pressed at any moment during the
    /// return, or still held), it is caught and tier 1 starts spinning at once; otherwise the projectile ends (tier back to zero).
    /// FAIL: the head getting home without a new press, an early release or too short a hold sends the head back and ends the loop.
    /// Ending the projectile IS the reset: the next press starts a new projectile at tier 1.
    ///
    /// ai[0] = phase * 4 + sub (Charge: tier being charged, Throw/Caught: thrown tier, Special: 3, Fail: reason), ai[1] = aim angle, ai[2] = reach.
    /// MULTIPLAYER: the owner's client decides everything (holding, releasing, the window, the tier, the throw, the detonation, every mistake)
    /// and syncs the decision through ai[0..2] with netUpdate. The ellipse is a pure function of the owner's position, the aim and the time
    /// since the phase began, so every machine flies the same arc and plays the jingle at its own apex without any per-tick sync. The
    /// explosion is a normal spawned projectile (SunfuryBlastProjectile). Other machines add the head's hit sparks through RemoteHitTracker.
    /// </summary>
    public class SunfuryFlailProjectile : ModProjectile
    {
        // Vanilla art until a SunfuryFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Sunfury;

        static Asset<Texture2D> art;
        Asset<Texture2D> chainAsset;

        public enum Phase { Charge = 0, Throw = 1, Caught = 2, Special = 3, Retract = 4, Fail = 5 }
        public enum FailReason { Undercharged = 0, LockedChain = 1, Mistimed = 2 }

        public static float Encode(Phase phase, int sub)
        {
            return (int)phase * 4 + sub;
        }

        Phase CurrentPhase => (Phase)((int)Projectile.ai[0] / 4);
        int Sub => (int)Projectile.ai[0] % 4;
        float AimAngle => Projectile.ai[1];
        float AimReach => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => Owner.direction >= 0 ? 1 : -1;

        // The tier whose charge is running (Charge) or will run next (Caught).
        int ChargingTier => CurrentPhase == Phase.Caught ? Sub + 1 : Sub;
        bool Damaging => CurrentPhase == Phase.Throw || CurrentPhase == Phase.Caught || CurrentPhase == Phase.Special;
        // The spinning head near the player also hurts (SunfuryTuning.SpinDamageMultiplier; 0 turns it off).
        bool SpinHurts => CurrentPhase == Phase.Charge && SunfuryTuning.SpinDamageMultiplier(Sub) > 0f;
        bool CanHurt => Damaging || SpinHurts;
        // The tier that colors the head and its trail.
        int LookTier => CurrentPhase == Phase.Special || CurrentPhase == Phase.Retract ? 3 : CurrentPhase == Phase.Fail ? 1 : Math.Max(1, Sub);

        // Per-machine state: counters and visuals, never synced.
        int lastPhase = -1;
        int phaseTimer;          // ticks in the current phase (it keeps counting from Throw into Caught)
        int chargeTimer;         // ticks the hold has lasted (from the press in the window, or from the start of the charge)
        bool apexPlayed;
        int parkedTicks;         // other machines: ticks the head has waited at the hand for the owner's decision after the special
        bool chargeComplete;
        float spinAngle;
        float spinRadius;
        float headRotation;
        float flash;
        Vector2 releasePosition;
        Vector2 sweepStart;      // where the spinning head was last tick (the hit sweeps from here to the head)
        Vector2 specialTarget;
        PixelTrail trail;
        AfterimageTrail afterimages;
        RemoteHitTracker hitTracker; // other machines only

        // Owner only.
        bool previousHold = true;
        bool releasedAfterComplete;
        int baseDamage;
        int lockedDamage;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Sunfury/SunfuryFlailProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)SunfuryTuning.HeadSize;
            Projectile.height = (int)SunfuryTuning.HeadSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SunfuryTuning.HitCooldownTicks;
        }

        // ---- helpers ----

        // Ticks the head needs for ONE half of the ellipse (out or back). The ellipse runs from the hand to the apex and back; its half
        // perimeter divided by the tier's speed is the retract window.
        static float HalfTicks(int tier, float reach)
        {
            float a = reach * 0.5f;
            float b = a * SunfuryTuning.EllipseWidthRatio;
            float perimeter = MathHelper.Pi * (3f * (a + b) - (float)Math.Sqrt((3f * a + b) * (a + 3f * b)));
            return Math.Max(SunfuryTuning.MinHalfTicks, perimeter * 0.5f / Math.Max(1f, SunfuryTuning.ThrowSpeed(tier)));
        }

        Vector2 EllipsePoint(float theta, Vector2 hand)
        {
            Vector2 direction = AimAngle.ToRotationVector2();
            int side = direction.X >= 0f ? -1 : 1; // the way out bulges upward, the way back downward
            Vector2 perpendicular = direction.RotatedBy(side * MathHelper.PiOver2);
            float a = AimReach * 0.5f;
            float b = a * SunfuryTuning.EllipseWidthRatio;
            return hand + direction * (a * (1f - (float)Math.Cos(theta))) + perpendicular * (b * (float)Math.Sin(theta));
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            if (!owner.active || owner.dead || owner.noItems || owner.HeldItem.type != ItemID.Sunfury || owner.CCed)
            {
                Projectile.Kill();
                return;
            }
            if (baseDamage == 0)
                baseDamage = Projectile.damage;

            Projectile.velocity = Vector2.Zero; // every move is a direct position change
            Phase phase = CurrentPhase;
            if ((int)phase != lastPhase)
            {
                EnterPhase(lastPhase, phase);
                lastPhase = (int)phase;
            }

            bool holding = IsOwner && owner.controlUseItem;
            switch (phase)
            {
                case Phase.Charge:
                    UpdateCharge(owner, holding);
                    break;
                case Phase.Throw:
                case Phase.Caught:
                    UpdateEllipse(owner, holding);
                    break;
                case Phase.Special:
                    UpdateSpecial(owner);
                    break;
                case Phase.Retract:
                    UpdateReturn(owner, SunfuryTuning.RetractSpeed, SunfuryTuning.SpecialHangTicks, true, holding);
                    break;
                default:
                    UpdateReturn(owner, SunfuryTuning.FailRetractSpeed, 0, false, false);
                    break;
            }
            if (!Projectile.active)
                return;

            if (CanHurt && !IsOwner)
            {
                hitTracker ??= new RemoteHitTracker();
                int tier = LookTier;
                hitTracker.Update(Projectile, (int)phase * 10 + (apexPlayed ? 1 : 0) + tier * 100, npc => SunfuryFx.HitSparks(npc.Center, tier));
            }

            if (IsOwner)
            {
                if (owner.controlUseItem)
                    owner.releaseUseItem = false; // after a reset, the button must be let go before a new loop can start
                previousHold = holding;
            }

            flash = Math.Max(0f, flash - 1f / Math.Max(1, SunfuryTuning.FlashTicks));
            trail?.Tick();
            headRotation += 0.35f + 0.3f * (Damaging ? 1f : 0f);
            ApplyPose(owner);
            phaseTimer++;
            if (phase == Phase.Charge || phase == Phase.Caught)
                chargeTimer++;
        }

        void EnterPhase(int previous, Phase phase)
        {
            Player owner = Owner;
            bool throwToCaught = previous == (int)Phase.Throw && phase == Phase.Caught;
            if (!throwToCaught)
                phaseTimer = 0;

            switch (phase)
            {
                case Phase.Charge:
                    if (previous != (int)Phase.Caught)
                    {
                        chargeTimer = 0;
                        chargeComplete = false;
                    }
                    Vector2 offset = Projectile.Center - owner.MountedCenter;
                    spinRadius = offset.Length();
                    spinAngle = spinRadius > 1f ? offset.ToRotation() : -MathHelper.PiOver2;
                    NewTrail(Sub);
                    break;
                case Phase.Throw:
                    releasePosition = Projectile.Center;
                    apexPlayed = false;
                    NewTrail(Sub);
                    SunfuryFx.ThrowWhoosh(Projectile.Center, Sub);
                    break;
                case Phase.Caught:
                    chargeTimer = 0;
                    chargeComplete = false;
                    flash = 1f;
                    SunfuryFx.Caught(owner.MountedCenter, Sub + 1);
                    break;
                case Phase.Special:
                    NewTrail(3);
                    specialTarget = owner.MountedCenter + AimAngle.ToRotationVector2() * AimReach;
                    flash = 1f;
                    SunfuryFx.SpecialLaunch(Projectile.Center);
                    break;
                case Phase.Fail:
                    switch ((FailReason)Sub)
                    {
                        case FailReason.Undercharged:
                            SunfuryFx.Fizzle(Projectile.Center);
                            break;
                        case FailReason.LockedChain:
                            SunfuryFx.LockedChain(Projectile.Center);
                            break;
                        default:
                            SunfuryFx.Mistimed(Projectile.Center);
                            break;
                    }
                    break;
            }
        }

        void NewTrail(int tier)
        {
            if (Main.dedServ)
                return;
            DetachedTrails.Hand(trail);
            trail = new PixelTrail(new SunfuryTrailStyle(Math.Max(1, tier)), IsOwner);
        }

        // ---- charge ----

        void UpdateCharge(Player owner, bool holding)
        {
            int tier = Sub;
            int need = SunfuryTuning.ChargeTicks(tier);
            if (IsOwner)
                Projectile.damage = Math.Max(1, (int)(baseDamage * SunfuryTuning.TierDamageMultiplier(tier) * SunfuryTuning.SpinDamageMultiplier(tier)));
            float progress = MathHelper.Clamp(chargeTimer / (float)Math.Max(1, need), 0f, 1f);

            if (IsOwner)
                owner.ChangeDir(Main.MouseWorld.X >= owner.Center.X ? 1 : -1);

            // The head spins faster and wider as the charge fills.
            float eased = 1f - (1f - progress) * (1f - progress);
            float omega = MathHelper.Lerp(SunfuryTuning.SpinOmegaStart(tier), SunfuryTuning.SpinOmegaEnd(tier), eased);
            if (IsOwner)
                Projectile.localNPCHitCooldown = Math.Max(1, (int)(MathHelper.TwoPi / omega * SunfuryTuning.SpinHitPeriodFraction)); // about one hit per full circle
            spinAngle += omega * Facing;
            float targetRadius = MathHelper.Lerp(SunfuryTuning.SpinRadiusMin, SunfuryTuning.SpinRadiusMax, eased);
            spinRadius = MathHelper.Lerp(spinRadius, targetRadius, SunfuryTuning.SpinRadiusSmoothing);
            Vector2 previousCenter = Projectile.Center;
            sweepStart = previousCenter;
            Projectile.Center = owner.MountedCenter + spinAngle.ToRotationVector2() * spinRadius;
            trail?.Add(Projectile.Center);

            CheckChargeComplete(owner, tier, need);
            SunfuryFx.SpinEmbers(Projectile.Center, Projectile.Center - previousCenter, tier, progress);
            SunfuryFx.ChargeGather(owner.MountedCenter, tier, progress);
            Lighting.AddLight(Projectile.Center, 0.6f + 0.6f * progress + flash, 0.3f + 0.3f * progress + 0.5f * flash, 0.08f + 0.2f * flash);

            if (!IsOwner || holding)
                return;

            // Owner: the button was released.
            if (chargeComplete)
                ThrowTier(owner, tier);
            else
                Fail(tier <= 1 ? FailReason.Undercharged : FailReason.Mistimed);
        }

        // Runs on every machine: the same timers fire the same completion effects. The owner also locks in the tier's damage here.
        void CheckChargeComplete(Player owner, int tier, int need)
        {
            if (chargeComplete || chargeTimer < need)
                return;
            chargeComplete = true;
            flash = 1f;
            SunfuryFx.TierComplete(Projectile.Center, tier);
            if (IsOwner)
                lockedDamage = Math.Max(1, (int)(baseDamage * SunfuryTuning.TierDamageMultiplier(tier)));
        }

        // Owner only: release with a completed charge. Tier 1 and 2 throw the ellipse, tier 3 the special.
        void ThrowTier(Player owner, int tier)
        {
            Vector2 toMouse = Main.MouseWorld - owner.MountedCenter;
            float reach = toMouse.Length();
            Projectile.ai[1] = toMouse.ToRotation();
            Projectile.localNPCHitCooldown = SunfuryTuning.HitCooldownTicks;
            if (tier >= 3)
            {
                Projectile.ai[2] = MathHelper.Clamp(reach, SunfuryTuning.SpecialMinReach, SunfuryTuning.SpecialMaxReach);
                Projectile.damage = Math.Max(1, (int)(lockedDamage * SunfuryTuning.SpecialPathDamageMultiplier));
                Projectile.ai[0] = Encode(Phase.Special, 3);
            }
            else
            {
                Projectile.ai[2] = MathHelper.Clamp(reach, SunfuryTuning.MinReach, SunfuryTuning.MaxReach);
                Projectile.damage = lockedDamage;
                Projectile.ai[0] = Encode(Phase.Throw, tier);
            }
            releasedAfterComplete = false;
            Projectile.netUpdate = true;
        }

        void Fail(FailReason reason)
        {
            Projectile.ai[0] = Encode(Phase.Fail, (int)reason);
            Projectile.netUpdate = true;
        }

        // ---- the ellipse and the retract window ----

        void UpdateEllipse(Player owner, bool holding)
        {
            int thrownTier = Sub;
            float half = HalfTicks(thrownTier, AimReach);
            float time = phaseTimer;
            float theta = MathHelper.Clamp(time / half, 0f, 2f) * MathHelper.Pi;

            Vector2 target = EllipsePoint(theta, owner.MountedCenter);
            float blend = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(time / Math.Max(1, SunfuryTuning.ReleaseBlendTicks), 0f, 1f));
            Projectile.Center = Vector2.Lerp(releasePosition, target, blend);
            trail?.Add(Projectile.Center);

            if (!apexPlayed && time >= half)
            {
                apexPlayed = true; // the window opens here, with a chain jingle
                SunfuryFx.ApexJingle(Projectile.Center, thrownTier);
            }
            SunfuryFx.FlightDust(Projectile.Center, target - Projectile.Center, thrownTier);
            Lighting.AddLight(Projectile.Center, 1f, 0.55f, 0.15f);

            if (CurrentPhase == Phase.Caught)
            {
                int nextTier = thrownTier + 1;
                CheckChargeComplete(owner, nextTier, SunfuryTuning.ChargeTicks(nextTier));
                SunfuryFx.ChargeGather(owner.MountedCenter, nextTier, chargeTimer / (float)Math.Max(1, SunfuryTuning.ChargeTicks(nextTier)));
            }
            if (!IsOwner)
                return;

            float arrival = half * 2f;
            bool press = holding && !previousHold;

            if (CurrentPhase == Phase.Throw)
            {
                if (press)
                {
                    Projectile.ai[0] = Encode(Phase.Caught, thrownTier); // a press at any moment while the head is out: the next charge starts
                    Projectile.netUpdate = true;
                }
                else if (time > arrival)
                    Fail(FailReason.Mistimed);          // the head got home and no press came
                return;
            }

            // Caught: the hold must continue until the next tier's charge is complete.
            int next = thrownTier + 1;
            if (!holding && !releasedAfterComplete)
            {
                if (chargeComplete)
                    releasedAfterComplete = true;       // let go after completing it: the throw happens as soon as the head is back
                else
                {
                    Fail(FailReason.Mistimed);          // under-held
                    return;
                }
            }
            if (time >= arrival)
            {
                if (releasedAfterComplete)
                    ThrowTier(owner, next);
                else
                {
                    Projectile.ai[0] = Encode(Phase.Charge, next); // the head is home and the hold goes on
                    Projectile.netUpdate = true;
                }
            }
        }

        // ---- the special ----

        void UpdateSpecial(Player owner)
        {
            Vector2 toTarget = specialTarget - Projectile.Center;
            float speed = SunfuryTuning.SpecialSpeed;
            bool arrived = toTarget.Length() <= speed;
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = arrived ? specialTarget : Projectile.Center + toTarget.SafeNormalize(Vector2.UnitX) * speed;
            trail?.Add(Projectile.Center);
            SunfuryFx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 1.4f, 0.9f, 0.3f);

            if (!IsOwner)
                return;
            bool blocked = SunfuryTuning.SpecialStopsOnTiles && phaseTimer > 2 && Collision.SolidCollision(Projectile.Center - new Vector2(6f), 12, 12);
            if (arrived || blocked)
                Detonate();
        }

        // Owner only: the sun goes off where the head is, and the head starts its way home.
        void Detonate()
        {
            int damage = Math.Max(1, (int)(lockedDamage * SunfuryTuning.BlastDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SunfuryBlastProjectile>(),
                damage, Projectile.knockBack * SunfuryTuning.BlastKnockbackMultiplier, Projectile.owner);
            Projectile.ai[0] = Encode(Phase.Retract, 0);
            Projectile.netUpdate = true;
        }

        // The head comes straight back to the hand (after the explosion, or after a mistake) and the loop ends there.
        // restartOnHold (after the special only): the owner decides at the catch. Button held (pressed at any moment during the return,
        // or still down) = the same projectile goes straight into tier 1's spin; otherwise it ends. Other machines wait at the hand for that
        // decision (it arrives through ai[0]) and only end their copy themselves if it never comes.
        void UpdateReturn(Player owner, float speed, int hangTicks, bool restartOnHold, bool holding)
        {
            if (phaseTimer < hangTicks)
                return; // hanging where the sun went off; the head is hidden
            Vector2 toHand = owner.MountedCenter - Projectile.Center;
            if (toHand.Length() <= speed + SunfuryTuning.CatchDistance)
            {
                if (restartOnHold && !IsOwner && parkedTicks++ < 20)
                {
                    if (parkedTicks == 1)
                        SunfuryFx.CatchHead(owner.MountedCenter);
                    return;
                }
                SunfuryFx.CatchHead(owner.MountedCenter);
                if (restartOnHold && IsOwner && holding)
                {
                    releasedAfterComplete = false;
                    Projectile.ai[0] = Encode(Phase.Charge, 1); // the hold goes on: a fresh loop at tier 1, same head
                    Projectile.netUpdate = true;
                    return;
                }
                Projectile.Kill();
                return;
            }
            Projectile.Center += toHand.SafeNormalize(Vector2.UnitX) * speed;
            if (CurrentPhase == Phase.Retract)
            {
                trail?.Add(Projectile.Center);
                SunfuryFx.FlightDust(Projectile.Center, toHand, 2);
            }
        }

        // ---- pose, hits, kill ----

        void ApplyPose(Player owner)
        {
            float armAngle;
            if (CurrentPhase == Phase.Charge)
                armAngle = Facing == 1 ? -1.1f : MathHelper.Pi + 1.1f; // raised while spinning
            else
                armAngle = (Projectile.Center - owner.MountedCenter).ToRotation();
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
            owner.itemAnimation = 2;            // keeps the item "in use" for as long as the head is out
            owner.itemTime = 2;
        }

        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(HeadTexture(), SunfuryTuning.AfterimageTicks, 0.7f);
            afterimages.Tick();
            if (!HeadHidden && (Damaging || CurrentPhase == Phase.Retract))
                afterimages.Add(Projectile.Center, headRotation, 1f, SunfuryTuning.GlowColor(LookTier) * 0.8f);
        }

        // The head is not drawn while it hangs inside the explosion.
        bool HeadHidden => CurrentPhase == Phase.Retract && phaseTimer < SunfuryTuning.SpecialHangTicks;

        // While spinning, the head hits everything along the path it travelled this tick, so a fast spin never skips an enemy.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CurrentPhase != Phase.Charge)
                return null;
            Vector2 start = Vector2.DistanceSquared(sweepStart, Projectile.Center) > 120f * 120f ? Projectile.Center : sweepStart;
            float unused = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, Projectile.Center, SunfuryTuning.HeadSize, ref unused);
        }

        public override bool? CanDamage()
        {
            return CanHurt ? (bool?)null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SunfuryFx.HitSparks(target.Center, LookTier);
            target.AddBuff(BuffID.OnFire, SunfuryTuning.BurnTicks);
        }

        // The trail and the afterimages keep fading on their own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            DetachedTrails.Hand(afterimages);
            trail = null;
            afterimages = null;
        }

        // ---- drawing ----

        Texture2D HeadTexture()
        {
            return art != null && art.IsLoaded ? art.Value : TextureAssets.Projectile[Type].Value;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Owner;
            afterimages?.Draw();
            trail?.Draw();
            SunfuryFx.DrawLayers();

            DrawChain(owner.MountedCenter, Projectile.Center); // the chain stays while the hidden head hangs in the explosion
            
            if (HeadHidden)
                return false;

            Texture2D texture = HeadTexture();
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            int tier = LookTier;
            float progress = CurrentPhase == Phase.Charge || CurrentPhase == Phase.Caught
                ? MathHelper.Clamp(chargeTimer / (float)Math.Max(1, SunfuryTuning.ChargeTicks(ChargingTier)), 0f, 1f)
                : 1f;
            float glow = (chargeComplete ? 0.9f : 0.15f + 0.5f * progress) + flash;
            if (Damaging)
                glow = 0.7f + flash;

            Main.EntitySpriteDraw(texture, position, null, SunfuryTuning.GlowColor(tier) * (0.7f * glow), headRotation, origin, 1.35f + 0.25f * flash, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, SunfuryTuning.HotColor(tier) * (0.6f * glow), headRotation, origin, 1.15f + 0.1f * flash, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.5f + 0.4f * flash), headRotation, origin, 1f, SpriteEffects.None, 0);

            // A ring of pixels pulses around the head when a tier's charge completes.
            //if (flash > 0.01f)
            //{
            //    float grid = SunfuryTuning.PixelSize;
            //    float ringRadius = 18f + 30f * (1f - flash);
            //    int count = Math.Max(10, (int)(MathHelper.TwoPi * ringRadius / (grid * 3f)));
            //    for (int i = 0; i < count; i++)
            //        IceFx.DrawPixel(Projectile.Center + (MathHelper.TwoPi * i / count).ToRotationVector2() * ringRadius, grid * 2f, SunfuryTuning.HotColor(tier) * flash, grid);
            //}
            return false;
        }

        // The vanilla flail chain: the chain texture tiled from the head to the hand, every link lit by the light where it hangs.
        void DrawChain(Vector2 hand, Vector2 head)
        {
            chainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + SunfuryTuning.ChainName);
            Texture2D chain = chainAsset.Value;
            Vector2 span = hand - head;
            float length = span.Length();
            if (length < 1f)
                return;
            Vector2 direction = span / length;
            float rotation = span.ToRotation() - MathHelper.PiOver2;
            Vector2 origin = chain.Size() * 0.5f;
            float step = Math.Max(1, chain.Height);
            for (float d = step * 0.5f; d < length; d += step)
            {
                Vector2 position = head + direction * d;
                Color color = Lighting.GetColor((int)(position.X / 16f), (int)(position.Y / 16f));
                Main.EntitySpriteDraw(chain, position - Main.screenPosition, null, color, rotation, origin, 1f, SpriteEffects.None, 0);
            }
        }
    }
}