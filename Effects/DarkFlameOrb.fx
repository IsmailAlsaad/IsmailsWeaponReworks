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

technique Technique1
{
    pass DarkFlameOrbPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}