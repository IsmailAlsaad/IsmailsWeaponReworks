using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// Every tunable number of the Bee Keeper rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Swing arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the
    /// swing starts): 0 = at the cursor, negative = above it, positive = below it.
    /// </summary>
    public static class BeeKeeperTuning
    {
        // ---- sword ----
        public static float BladeLength => 50f;       // hand to tip, in pixels at scale 1
        public static float SwordScale => 1.1f;

        // ---- swing (the Starfury's: alternates down and up, centered on the cursor; fixed ticks, NOT scaled by melee speed) ----
        public static int AnticipateTicks => 8;
        public static float AnticipateDeg => 20f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 10f; // and the wrist (the blade tilt) the same way
        public static int SwingTicks => 7;            // the strike (the only part that damages)
        public static float SwingEasePower => 2.5f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 6f;       // the strike carries this far past the end of the arc ...
        public static int RecoverTicks => 5;          // ... and settles back to the end of the arc over this long
        public static int SweepHitSteps => 4;         // the blade's hit test is checked at this many poses between two ticks
        public static float RestArmDeg => 10f;        // where a fresh combo starts (just before the high end of the arc)
        public static float SwingHighDeg => -70f;     // top of the arc
        public static float SwingLowDeg => 70f;       // bottom of the arc
        public static float TiltHighDeg => -12f;      // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 12f;        // ... and at the bottom
        public static int ComboTicks => 30;           // a swing started within this long after the last one continues the alternation
        public static float HitLengthScale => 1f;     // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;          // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static float HitShake => 1f;           // camera punch on a hit (pixels)

        // ---- swing trail (a SweepTrail fan, drawn by the shared pixel line renderer) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailOpacity => 0.5f;
        public static float TailLifeScale => 0.45f;   // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;            // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.9f;    // the fan has fully expanded this far into the swing (0..1)
        public static float TrailInner => 0.55f;      // fully expanded, the fan reaches down the blade to this fraction of its length
        public static float TrailOuter => 1.3f;         // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0.8f;    // how many ticks the trail runs BEHIND the sprite (0 = exactly on it; 0.8 is what it was before)
        public static int TrailSamplesPerTick => 8;   // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static Color CoreColor => new Color(255, 244, 180, 0);
        public static Color HoneyColor => new Color(255, 176, 40, 0);
        public static Color DeepColor => new Color(205, 95, 20, 0);

        // ---- the swarm ----
        public static int MaxBig => 5;
        public static int MaxSmall => 10;
        public static int SpawnIntervalTicks => 50;   // one new bee this often while the swarm is not full
        public static int LingerTicks => 280;         // the swarm stays this long after the sword is put away, then leaves
        public static int LeaveTicks => 24;           // a leaving bee shrinks and rises for this long, then is gone
        public static int BigSize => 22;              // hitbox of a big bee (square, pixels)
        public static int SmallSize => 14;
        public static float BigScale => 1f;           // drawn size
        public static float SmallScale => 0.85f;
        public static int PopTicks => 10;             // a new bee grows to full size over this long
        public static int FlapTicks => 3;             // ticks per wing frame
        public static int ArtFrames => 1;             // frames of YOUR bee art (the vanilla stand-in reads its own frame count)
        public static bool VanillaBeeFacesRight => true; // the stand-in vanilla bee sprite: flip this if it flies backwards

        // ---- hover: the bees circle above the player's head ----
        public static float HoverHeight => 38f;       // the middle of the swarm, above the player's top
        public static float BigRadiusX => 46f;        // how far sideways a big bee wanders
        public static float SmallRadiusX => 62f;
        public static float HoverRadiusY => 22f;
        public static float FlutterSpeed => 0.045f;   // radians per tick of the wander (big bees use 70%)
        public static float Spring => 0.02f;          // pull toward the wander point
        public static float Damping => 0.92f;         // velocity kept per tick
        public static float MaxSpeed => 12f;
        public static float BuzzJitter => 0.25f;      // random shake added to the velocity
        public static float SnapDistance => 900f;     // a bee further away than this jumps back to the swarm

        // ---- the swarm attack (a true melee hit sends bees: they fly out, latch on, sting) ----
        public static int MaxBeesPerEnemy => 3;        // at most this many of a player's bees fly to or sit on one enemy at a time
        public static int BeesPerHit => 2;             // bees sent by the FIRST hit on an enemy ...
        public static int HitsForFullSwarm => 5;       // ... growing evenly until this hit, which sends every idle bee
        public static int HitMemoryTicks => 240;       // an enemy's hit count is forgotten after this long without a new hit on it
        public static float RecruitKick => 5f;         // a recruited bee is thrown off toward the enemy at this speed ...
        public static float RecruitKickSpread => 0.9f; // ... turned by a random angle up to this (radians)
        public static float TravelSpeed => 15f;        // top speed of a bee flying to its enemy
        public static float TravelTurn => 0.18f;       // how fast its velocity follows the wanted one (0..1 per tick)
        public static float TravelWeave => 3.5f;       // sideways swoop (speed) while it is still far away
        public static float TravelArriveDistance => 12f;
        public static int TravelTimeoutTicks => 120;   // not there after this long: it latches anyway (snaps onto the enemy)
        public static float LatchSpread => 0.42f;      // latched bees sit within this fraction of the enemy's size around its centre ...
        public static float LatchMaxSpread => 70f;     // ... but never further than this (pixels), so a boss does not scatter them
        public static float LatchWobble => 5f;         // a latched bee jitters this far around its spot
        public static float LatchWobbleSpeed => 0.35f;
        public static int StingIntervalTicks => 80;    // every latched bee stings this often (each has its own phase)
        public static float StingDamageFraction => 0.1f; // of the sword's damage (the swing's damage when the bee was sent)
        public static int StingPoisonTicks => 120;     // Poisoned is topped up to this whenever it runs below half
        public static int MaxLatchTicks => 360;        // a bee lets go after this long, with one final sting
        public static float StingJab => 5f;            // a sting pushes the bee this far into the enemy and back
        public static int StingJabTicks => 6;
        public static float StingSoundChance => 0.3f;  // not every sting makes a sound (15 bees would be a wall of noise)

        // ---- honey (patch 3): latched bees gather it, the released bees fly it home and hand it over ----
        public static int HoneyFillTicks => 300;          // a bee that has sat on an enemy this long carries a full load (it fills evenly)
        public static float HoneyMinCarry => 0.05f;       // a bee that gathered less than this goes straight back to the swarm instead of flying home

        // the bees' orange outline (the bee's own sprite repeated around itself; its strength is the honey the bee holds) and the drips off the enemy
        public static float HoneyGlowAlpha => 0.3f;       // opacity of the soft glow copy behind the outline
        public static float HoneyOutlineEase => 0.08f;    // how fast the outline fades in and out (0..1 per tick)
        public static float HoneyOutlineWidth => 1.5f;    // thickness at full strength (pixels; a bee is tiny)
        public static float HoneyOutlineCoreFraction => 0.5f; // the brighter inner ring sits at this fraction of the outline width
        public static int HoneyOutlinePulseTicks => 50;
        public static float HoneyOutlineMin => 0.65f;     // the pulse swings between these fractions of the strength
        public static float HoneyOutlineMax => 1f;
        public static float HoneyGlowScale => 1.08f;      // the soft glow copy behind the outline
        public static Color HoneyOutlineColor => new Color(255, 150, 20, 0);   // alpha 0 = additive
        public static Color HoneyOutlineCoreColor => new Color(255, 215, 90, 0);
        public static float HoneyDripChance => 0.05f;       // per latched bee per tick, times its fill
        public static float HoneyReturnDripChance => 0.04f; // per returning bee per tick, times its load
        public static float HoneyDripGravity => 0.12f;
        public static int HoneyDripLifeMin => 40;
        public static int HoneyDripLifeMax => 80;
        public static float HoneyDripSizeMin => 2f;
        public static float HoneyDripSizeMax => 3.4f;

        // the metaball look of the drips and of the blob a bee carries
        public static float HoneyPixelSize => 2f;
        public static float HoneyMerge => 2.2f;
        public static float HoneyBlobOutline => 0.22f;
        public static float HoneyGlow => 0.25f;
        public static float HoneyDripHeat => 0.55f;
        public static Color HoneyFillDark => new Color(190, 85, 15);
        public static Color HoneyFillLight => new Color(255, 200, 60);
        public static Color HoneyFillShine => new Color(255, 238, 160);
        public static Color HoneyDripOutlineColor => new Color(115, 45, 8);
        public static Color HoneyGlowColor => new Color(255, 160, 30);

        public static Color HoneyFill(float heat)
        {
            heat = MathHelper.Clamp(heat, 0f, 1f);
            return heat < 0.5f ? Color.Lerp(HoneyFillDark, HoneyFillLight, heat * 2f) : Color.Lerp(HoneyFillLight, HoneyFillShine, (heat - 0.5f) * 2f);
        }

        // the blob a returning bee carries: it hangs under the bee and is bigger with a fuller load
        public static float HoneyBlobRadius => 4f;        // at a full load, on a big bee
        public static float HoneyBlobMinScale => 0.35f;   // an almost empty load is this fraction of that
        public static float HoneyBlobSmallScale => 0.8f;  // a small bee carries (and hangs) this fraction
        public static float HoneyBlobOffset => 6f;        // how far under the bee's middle
        public static float HoneyBlobHeat => 0.55f;

        // ---- the flight home (every machine flies it, the owner's client decides it has arrived) ----
        public static float ReturnSpeed => 14f;
        public static float ReturnTurn => 0.14f;          // how fast its velocity follows the wanted one (0..1 per tick)
        public static float ReturnWeave => 2.5f;          // sideways swoop (speed) while still far away
        public static float ReturnArriveDistance => 22f;  // close enough to the player's middle to hand the honey over
        public static int ReturnTimeoutTicks => 300;      // not there after this long: it hands the honey over anyway

        // ---- delivery (owner's client): the honey of the bees that arrive within DeliveryBatchTicks of each other is given at once ----
        public static float HealPerHoneyBee => 3f;        // health per FULL load (a half-full bee gives half)
        public static int HealMaxPerBatch => 60;
        public static int HoneyBuffTicksPerBee => 240;    // ticks of the vanilla Honey buff per full load ...
        public static int HoneyBuffMaxTicks => 1800;      // ... never topped up beyond this
        public static int DeliveryBatchTicks => 10;
        public static float DeliverSoundChance => 0.4f;   // not every arriving bee makes a sound
    }
}