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
