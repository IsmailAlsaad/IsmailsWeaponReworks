# ==============================================================================
# WEAPON REWORKS PATCH: Dark Lance fix round 1 (v2, compiler-safe shader) - detached shader embers, smooth halos, orb trail on PixelTrail
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_darklance_embers2.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_darklance_embers2.ps1 -DryRun    (verify only)
# Engine lives in the repo at tools\patch_engine.ps1 (all-or-nothing, see its header).
# ==============================================================================
param(
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks',
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'
$engine = Join-Path $Root 'tools\patch_engine.ps1'
if (-not (Test-Path -LiteralPath $engine)) { Write-Host "Missing $engine - copy/commit tools\patch_engine.ps1 first." -ForegroundColor Red; exit 1 }
. $engine

# Literal here-strings: no escaping needed. The closing '@ must start its line, and
# no line INSIDE the text may start with '@ . Copy Find text exactly (tabs vs spaces!).

Edit -File 'Effects\DarkFlameOrb.fx' -Label 'orb shader: header comment' -Find @'
// Dark flame orb of the Dark Lance: a pixelated, procedural ball of purple shadowflame with tongues that rise and flicker. One pixel
// shader, no particles. Drawn by DarkLanceFx.DrawOrb through Common/PixelShaderFx over a square quad centered on the orb.
// Uses only the standard uniforms of PixelShared.fxh:
//   uParams.x = world px per shader unit (the orb's body radius is 0.5 units), uParams.y = pop-in scale (0..1.25, 1 = full size)
//   uTime = seconds, uSeed = per-orb seed, uIntensity = flicker strength, uBands = number of color bands
//   uColorA / uColorB / uColorC = outer / mid / core (the palette ramp)
// Output is premultiplied: alpha 1 = solid flame / ember pixel, alpha 0 = additive glow (the PixelLine convention).

'@ -Replace @'
// Dark flame orb of the Dark Lance: a pixelated, procedural ball of purple shadowflame. One pixel shader, no particle objects.
// The ball is noise-shaped. The embers are "hidden circles": 10 per orb, each one a pure function of time (its own phase and its own
// random numbers every life), pixelated and merged into one blob field with the ball. An ember breaks off the upper rim of the ball,
// rises, drifts sideways, cools (bright, to dark, to gone) and shrinks. Once broken off it stays where it was in the WORLD: uOrbit and
// uOwnerVel tell the shader how the orb has moved since, so the embers are left behind by a turning or running orb.
// Drawn by DarkLanceFx.DrawOrb through Common/PixelShaderFx over a square quad centered on the orb.
// Standard uniforms of PixelShared.fxh:
//   uParams.x = world px per shader unit (the ball's radius is 0.5 units), .y = pop-in scale (0..1.25, 1 = full size),
//   .z = halo reach in units beyond the ball, .w = halo strength
//   uParams2.x = ember rise height (units), .y = ember life (seconds), .z = ember radius (units), .w = ember sideways drift (units)
//   uTime = seconds, uSeed = per-orb seed, uIntensity = flicker strength, uBands = number of color bands
//   uColorA / uColorB / uColorC = outer / mid / core (the palette ramp)
// Own uniforms: uOrbit.xy = vector from the orb to the center it orbits (units), uOrbit.z = angular speed (rad/s), uOrbit.w = how much
//   the embers stay behind in the world (0..1); uOwnerVel = velocity of that center (units per second).
// Output is premultiplied: alpha 1 = solid flame / ember pixel, alpha 0 = additive glow (the PixelLine convention). The halo is a smooth falloff.

'@

Edit -File 'Effects\DarkFlameOrb.fx' -Label 'orb shader: embers + smooth halo' -Find @'
#include "PixelShared.fxh"

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // shader space: the orb's center at the origin, +y = down on screen; the pixels snap to the world grid
    float2 center = uOrigin + uSize * 0.5;
    float2 q = (pixelCenter(coords) - center) / max(uParams.x, 1.0);
    q /= max(uParams.y, 0.05);                              // pop-in: the whole orb grows
    float t = uTime;

    float up = max(-q.y, 0.0);                              // how far above the center (the flames rise)
    float2 sp = q * 3.2 + seedShift() + float2(0.0, t * 2.4);   // the noise slides down = the flames flow up
    float n1 = fbm4(sp);
    float n2 = fbm3(sp * 1.9 + float2(t * 0.7, 7.0) + n1 * 1.5);   // domain warp

    float2 wv = q;
    wv.x += (n1 - 0.5) * 0.45 * (0.15 + up);               // the tongues sway more the higher they are
    float k = lerp(1.0, 0.55, step(wv.y, 0.0));             // stretched upward
    float d = length(float2(wv.x, wv.y * k));
    float R = 0.5;

    float heat = ((R - d) / R + (n2 - 0.5) * 0.9 * (0.3 + up * 1.1)) * uIntensity;

    // posterize into hard color bands
    float idx = bandIndex(heat, uBands);
    float inFlame = step(1.0, idx) * step(0.001, heat);
    float3 flameCol = paletteRamp(saturate((idx - 1.0) / max(uBands - 1.0, 1.0)));

    // additive halo around the body (two hard levels)
    float gd = max(d - R, 0.0);
    float glow = exp(-gd * 3.5) * 0.9;
    float glowOn = step(0.5, glow);
    float3 glowCol = uColorA * lerp(0.7, 1.2, step(0.75, glow));

    // embers rising from the top (about 2x2 pixels each)
    float2 sc = q * (uParams.x / (uGrid * 2.0)) + float2(0.0, t * 12.0);
    float h = hash(floor(sc) + floor(t * 3.0) + seedShift());
    float spark = step(0.985, h) * step(0.15, up) * step(abs(q.x), 0.12 + up * 0.25) * step(up, 1.1);
    float3 sparkCol = lerp(uColorC, uColorB, saturate(up));

    float4 outc = float4(glowCol * glowOn, 0.0);
    outc = lerp(outc, float4(sparkCol, 1.0), spark);
    outc = lerp(outc, float4(flameCol, 1.0), inFlame);
    return outc;
}
'@ -Replace @'
#include "PixelShared.fxh"

