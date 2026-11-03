// Pixelated procedural fireball. One pixel shader, no particles.
// Drawn by FlowerOfFireFx over a square quad (MagicPixel stretched) centered on the fireball's head.
// The pixel grid is anchored to WORLD coordinates (uGrid world px per shader pixel), so the pixels never swim while the fireball moves.
// Output is premultiplied: alpha 1 = solid flame/spark pixel, alpha 0 = additive glow (same convention as PixelLine).

float2 uOrigin;   // world position of the quad's top-left corner
float2 uSize;     // quad size in world px
float2 uHead;     // world position of the fireball's head
float uScale;     // world px per shader unit (head radius is about 0.26 units)
float uGrid;      // world px per shader pixel
float uTime;      // seconds of flame time
float uAngle;     // travel direction in radians (the tail trails behind it)
float uBands;     // number of color bands
float uDither;    // 0 = hard bands, 1 = Bayer dither
float uBurst;     // 0 = flying, 0..1 = detonation progress

float hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float vnoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
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

float b2(float x, float y)
{
    return fmod(2.0 * x + 3.0 * y, 4.0);
}

// 4x4 Bayer threshold for a pixel cell index
float bayer(float2 c)
{
    float2 q = c - 4.0 * floor(c / 4.0);
    float2 hi = floor(q / 2.0);
    float2 lo = q - 2.0 * hi;
    return (4.0 * b2(lo.x, lo.y) + b2(hi.x, hi.y) + 0.5) / 16.0;
}

float3 ramp(float t)
{
    float3 c0 = float3(0.30, 0.03, 0.06);
    float3 c1 = float3(0.72, 0.10, 0.05);
    float3 c2 = float3(0.97, 0.38, 0.05);
    float3 c3 = float3(1.00, 0.74, 0.14);
    float3 c4 = float3(1.00, 0.96, 0.65);
    float x = t * 4.0;
    float3 c = lerp(c0, c1, saturate(x));
    c = lerp(c, c2, saturate(x - 1.0));
    c = lerp(c, c3, saturate(x - 2.0));
    c = lerp(c, c4, saturate(x - 3.0));
    return c;
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

    // posterize into color bands (optional Bayer dither between bands)
    float bd = bayer(cell);
    float idx = floor(saturate(heat) * uBands + (bd - 0.5) * uDither * 0.9);
    float inFlame = step(1.0, idx) * step(0.001, heat);
    float3 flameCol = ramp(saturate((idx - 1.0) / max(uBands - 1.0, 1.0)));

    // dithered additive halo outside the flame
    float gd = max(d - R, 0.0);
    float glow = exp(-gd * 4.5) * 0.9 / (1.0 + tx * 2.5);
    glow *= 1.0 - burst * 0.6;
    float th = lerp(0.5, bd, uDither);
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