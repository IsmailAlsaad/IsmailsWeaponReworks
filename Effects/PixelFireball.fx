// Pixelated procedural fireball. One pixel shader, no particles.
// Drawn by FlowerOfFireFx over a square quad (MagicPixel stretched) centered on the fireball's head.
// The pixel grid is anchored to WORLD coordinates (uGrid world px per shader pixel), so the pixels never swim while the fireball moves.
// Output is premultiplied: alpha 1 = solid flame/spark pixel, alpha 0 = additive glow (same convention as PixelLine).

#include "PixelShared.fxh"   // standard uniforms (uOrigin, uSize, uGrid, uTime, uBands, ...) and the noise / ramp helpers

float2 uHead;     // world position of the fireball's head
float uScale;     // world px per shader unit (head radius is about 0.26 units)
float uAngle;     // travel direction in radians (the tail trails behind it)
float uBurst;     // 0 = flying, 0..1 = detonation progress

float3 fireRamp(float t)
{
    return ramp5(t, float3(0.30, 0.03, 0.06), float3(0.72, 0.10, 0.05), float3(0.97, 0.38, 0.05), float3(1.00, 0.74, 0.14), float3(1.00, 0.96, 0.65));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // snap to the world pixel grid
    float2 worldPos = uOrigin + coords * uSize;
    float2 cell = floor(worldPos / uGrid);
    float2 snapped = (cell + 0.5) * uGrid;

    // shader space: head at the origin, +x = travel direction
    float2 w = (snapped - uHead) / uScale;
    float2 dir = float2(cos(uAngle), sin(uAngle));
    float2 q = float2(dot(w, dir), dot(w, float2(-dir.y, dir.x)));

    float burst = uBurst;
    float tx = max(-q.x, 0.0) * (1.0 - burst);          // how far behind the head
    float2 sp = q * 3.0 + float2(uTime * 2.2, 0.0);       // noise slides backward = flames flow back
    float n1 = fbm4(sp);
    float n2 = fbm3(sp * 1.9 + float2(uTime * 1.5, 7.0) + n1 * 1.5);   // domain warp

    float2 wv = q;
    wv.y += (n1 - 0.5) * 0.4 * (0.2 + tx);                // the tail wiggles more the farther back it is
    float k = lerp(0.4, 1.0, burst);                      // 0.4 stretches the tail; the burst rounds it
    float d = length(float2(lerp(wv.x, wv.x * k, step(wv.x, 0.0)), wv.y));
    float R = 0.26 * (1.0 + burst * 1.6);

    float heat = (R - d) / R + (n2 - 0.5) * 0.9 * (0.35 + tx * 1.2);
    heat += burst * (1.0 - burst) * 1.2;                  // detonation flash
    heat -= burst * burst * 0.9;                          // then it burns out

    // posterize into hard color bands
    float idx = bandIndex(heat, uBands);
    float inFlame = step(1.0, idx) * step(0.001, heat);
    float3 flameCol = fireRamp(saturate((idx - 1.0) / max(uBands - 1.0, 1.0)));

    // additive halo outside the flame (two hard levels)
    float gd = max(d - R, 0.0);
    float glow = exp(-gd * 4.5) * 0.9 / (1.0 + tx * 2.5);
    glow *= 1.0 - burst * 0.6;
    float th = 0.5;
    float glowOn = step(th, glow);
    float3 glowCol = float3(0.32, 0.06, 0.03) * lerp(1.0, 1.6, step(th + 0.25, glow));

    // sparks drifting back along the trail (about 2x2 pixels each)
    float2 sc = q * (uScale / (uGrid * 2.0)) + float2(uTime * 14.0, 0.0);
    float h = hash(floor(sc) + floor(uTime * 3.0));
    float lane = 0.10 + tx * 0.30;
    float spark = step(0.985, h) * step(0.08, tx) * step(abs(q.y), lane) * step(tx, 0.9) * (1.0 - burst);
    float3 sparkCol = lerp(float3(1.0, 0.8, 0.25), float3(0.9, 0.25, 0.05), saturate(tx));

    float4 outc = float4(glowCol * glowOn, 0.0);
    outc = lerp(outc, float4(sparkCol, 1.0), spark);
    outc = lerp(outc, float4(flameCol, 1.0), inFlame);
    return outc;
}

technique Technique1
{
    pass PixelFireballPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}