float4 uOrbit;
float2 uOwnerVel;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // shader space: the orb's center at the origin, +y = down on screen; the pixels snap to the world grid
    float2 center = uOrigin + uSize * 0.5;
    float2 q = (pixelCenter(coords) - center) / max(uParams.x, 1.0);
    float appear = max(uParams.y, 0.05);
    float2 qb = q / appear;                                 // pop-in: the ball and its halo grow
    float grow = min(appear, 1.0);
    float t = uTime;
    float R = 0.5;
    float bd = length(qb);

    // ---- the ball: noise-shaped, hard color bands, only a short flicker of tongues (the long ones are the embers now) ----
    float up = max(-qb.y, 0.0);                             // how far above the center
    float2 sp = qb * 3.2 + seedShift() + float2(0.0, t * 2.4);   // the noise slides down = the flames flow up
    float n1 = fbm4(sp);
    float n2 = fbm3(sp * 1.9 + float2(t * 0.7, 7.0) + n1 * 1.5);   // domain warp
    float2 wv = qb;
    wv.x += (n1 - 0.5) * 0.3 * (0.1 + up);
    float k = lerp(1.0, 0.8, step(wv.y, 0.0));              // only a little stretched upward
    float d = length(float2(wv.x, wv.y * k));
    float heat = ((R - d) / R + (n2 - 0.5) * 0.55 * (0.25 + up * 0.5)) * uIntensity;
    float idx = bandIndex(heat, uBands);
    float inFlame = step(1.0, idx) * step(0.001, heat);
    float3 flameCol = paletteRamp(saturate((idx - 1.0) / max(uBands - 1.0, 1.0)));

    // ---- soft halo: a smooth additive falloff that reaches exactly zero at its reach (no ring that ends abruptly) ----
    float reach = max(uParams.z, 0.1);
    float gk = 3.0 / reach;
    float gEdge = exp(-reach * gk);
    float gd = max(bd - R, 0.0);
    float glow = saturate((exp(-gd * gk) - gEdge) / (1.0 - gEdge)) * uParams.w * uIntensity;
    float3 glowCol = lerp(uColorA, uColorB, saturate(glow * 0.8)) * glow;

    // ---- embers: circles that nobody sees, pixelated and merged. The ball is part of the field, so a fresh ember grows out of it ----
    float life = max(uParams2.y, 0.05);
    float bodyField = 0.42 / max(bd, 0.05);
    bodyField *= bodyField;
    float F = bodyField;                                    // summed field of everything: >= 1 = inside the merged blob
    float wsum = bodyField * bodyField;                     // weights for averaging the temperature (nearer circles count more)
    float tsum = 0.55 * wsum;                               // the ball's own temperature is a middle one
    [loop]
    for (int i = 0; i < 10; i++)
    {
        float fi = i;
        float x = t / life + hash(float2(fi * 1.37 + 3.1 + uSeed * 0.173, 4.7 + uSeed * 0.311));
        float cyc = floor(x);                               // which life of this ember it is
        float ph = x - cyc;                                 // 0 = just broke off, 1 = gone
        float2 r = hash2(float2(fi * 7.7 + cyc * 3.3 + 1.0, uSeed * 0.57 + 9.0));   // new random numbers every life
        float dt = ph * life;                               // seconds since it broke off

        // where the orb was at that moment, relative to where it is now: it turned on its orbit and moved with its owner
        float2 back = (uOrbit.xy - rotate2(uOrbit.xy, -uOrbit.z * dt) - uOwnerVel * dt) * uOrbit.w;

        float ang = -1.5708 + (r.x - 0.5) * 2.4;            // breaks off somewhere on the upper rim
        float2 start = float2(cos(ang), sin(ang)) * (R * 0.8 * grow);
        float rise = uParams2.x * ph * (1.25 - 0.25 * ph);  // quick at first, slower as it cools
        float sway = (r.x - 0.5) * 2.0 * ph + 0.35 * sin(ph * 5.0 + r.y * 6.283) * ph;
        float2 pos = back + start + float2(sway * uParams2.w, -rise);

        float rad = uParams2.z * (0.6 + 0.8 * r.y) * (1.0 - ph * ph) * grow;   // shrinks to nothing
        float2 dv = q - pos;
        float f = rad * rad / max(dot(dv, dv), 0.0004);
        float wgt = f * f;
        F += f;
        wsum += wgt;
        tsum += (1.0 - ph) * wgt;                           // hot at birth, cold at the end
    }
    float emberOn = step(1.0, F);
    float temp = tsum / max(wsum, 0.00001);
    float3 emberCol = paletteRamp(posterize(temp, uBands)) * lerp(0.4, 1.0, saturate(temp * 2.5));   // a cold ember is dark ash

    float4 outc = float4(glowCol, 0.0);
    outc = lerp(outc, float4(emberCol, 1.0), emberOn);
    outc = lerp(outc, float4(flameCol, 1.0), inFlame);
    return outc;
}
'@

