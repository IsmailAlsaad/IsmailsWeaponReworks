// Dark flame orb of the Dark Lance: a pixelated, procedural ball of purple shadowflame with tongues that rise and flicker. One pixel
// shader, no particles. Drawn by DarkLanceFx.DrawOrb through Common/PixelShaderFx over a square quad centered on the orb.
// Uses only the standard uniforms of PixelShared.fxh:
//   uParams.x = world px per shader unit (the orb's body radius is 0.5 units), uParams.y = pop-in scale (0..1.25, 1 = full size)
//   uTime = seconds, uSeed = per-orb seed, uIntensity = flicker strength, uBands = number of color bands
//   uColorA / uColorB / uColorC = outer / mid / core (the palette ramp)
// Output is premultiplied: alpha 1 = solid flame / ember pixel, alpha 0 = additive glow (the PixelLine convention).

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

technique Technique1
{
    pass DarkFlameOrbPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}