using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Every tunable number of the Candy Cane Sword (ticks: 60 = 1 second, lengths in pixels). The SWING itself (the arm, the chained chops, the idle
    /// pose, the hit line) is exactly the wooden swords': CandyCaneSwordBladeTune forwards it to WoodenSwordTuning. Only the trail, the candies and
    /// the Sugar Rush buff are separate. Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class CandyCaneSwordTuning
    {
        // ---- when a candy appears ----
        public static float SpawnChance => 0.25f;       // per true melee hit that does not kill (1/4)
        public static float GreenChanceFullHealth => 0.05f; // chance that a new candy is green at full health (the other kinds share the rest equally)
        public static float GreenChanceNoHealth => 0.40f;   // the same at no health; linear in between (the owner's health when the candy is rolled)
        public static bool BlueEnabled => true;         // false: the random pick skips blue candies
        public static int BurstMin => 6;                // candies shot upward when a hit of the sword kills an enemy
        public static int BurstMax => 8;
        public static float BurstSpreadDeg => 50f;      // the fan is straight up +- this many degrees
        public static float BurstSpeedMin => 5f;
        public static float BurstSpeedMax => 9f;
        public static float PopSpeedMin => 4.5f;        // upward speed of a single candy from a normal hit
        public static float PopSpeedMax => 7f;
        public static float PopSideSpeed => 2.5f;       // sideways speed, random both ways
        public static int MaxCandies => 24;             // alive per player; one more makes the oldest break

        // ---- lifetimes ----
        public static int RedLifeTicks => 360;
        public static int GreenLifeTicks => 600;
        public static int BlueLifeTicks => 480;
        public static int YellowLifeTicks => 600;
        public static int PopInTicks => 6;              // it grows from half size to full size this fast

        // ---- physics (every machine runs the same) ----
        public static float Gravity => 0.28f;
        public static float MaxFallSpeed => 12f;
        public static float AirDrag => 0.995f;          // horizontal speed kept per tick in the air
        public static float BounceKeep => 0.45f;        // speed kept after a bounce off a block
        public static float GroundFriction => 0.86f;    // horizontal speed kept per tick on the ground
        public static float SettleSpeed => 1.2f;        // a bounce slower than this stops
        public static float SpinMin => 0.06f;           // radians per tick in the air, random direction
        public static float SpinMax => 0.22f;
        public static float GroundSpinKeep => 0.85f;    // spin kept per tick on the ground

        // ---- pickup (green and yellow) ----
        public static int PickupDelayTicks => 25;       // a fresh candy cannot be picked up yet
        public static int PickupRange => 28;            // how far outside the player's box a candy is picked up
        public static float MagnetRange => 110f;        // a candy drifts toward the nearest player who could pick it up
        public static float MagnetAccel => 0.9f;
        public static float MagnetMaxSpeed => 10f;
        public static float MagnetDamp => 0.96f;
        public static int RequestRetryTicks => 20;      // a client asks the server again after this long if nothing came back
        public static float ServerReachSlack => 140f;   // the server accepts a pickup this far beyond (pickup + magnet range)
        public static int HealAmount => 5;              // green candy
        public static bool HealOnlyWhenHurt => false;   // true: a green candy is left alone by a player at full health

        // ---- Sugar Rush (yellow candy) ----
        public static int SugarRushTicksPerCandy => 120;
        public static int SugarRushMaxTicks => 600;
        public static float SugarRushAttackSpeed => 0.30f; // melee attack speed
        public static float SugarRushMoveSpeed => 0.15f;   // movement speed

        // ---- the purple seed (getfixedboi, see CandyCaneSwordPurple) ----
        public static int GrapeTicks => 600;            // Grape buff: a green or yellow candy refreshes it to this long (600 = 10 s)
        public static int PurpleStyleCount => 2;        // PurpleCandy.png: 4 columns (green, blue, yellow, red) x 2 rows; a candy's style picks the row
        public static Color PurpleText => new Color(185, 110, 255);   // the two tooltip lines

        // ---- red candy explosion ----
        public static float RedBlastRadius => 56f;
        public static int RedBlastTicks => 3;
        public static float RedBlastDamageMultiplier => 1f; // of the sword's damage
        public static float RedBlastKnockback => 4f;
        public static float RedBlastShake => 2f;

        // ---- blue candy (sticks to an enemy or a block; stuck to an enemy it hurts it every interval until it breaks) ----
        public static int BlueStickDelayTicks => 10;       // a fresh candy cannot stick to an enemy yet (it pops up first); a block it touches sticks it at once
        public static int BlueSourceIgnoreTicks => 60;     // and it ignores the enemy it popped out of for this long (60 = 1 second)
        public static float BlueStickReach => 4f;          // how far outside an enemy's box a blue candy still counts as touching it
        public static int BlueMaxPerEnemy => 3;            // per player on one enemy; one more pushes the oldest off
        public static float BlueDamageMultiplier => 1f; // of the sword's damage, per hit
        public static int BlueHitIntervalTicks => 60;      // 60 = once per second
        public static float StickSoundVolume => 0.4f;

        // ---- look (Candy.png: 5 columns x 8 rows of 40 x 40 cells; rows 0-1 red, 2-3 green, 4-5 blue, 6-7 yellow) ----
        public static int HitboxSize => 12;
        public static int CellSize => 40;
        public static int Columns => 5;
        public static int StyleCount => 10;             // 2 rows x 5 columns per kind
        public static float DrawScale => 1f;
        public static float FallbackSize => 14f;        // the colored square drawn until Candy.png exists
        public static float LightStrength => 0.25f;
        public static float GripOffset => 5f;           // where the hand holds the sword: pixels up from the end of the handle, along the blade, at scale 1 (the sprite is drawn that much further back; visual only, the reach and the hit line stay)
        public static bool ShowHitboxDebug => false;    // true: a magenta circle on every candy's hitbox (radius = half of HitboxSize) and a dot on its center

        // ---- dust, sound, shake ----
        public static int HitSparkCount => 3;
        public static int SpawnDustCount => 4;
        public static int BreakDustCount => 8;
        public static int PickupDustCount => 10;
        public static int BlastDustCount => 24;
        public static int BlastSmokeCount => 6;
        public static int KillBurstDustCount => 16;
        public static float KillBurstShake => 1.5f;
        public static float SwingSoundVolume => 0.7f;
        public static float SpawnSoundVolume => 0.3f;
        public static float BreakSoundVolume => 0.5f;
        public static float PickupSoundVolume => 0.6f;
        public static float BlastSoundVolume => 0.6f;

        // ---- swing trail geometry (a SweepTrail fan, the same numbers as the ore swords) ----
        public static int TrailSamplesPerTick => 8;
        public static float TrailLagTicksDown => 1f; // how many ticks the trail lags behind the sword on the DOWNWARD swing (starts equal to the upward one: put its own number here)
        public static float TrailLagTicksUp => 0f;
        public static float TrailOuter => 0.9f;
        public static float TrailInnerLow => 0.4f;
        public static float TrailInnerHigh => 0.1f;
        public static float TrailExpandBy => 0.8f;

        // ---- swing trail look ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 12;
        public static float TailLifeScale => 0.45f;
        public static int TrailRails => 6;
        public static float TrailOpacity => 0.3f;
        public static Color TrailCore => new Color(255, 255, 255);
        public static float TrailDeepScale => 0.55f; // the trail's outer edge is the candy's color at this brightness (the middle is the color itself)
    }
}