NewFile -File 'Effects\PixelGlow.fx' -Label 'new shader: soft round glow' -Content @'
// A soft round glow with a SMOOTH falloff (no hard rings): bright in the middle, fading to exactly nothing at the radius. Pixelated to the
// world grid (the pixels are still blocks, only their brightness is smooth). First user: the Dark Lance's tip glow while it charges.
// Standard uniforms of PixelShared.fxh:
//   uParams.x = radius in world px (the glow is zero from there on), .y = falloff power (1 = soft cone, 2 = tight bright core), .z = peak strength
//   uIntensity = extra strength multiplier (pulse), uColorA / uColorB / uColorC = outer / mid / core
// Output: premultiplied, alpha 0 = additive.

#include "PixelShared.fxh"

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 center = uOrigin + uSize * 0.5;
    float r = length(pixelCenter(coords) - center) / max(uParams.x, 1.0);
    float g = pow(1.0 - smoothstep(0.0, 1.0, r), max(uParams.y, 0.1));      // 1 in the middle, 0 at the radius, smooth at both ends
    float3 col = ramp3(g, uColorA, uColorB, uColorC);
    return float4(col * (g * uParams.z * uIntensity), 0.0);
}

technique Technique1
{
    pass PixelGlowPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceFx.cs' -Label 'DarkLanceFx: DrawOrb with embers, DrawGlow' -Find @'
        // Draws one orb: Effects/DarkFlameOrb.fx over a square quad centered on it (visual only, never synced, every machine draws it from
        // the projectile's own state). 'time' in seconds, 'seed' something every machine knows (the spawn serial), 'appear' 0 to about 1.25
        // (the pop-in with overshoot), 'flicker' around 1. A missing .xnb shows a small purple square instead of the orb.
        public static void DrawOrb(Vector2 center, float time, float seed, float appear, float flicker)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = DarkLanceTuning.PixelSize,
                Time = time,
                Seed = seed,
                Intensity = flicker,
                Bands = DarkLanceTuning.ColorBands,
                ColorA = palette.Halo,
                ColorB = palette.Mid,
                ColorC = palette.Core,
                Params = new Vector4(DarkLanceTuning.OrbUnitPx, appear, 0f, 0f),
                FallbackColor = palette.Mid
            };
            PixelShaderFx.Draw("DarkFlameOrb", center, DarkLanceTuning.OrbQuadSize, p);
        }
