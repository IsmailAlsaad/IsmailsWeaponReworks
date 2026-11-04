// Dark flame orb of the Dark Lance: a pixelated, procedural ball of purple shadowflame with a smooth halo. One pixel shader, no loops (a loop
// of embers is what fxcompiler.exe rejects). The embers are NOT drawn here: they are world particles merged as metaballs on the CPU
// (Content/Weapons/DarkLance/DarkLanceEmbers.cs). Drawn by DarkLanceFx.DrawOrb through Common/PixelShaderFx over a square quad centered on the orb.
// Standard uniforms of PixelShared.fxh:
//   uParams.x = world px per shader unit (the ball's radius is 0.5 units), .y = pop-in scale (0..1.25, 1 = full size),
//   .z = halo reach in units beyond the ball, .w = halo strength
//   uTime = seconds, uSeed = per-orb seed, uIntensity = flicker strength, uBands = number of color bands
//   uColorA / uColorB / uColorC = outer / mid / core (the palette ramp)
// Output is premultiplied: alpha 1 = solid flame / ember pixel, alpha 0 = additive glow (the PixelLine convention). The halo is a smooth falloff.

#include "PixelShared.fxh"

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // shader space: the orb's center at the origin, +y = down on screen; the pixels snap to the world grid
    float2 q = (pixelCenter(coords) - (uOrigin + uSize * 0.5)) / max(uParams.x, 1.0);
    float appear = max(uParams.y, 0.05);
    float grow = min(appear, 1.0);
    float t = uTime;
    float R = 0.5;
    float bd = length(q / appear);                          // distance from the center (pop-in: the ball and its halo grow)



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