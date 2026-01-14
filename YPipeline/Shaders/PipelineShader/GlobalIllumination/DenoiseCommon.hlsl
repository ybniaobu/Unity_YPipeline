#ifndef YPIPELINE_DENOISE_COMMON_INCLUDED
#define YPIPELINE_DENOISE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#define YCOCG 1

// ----------------------------------------------------------------------------------------------------
// Temporal Denoise Functions
// ----------------------------------------------------------------------------------------------------

inline float3 RGB2YCoCg(float3 rgb)
{
    return float3(
             rgb.x/4.0 + rgb.y/2.0 + rgb.z/4.0,
             rgb.x/2.0 - rgb.z/2.0,
            -rgb.x/4.0 + rgb.y/2.0 - rgb.z/4.0
        );
}

inline float3 YCoCg2RGB(float3 YCoCg)
{
    return float3(
            YCoCg.x + YCoCg.y - YCoCg.z,
            YCoCg.x + YCoCg.z,
            YCoCg.x - YCoCg.y - YCoCg.z
        );
}

inline float4 LoadColorAndDepth(TEXTURE2D(tex), int2 pixelCoord)
{
    float4 sample = LOAD_TEXTURE2D_LOD(tex, pixelCoord, 0);
    #if YCOCG
    return float4(RGB2YCoCg(sample.rgb), sample.a);
    #else
    return sample;
    #endif
}

inline float4 SampleColorAndDepth(TEXTURE2D(tex), SAMPLER(samplerTex), float2 screenUV)
{
    float4 sample = SAMPLE_TEXTURE2D_LOD(tex, samplerTex, screenUV, 0);
    #if YCOCG
    return float4(RGB2YCoCg(sample.rgb), sample.a);;
    #else
    return sample;
    #endif
}

inline float3 OutputColor(float3 color)
{
    #if YCOCG
    return max(YCoCg2RGB(color), 0);
    #else
    return max(color, 0);
    #endif
}

float3 BilateralFilterMiddleColor(in float4 neighbours[9])
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    
    float3 middleColor = neighbours[0].rgb;
    float middleDepth = neighbours[0].a;
    float weightSum = 4.0;
    float3 filtered = weightSum * middleColor;

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float3 sampleColor = neighbours[i + 1].rgb;
        float sampleDepth = neighbours[i + 1].a;
        bool occlusionTest = abs(1 - sampleDepth / middleDepth) < 0.05;
        float weight = weights[i + 1] * occlusionTest;
        weightSum += weight;
        filtered += weight * sampleColor;
    }
    
    filtered *= rcp(weightSum);
    return filtered;
}

void VarianceMinMax(in float4 samples[9], float gamma, float3 prefiltered, out float3 minColor, out float3 maxColor)
{
    float3 m1 = 0;
    float3 m2 = 0;

    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float3 sampleColor = samples[i].rgb;
        m1 += sampleColor;
        m2 += sampleColor * sampleColor;
    }

    const int sampleCount = 9;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float3 sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float3 neighborMin = m1 - gamma * sigma;
    float3 neighborMax = m1 + gamma * sigma;

    neighborMin = min(neighborMin, prefiltered);
    neighborMax = max(neighborMax, prefiltered);

    minColor = neighborMin;
    maxColor = neighborMax;
}

float3 NeighborhoodClipToFiltered(float3 minColor, float3 maxColor, float3 prefiltered, float3 history)
{
    float3 boxMin = minColor;
    float3 boxMax = maxColor;

    float3 rayOrigin = history;
    float3 rayDir = prefiltered - history;
    rayDir = abs(rayDir) < HALF_MIN ? HALF_MIN : rayDir;
    float3 invDir = rcp(rayDir);
    
    float3 minIntersect = (boxMin - rayOrigin) * invDir;
    float3 maxIntersect = (boxMax - rayOrigin) * invDir;
    float3 enterIntersect = min(minIntersect, maxIntersect);
    float intersect = Max3(enterIntersect.x, enterIntersect.y, enterIntersect.z);
    float historyBlend = saturate(intersect);
    return lerp(history, prefiltered, historyBlend);
}


#endif