'@ -Replace @'
        // Draws one orb: Effects/DarkFlameOrb.fx over a square quad centered on it (visual only, never synced, every machine draws it from
        // the projectile's own state). 'time' in seconds, 'seed' something every machine knows (the spawn serial), 'appear' 0 to about 1.25
        // (the pop-in with overshoot), 'flicker' around 1. The embers break off the ball and stay where they were in the world, so the
        // shader needs to know how the orb moves: 'toOrbitCenter' = from the orb to the point it turns around (px), 'angularSpeed' = how
        // fast it turns (radians per second), 'centerVelocity' = how fast that point moves (px per second), 'lag' 0 to 1 = how much the
        // embers stay behind (0 = they travel with the orb). A missing .xnb shows a small purple square instead of the orb.
        public static void DrawOrb(Vector2 center, float time, float seed, float appear, float flicker, Vector2 toOrbitCenter, float angularSpeed, Vector2 centerVelocity, float lag)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            float unit = DarkLanceTuning.OrbUnitPx;
            Vector4 orbit = new Vector4(toOrbitCenter.X / unit, toOrbitCenter.Y / unit, angularSpeed, lag);
            Vector2 velocity = centerVelocity / unit;
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = DarkLanceTuning.PixelSize,
                Time = time,
                Seed = seed,
                Intensity = flicker,
                Bands = DarkLanceTuning.ColorBands,
                ColorA = palette.Halo,
                ColorB = palette.Mid,
                ColorC = palette.Core,
                Params = new Vector4(unit, appear, DarkLanceTuning.HaloReach, DarkLanceTuning.HaloStrength),
                Params2 = new Vector4(DarkLanceTuning.EmberRise, DarkLanceTuning.EmberLifeSeconds, DarkLanceTuning.EmberSize, DarkLanceTuning.EmberDrift),
                Configure = fx =>
                {
                    PixelShaderFx.Set(fx, "uOrbit", orbit);
                    PixelShaderFx.Set(fx, "uOwnerVel", velocity);
                },
                FallbackColor = palette.Mid
            };
            PixelShaderFx.Draw("DarkFlameOrb", center, DarkLanceTuning.OrbQuadSize, p);
        }

        // A soft round glow (Effects/PixelGlow.fx): the palette's core in the middle, then its mid and halo colors, fading smoothly to
        // nothing at 'radius' (px). 'strength' is the peak brightness (0 to 1). Visual only.
        public static void DrawGlow(Vector2 center, float radius, float strength)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = DarkLanceTuning.PixelSize,
                ColorA = palette.Halo,
                ColorB = palette.Mid,
                ColorC = palette.Core,
                Params = new Vector4(radius, DarkLanceTuning.TipGlowFalloff, strength, 0f),
                FallbackColor = palette.Mid
            };
            PixelShaderFx.Draw("PixelGlow", center, (int)System.Math.Ceiling(radius * 2f) + 4, p);
        }
