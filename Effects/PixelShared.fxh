// Shared include for every pixel shader of the mod (Effects/*.fx). Used with Common/PixelShaderFx.
//   #include "PixelShared.fxh"
// It declares the STANDARD UNIFORMS below (Common/PixelShaderFx sets them on every draw), so a shader must never declare them
// itself. Unused ones cost nothing (the compiler drops them). Shader-specific values go into uParams / uParams2 or into the
// shader's own uniforms (set from PixelShaderParams.Configure). No dither helper on purpose: hard bands and hard thresholds only.
// Output convention (same as PixelLine): premultiplied color, alpha 1 = solid pixel, alpha 0 = additive glow.

#ifndef PIXEL_SHARED_FXH
#define PIXEL_SHARED_FXH

// ---- standard uniforms ----
float2 uOrigin;      // world position of the quad's top-left corner
float2 uSize;        // quad size in world px
float uGrid;         // world px per shader pixel (the world grid the pixels snap to)
float uTime;         // seconds of effect time (local visual state, never synced)
float uSeed;         // per-instance seed so two copies do not look identical (use seedShift())
float uProgress;     // 0..1 lifetime / phase of the effect
float uIntensity;    // overall strength multiplier
float uBands;        // number of color bands for posterizing
float uShape;        // quad mask: 0 = rectangle, 1 = ellipse (see quadMask)
float uUseRamp;      // 1 = paletteRamp reads the uRamp texture, 0 = it blends uColorA / B / C
float3 uColorA;      // palette: dark / outer
float3 uColorB;      // palette: mid
float3 uColorC;      // palette: bright / core
float4 uParams;      // free for the shader (meaning documented in the shader)
float4 uParams2;     // free for the shader
sampler2D uRamp : register(s1);   // optional hand-painted gradient, left to right (PixelShaderParams.Ramp)

// ---- world pixel grid ----
float2 pixelCell(float2 uv) { return floor((uOrigin + uv * uSize) / uGrid); }
float2 pixelCenter(float2 uv) { return (pixelCell(uv) + 0.5) * uGrid; }   // world position of this pixel's center

// 1 inside the quad's mask, 0 outside, decided per pixel cell so the edge is as hard as the pixels
float quadMask(float2 uv)
{
    float2 c = (pixelCenter(uv) - uOrigin) / uSize * 2.0 - 1.0;
    float rect = step(max(abs(c.x), abs(c.y)), 1.0);
    float ell = step(dot(c, c), 1.0);
    return lerp(rect, ell, uShape);
}

float2 seedShift() { return float2(uSeed * 17.131, uSeed * 5.713); }

float2 rotate2(float2 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// ---- noise ----
float hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 hash2(float2 p) { return float2(hash(p), hash(p + 19.19)); }

float vnoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
}

float fbm3(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 3; i++)
    {
        v += a * vnoise(p);
        p = p * 2.03 + float2(11.7, 3.1);
        a *= 0.5;
    }
    return v;
}

float fbm4(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * vnoise(p);
        p = p * 2.03 + float2(11.7, 3.1);
        a *= 0.5;
    }
    return v;
}

// octaves 1..6 (always costs 6 noise lookups; use fbm3 / fbm4 where the count is fixed)
float fbm(float2 p, float octaves)
{
    float v = 0.0;
    float a = 0.5;
    float norm = 0.0;
    for (int i = 0; i < 6; i++)
    {
        float on = step(i + 0.5, octaves);
        v += a * vnoise(p) * on;
        norm += a * on;
        p = p * 2.03 + float2(11.7, 3.1);
        a *= 0.5;
    }
    return v / max(norm, 0.001) * 0.9375;   // keeps the range close to fbm3 / fbm4 (about 0..1)
}

// shifts p by a noise vector of up to +-strength (domain warp)
float2 domainWarp(float2 p, float strength)
{
    return p + (float2(vnoise(p + float2(1.7, 9.2)), vnoise(p + float2(8.3, 2.8))) - 0.5) * 2.0 * strength;
}

// ---- posterize (hard bands) ----
float bandIndex(float v, float bands) { return floor(saturate(v) * bands); }          // 0 .. bands
float posterize(float v, float bands) { return bandIndex(v, bands) / max(bands, 1.0); }

// ---- color ramps, t in 0..1 ----
float3 ramp3(float t, float3 c0, float3 c1, float3 c2)
{
    float x = t * 2.0;
    return lerp(lerp(c0, c1, saturate(x)), c2, saturate(x - 1.0));
}

float3 ramp5(float t, float3 c0, float3 c1, float3 c2, float3 c3, float3 c4)
{
    float x = t * 4.0;
    float3 c = lerp(c0, c1, saturate(x));
    c = lerp(c, c2, saturate(x - 1.0));
    c = lerp(c, c3, saturate(x - 2.0));
    c = lerp(c, c4, saturate(x - 3.0));
    return c;
}

// the weapon's own palette: uColorA -> uColorB -> uColorC, or the hand-painted uRamp texture when uUseRamp = 1
float3 paletteRamp(float t)
{
    float3 tex = tex2Dlod(uRamp, float4(saturate(t), 0.5, 0.0, 0.0)).rgb;
    return lerp(ramp3(t, uColorA, uColorB, uColorC), tex, uUseRamp);
}

#endif