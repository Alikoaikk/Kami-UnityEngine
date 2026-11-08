#ifndef KAMI_COMMON_INCLUDED
#define KAMI_COMMON_INCLUDED

// Shared helpers for Kami's paper shaders. Everything is procedural, so no extra textures are needed.

float KamiHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float KamiValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = KamiHash21(i);
    float b = KamiHash21(i + float2(1, 0));
    float c = KamiHash21(i + float2(0, 1));
    float d = KamiHash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Paper grain: soft blotches plus fine horizontal fibres. Roughly centred on 0, range about -0.8..0.8.
float KamiPaperGrain(float2 p)
{
    float blotch = KamiValueNoise(p * 0.35) * 0.6 + KamiValueNoise(p * 1.3) * 0.4;
    float fibre = KamiValueNoise(float2(p.x * 0.6, p.y * 9.0));
    return (blotch - 0.5) + (fibre - 0.5) * 0.6;
}

// World-space size of the whole sprite quad. The quad is affine, so the ratio of screen-space
// derivatives is exact for an unrotated sprite. Lets water/wind measure "units below the surface"
// however the sprite is scaled.
float2 KamiQuadWorldSize(float2 positionWS, float2 uv)
{
    float w = abs(ddx(positionWS.x)) / max(abs(ddx(uv.x)), 1e-6);
    float h = abs(ddy(positionWS.y)) / max(abs(ddy(uv.y)), 1e-6);
    return float2(w, h);
}

#endif