'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceTuning.cs' -Label 'Tuning: trail replaces afterimages' -Find @'
        public static int AfterimageTicks => 10;

'@ -Replace @'
        public static int TrailLifeTicks => 10;          // the orb's trail (Common PixelTrail) fades out over this long
        public static float TrailWidth => 12f;           // px at its newest end; it shrinks as it ages

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceTuning.cs' -Label 'Tuning: bigger orb quad' -Find @'
        public static int OrbQuadSize => 80;             // world px: the square the orb shader is drawn over (must cover the glow)

'@ -Replace @'
        public static int OrbQuadSize => 288;            // world px: the square the orb shader is drawn over (must cover the halo and where the embers hang behind a moving orb; smaller = cheaper)

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceTuning.cs' -Label 'Tuning: ember, halo and tip glow numbers' -Find @'
        public static float OrbUnitPx => 28f;            // world px per shader unit (the orb's body radius is half of it)

'@ -Replace @'
        public static float OrbUnitPx => 28f;            // world px per shader unit (the orb's body radius is half of it)
        public static float HaloReach => 1.5f;           // the halo fades to nothing this many units beyond the ball (smooth falloff)
        public static float HaloStrength => 0.8f;        // brightness of the halo right at the ball (0 to 1)
        public static float EmberRise => 1.6f;           // units an ember rises before it is gone
        public static float EmberLifeSeconds => 0.75f;   // how long an ember lives (10 embers per orb, each one restarts when it is gone)
        public static float EmberSize => 0.13f;          // units: the radius of an ember at birth (random 0.6 to 1.4 times this), it shrinks to nothing
        public static float EmberDrift => 0.45f;         // units an ember wanders sideways
        public static float EmberLag => 1f;              // 1 = a broken-off ember stays where it was in the world, 0 = it travels with the orb
        public static float TipGlowMinRadius => 16f;     // px: the glow at the lance tip with no charge
        public static float TipGlowMaxRadius => 46f;     // px: fully charged
        public static float TipGlowFalloff => 1.6f;      // 1 = soft cone, 2 = tight bright core
        public static float TipGlowStrength => 0.9f;

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceChargeProjectile.cs' -Label 'Charge: no unused grid' -Find @'
            float scale = Projectile.scale;
            float grid = DarkLanceTuning.PixelSize;

'@ -Replace @'
            float scale = Projectile.scale;

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceChargeProjectile.cs' -Label 'Charge: smooth tip glow' -Find @'
            // A dark glow gathering at the tip: it swells with the charge and pulses.
            float pulse = 1f + 0.15f * (float)Math.Sin(age * 0.4f);
            float size = (10f + 26f * charge) * pulse;
            IceFx.DrawPixel(tip, size * 1.6f, DarkLanceFx.Glow(palette.Halo) * 0.5f, grid);
            IceFx.DrawPixel(tip, size, DarkLanceFx.Glow(palette.Mid) * 0.7f, grid);
            IceFx.DrawPixel(tip, size * 0.45f, DarkLanceFx.Glow(palette.Core) * 0.9f, grid);

