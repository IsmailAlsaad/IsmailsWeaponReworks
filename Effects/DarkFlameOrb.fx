// Dark flame orb of the Dark Lance: a pixelated, procedural ball of purple shadowflame with embers that break off it. One pixel shader,
// no particle objects. Drawn by DarkLanceFx.DrawOrb through Common/PixelShaderFx over a square quad centered on the orb.
// The embers are "hidden circles": 10 per orb, each a pure function of time (its own phase, new random numbers every life), pixelated and
// merged into one blob field with the ball. An ember breaks off the upper rim, rises, drifts sideways, cools (bright to dark to gone) and
// shrinks. Once broken off it stays where it was in the WORLD: uOrbit and uOwnerVel say how the orb has moved since.
// KEEP IT SMALL: ps_3_0 allows 512 instruction slots and 32 temporary registers, and the compiler used by tools\compile_shader.ps1 is
// stricter than Windows' own (tools\check_shader.ps1). The embers are computed FIRST so that the ball's and the halo's values are not alive
// during the loop (that is what ran it out of registers). No hash2 / fbm in here, on purpose.
// Standard uniforms of PixelShared.fxh:
//   uParams.x = world px per shader unit (the ball's radius is 0.5 units), .y = pop-in scale (0..1.25, 1 = full size),
//   .z = halo reach in units beyond the ball, .w = halo strength
//   uParams2.x = ember rise height (units), .y = ember life (seconds), .z = ember radius (units), .w = ember sideways drift (units)
//   uTime = seconds, uSeed = per-orb seed, uIntensity = flicker strength, uBands = number of color bands
//   uColorA / uColorB / uColorC = outer / mid / core (the palette ramp)
// Own uniforms: uOrbit.xy = vector from the orb to the center it orbits (units), uOrbit.z = angular speed (rad/s), uOrbit.w = how much
//   the embers stay behind in the world (0..1); uOwnerVel = velocity of that center (units per second).
// Output is premultiplied: alpha 1 = solid flame / ember pixel, alpha 0 = additive glow (the PixelLine convention). The halo is a smooth falloff.

#include "PixelShared.fxh"

float4 uOrbit;
float2 uOwnerVel;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // shader space: the orb's center at the origin, +y = down on screen; the pixels snap to the world grid
    float2 q = (pixelCenter(coords) - (uOrigin + uSize * 0.5)) / max(uParams.x, 1.0);
    float appear = max(uParams.y, 0.05);
    float grow = min(appear, 1.0);
    float t = uTime;
    float R = 0.5;
    float bd = length(q / appear);                          // distance from the center (pop-in: the ball and its halo grow)

    // ---- embers FIRST: circles that nobody sees, pixelated and merged. The ball is part of the field, so a fresh ember grows out of it ----
    float life = max(uParams2.y, 0.05);
    float tl = t / life + uSeed * 0.37;
    float bodyField = 0.42 / max(bd, 0.05);
    bodyField *= bodyField;
    float F = bodyField;                                    // summed field of everything: >= 1 = inside the merged blob
    float wsum = bodyField * bodyField;                     // weights for averaging the temperature (nearer circles count more)
    float tsum = 0.55 * wsum;                               // the ball's own temperature is a middle one
    [loop]
    for (int i = 0; i < 10; i++)
    {
        float fi = i;
        float x = tl + fi * 0.1 + frac(fi * 0.7548) * 0.06; // the 10 embers are spread over one life
        float cyc = floor(x);                               // which life of this ember it is
        float ph = x - cyc;                                 // 0 = just broke off, 1 = gone
        float2 r = frac(float2(0.618034, 0.754878) * (fi * 3.7 + cyc * 7.31 + uSeed + 1.0));   // new random numbers every life
        float dt = ph * life;                               // seconds since it broke off

        // where the orb was at that moment, relative to where it is now: it turned on its orbit and moved with its owner
        float rs, rc;
        sincos(-uOrbit.z * dt, rs, rc);
        float2 turned = float2(rc * uOrbit.x - rs * uOrbit.y, rs * uOrbit.x + rc * uOrbit.y);
        float2 back = (uOrbit.xy - turned - uOwnerVel * dt) * uOrbit.w;

        float sx = (r.x - 0.5) * 1.7;                       // breaks off the upper rim, up to about 58 degrees to each side
        float2 start = float2(sx, -sqrt(max(1.0 - sx * sx, 0.0))) * (R * 0.8 * grow);
        float rise = uParams2.x * ph * (1.25 - 0.25 * ph);  // quick at first, slower as it cools
        float sway = (r.y - 0.5) * 2.0 * ph;
        float2 dv = q - (back + start + float2(sway * uParams2.w, -rise));

        float rad = uParams2.z * (0.6 + 0.8 * frac(r.x + r.y)) * (1.0 - ph * ph) * grow;   // shrinks to nothing
        float f = rad * rad / max(dot(dv, dv), 0.0004);
        float wgt = f * f;
        F += f;
        wsum += wgt;
        tsum += (1.0 - ph) * wgt;                           // hot at birth, cold at the end
    }
    float emberOn = step(1.0, F);
    float temp = tsum / max(wsum, 0.00001);
    float3 emberCol = paletteRamp(posterize(temp, uBands)) * lerp(0.4, 1.0, saturate(temp * 2.5));   // a cold ember is dark ash

    // ---- the ball: one noise lookup plus two sines, hard color bands ----
    float2 qb = q / appear;
    float up = max(-qb.y, 0.0);                             // how far above the center
    float n = vnoise(qb * 3.5 + seedShift() + float2(0.0, t * 2.6));   // the noise slides down = the flames flow up
    float wob = 0.5 * sin(qb.y * 10.0 - t * 7.0 + uSeed) + 0.5 * sin(qb.x * 13.0 + t * 5.0 + uSeed * 2.0);
    float d = length(float2(qb.x, qb.y * lerp(1.0, 0.8, step(qb.y, 0.0))));   // a little stretched upward
    float heat = ((R - d) / R + ((n - 0.5) * 0.8 + wob * 0.1) * (0.25 + up * 0.6)) * uIntensity;
    float idx = bandIndex(heat, uBands);
    float inFlame = step(1.0, idx) * step(0.001, heat);
    float3 flameCol = paletteRamp(saturate((idx - 1.0) / max(uBands - 1.0, 1.0)));

    // ---- soft halo: a smooth additive falloff that reaches exactly zero at its reach ----
    float gk = 3.0 / max(uParams.z, 0.1);
    float gEdge = 0.049787;                                 // = exp(-3): reach * gk is always 3
    float glow = saturate((exp(-max(bd - R, 0.0) * gk) - gEdge) / (1.0 - gEdge)) * uParams.w * uIntensity;
    float3 glowCol = lerp(uColorA, uColorB, saturate(glow * 0.8)) * glow;

    float4 outc = float4(glowCol, 0.0);
    outc = lerp(outc, float4(emberCol, 1.0), emberOn);
    outc = lerp(outc, float4(flameCol, 1.0), inFlame);
    return outc;
}

technique Technique1
{
    pass DarkFlameOrbPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}