'@ -Replace @'
            // A dark glow gathering at the tip: it swells with the charge and pulses, with a smooth falloff (no hard squares).
            float pulse = 1f + 0.15f * (float)Math.Sin(age * 0.4f);
            float radius = MathHelper.Lerp(DarkLanceTuning.TipGlowMinRadius, DarkLanceTuning.TipGlowMaxRadius, charge) * pulse;
            DarkLanceFx.DrawGlow(tip, radius, DarkLanceTuning.TipGlowStrength * (0.6f + 0.4f * charge));

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceOrbProjectile.cs' -Label 'Orb: trail field' -Find @'
        AfterimageTrail trail;

'@ -Replace @'
        PixelTrail trail;     // the shared pixel trail (Common), handed to DetachedTrails when the orb ends

'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceOrbProjectile.cs' -Label 'Orb: PixelTrail instead of afterimage squares' -Find @'
            trail ??= new AfterimageTrail(DarkLanceTuning.AfterimageTicks, PaintAfterimage);
            trail.Tick();
            trail.Add(Projectile.Center, 0f, 1f, DarkLanceFx.Glow(DarkLanceTuning.Palette.Mid));
        }

        static void PaintAfterimage(Afterimage image, float age)
        {
            float fade = 1f - age;
            IceFx.DrawPixel(image.Position, 14f * fade * image.Scale, image.Color * (0.6f * fade), DarkLanceTuning.PixelSize);
        }
'@ -Replace @'
            trail ??= new PixelTrail(new DarkLanceOrbTrailStyle(), IsOwner);
            trail.Tick();
            trail.Add(Projectile.Center);
        }
'@

Edit -File 'Content\Weapons\DarkLance\DarkLanceOrbProjectile.cs' -Label 'Orb: tell the shader how the orb moves' -Find @'
            DarkLanceFx.DrawOrb(Projectile.Center, age / 60f, Projectile.ai[1] % 64f, appear, flicker); // the seed is the spawn serial: every machine knows it

'@ -Replace @'
            // The embers break off and stay in the world: the shader is told where the orb turns around, how fast, and how fast that point moves.
            // The values come from the owner's synced position and velocity, so every machine draws the same thing (visual only).
            float lag = DarkLanceTuning.EmberLag * MathHelper.SmoothStep(0f, 1f, t);
            DarkLanceFx.DrawOrb(Projectile.Center, age / 60f, Projectile.ai[1] % 64f, appear, flicker, Owner.Center - Projectile.Center, DarkLanceTuning.OrbSpin * 60f, Owner.velocity * 60f, lag); // the seed is the spawn serial: every machine knows it

'@

NewFile -File 'Content\Weapons\DarkLance\DarkLanceOrbTrail.cs' -Label 'new: orb trail style (Common PixelTrail)' -Content @'
using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The trail an orb leaves while it flies out of the lance and turns around the player: a wide dark halo line with a thinner bright
    /// line in it, both shrinking and cooling to nothing as they age (the shared PixelTrail / PixelLine renderer). The orb hands the
    /// trail to DetachedTrails when it ends, so it fades out on its own. Visual only, nothing to sync; it does nothing to the world.
    /// </summary>
    public class DarkLanceOrbTrailStyle : PixelTrailStyle
    {
        public override float Grid => DarkLanceTuning.PixelSize;
        public override int LifeTicks => DarkLanceTuning.TrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            float width = DarkLanceTuning.TrailWidth * (1f - 0.85f * age);
            return layer == 0 ? width : width * 0.45f;
        }

        public override Color GetColor(int layer, float age)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            float fade = 1f - age;
            if (layer == 0)
                return DarkLanceFx.Glow(palette.Halo) * (0.55f * fade * fade);
            return DarkLanceFx.Glow(Color.Lerp(palette.Core, palette.Mid, age)) * (0.75f * fade);
        }
    }
}

'@

Edit -File 'docs\ROADMAP.md' -Label 'Roadmap: progress line' -Find @'
- [~] Common: shared pixel shader tool (2026-11-04, section 13):
'@ -Replace @'
- [~] Dark Lance fix round 1 (2026-11-04): orb flames break off as shader embers that stay in the world, smooth halo falloff (orb and lance tip glow), orb trail on the shared `PixelTrail` (applied, untested; shaders `DarkFlameOrb` and the new `PixelGlow` must be compiled)
- [~] Common: shared pixel shader tool (2026-11-04, section 13):
'@

Edit -File 'docs\ROADMAP.md' -Label 'Roadmap: Dark Lance fix round 1 section' -Find @'
### Spears fix round 1 (2026-11-03, applied, untested)
'@ -Replace @'
### Dark Lance fix round 1 (2026-11-04, applied, untested)

Files: `Effects/DarkFlameOrb.fx` (rewritten body), `Effects/PixelGlow.fx` (new), `Content/Weapons/DarkLance/` (`DarkLanceFx`, `DarkLanceTuning`, `DarkLanceOrbProjectile`, `DarkLanceChargeProjectile`, new `DarkLanceOrbTrail`).

- Embers: the long flame tongues of the orb are gone. 10 hidden circles per orb live inside the shader; each is a pure function of time and the orb's seed (own phase, new random numbers every life). They break off the upper rim of the ball, rise (`EmberRise`), drift sideways (`EmberDrift`), cool from the palette's core to its dark halo color and shrink to nothing over `EmberLifeSeconds`. The shader pixelates them to the world grid and merges them with the ball in one field (metaball sum), so a fresh ember grows out of the ball and separates; merged pixels take the posterized average temperature of the circles that overlap there.
- Detached: the ember stays where it was in the WORLD. `DrawOrb` passes the vector from the orb to the point it turns around, the angular speed (`OrbSpin` x 60), the owner's velocity and `EmberLag` (0 = travels with the orb, 1 = fully left behind; eased in while the orb flies out of the lance). The shader moves the birth point back along the orbit (exact for a circular orbit) and with the owner.
- Smooth halo: the orb halo is a continuous additive falloff that reaches zero at `HaloReach` (no more two hard levels ending at about 6 px); `HaloStrength` is its brightness. The lance tip glow is no longer three nested hard squares: `PixelGlow.fx` draws a smooth round glow (`TipGlowMinRadius` to `TipGlowMaxRadius` with the charge, `TipGlowFalloff`, `TipGlowStrength`). The pixels are still blocks on the world grid, only the brightness is smooth.
- Trail: the orb's trail is now a `PixelTrail` (`DarkLanceOrbTrailStyle`, two layers: dark halo line and a hot thin line), handed to `DetachedTrails` in `OnKill`. `AfterimageTicks` became `TrailLifeTicks` and `TrailWidth`.
- `OrbQuadSize` 80 to 288 so the halo and the left-behind embers fit; it is the main cost knob (every pixel of the quad runs the shader), lower it if the frame rate suffers with 8 orbs.

Multiplayer: visual only, nothing synced. Every input is something each machine already has: the orb's age and spawn serial, the owner's synced position and velocity, the shared orbit constants. No new state, no packets.

Test checklist: both shaders compile (`tools\compile_shader.ps1 -Shader DarkFlameOrb`, then `-Shader PixelGlow`) and a full Build + Reload; an orb sheds embers that visibly separate from the ball, rise, drift, darken, shrink and vanish; embers hang behind in the world when the orbit turns and when the player runs; the halo fades out smoothly with no visible edge; the lance tip glow swells smoothly with the charge, no squares; the orb trail is a smooth line and keeps fading after the orb pops; a second player sees the same; 8 orbs do not drop the frame rate (else lower `OrbQuadSize`).

### Spears fix round 1 (2026-11-03, applied, untested)
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
