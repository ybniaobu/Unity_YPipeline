#ifndef YPIPELINE_SSAO_INCLUDED
#define YPIPELINE_SSAO_INCLUDED

// ----------------------------------------------------------------------------------------------------
// SSAO Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float LoadDepth(int2 pixelCoord)
{
    #ifdef _HALF_RESOLUTION
        return LOAD_TEXTURE2D_LOD(_HalfDepthTexture, pixelCoord, 0).r;
    #else
        return LOAD_TEXTURE2D_LOD(_CameraDepthTexture, pixelCoord, 0).r;
    #endif
}

inline float3 LoadAndDecodeNormal(int2 pixelCoord)
{
    #ifdef _HALF_RESOLUTION
        float3 packedNormalWS = LOAD_TEXTURE2D_LOD(_ThinGBuffer, pixelCoord * 2, 0).rgb;
    #else
        float3 packedNormalWS = LOAD_TEXTURE2D_LOD(_ThinGBuffer, pixelCoord, 0).rgb;
    #endif
    
    return DecodeNormalFrom888(packedNormalWS);
}

// ----------------------------------------------------------------------------------------------------
// Generate Hemisphere Samples
// ----------------------------------------------------------------------------------------------------

// Left-handed Spherical and Cartesian Coordinate, Coordinate Convention Detail see SamplingLibrary.hlsl

inline float3 GenerateHemisphereSamples(float2 xi, float scale)
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = scale * scale; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}

inline float3 GenerateCosineWeightedHemisphereSamples(float2 xi, float scale)
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt(1 - xi.y);
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = scale * scale; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}

// ----------------------------------------------------------------------------------------------------
// Filter Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float2 LoadAOandDepth(int2 pixelCoord)
{
    return LOAD_TEXTURE2D_LOD(_InputTexture, pixelCoord, 0).rg;
}

// ----------------------------------------------------------------------------------------------------
// Spatial Filter - Bilateral Filter
// ----------------------------------------------------------------------------------------------------

inline float BilateralWeight(float radiusDelta, float depthDelta, float2 sigma)
{
    return exp2(-radiusDelta * radiusDelta * rcp(2.0 * sigma.x * sigma.x) - depthDelta * depthDelta * rcp(2.0 * sigma.y * sigma.y));
}

// ----------------------------------------------------------------------------------------------------
// Temporal Filter
// ----------------------------------------------------------------------------------------------------

inline void GetNeighbourhoodSamples(in uint tileIDs[9], inout float2 samples[9])
{
    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        samples[i] = _AOZ[tileIDs[i]];
    }
}

float FilterMiddleColor(in float2 samples[9])
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    
    float weightSum = 4.0;
    float filtered = weightSum * samples[0].r;
    float middleDepth = GetViewDepthFromDepthTexture(samples[0].g);

    for (int i = 0; i < 8; i++)
    {
        float sampleDepth = GetViewDepthFromDepthTexture(samples[i + 1].g);
        bool occlusionTest = abs(1 - sampleDepth / middleDepth) < 0.1;
        float weight = weights[i + 1] * occlusionTest;
        weightSum += weight;
        filtered += weight * samples[i + 1].r;
    }
    filtered *= rcp(weightSum);
    return filtered;
}

void VarianceMinMax(in float2 samples[9], float filtered, float gamma, out float2 minMax)
{
    float m1 = 0;
    float m2 = 0;

    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float sampleColor = samples[i].r;
        m1 += sampleColor;
        m2 += sampleColor * sampleColor;
    }

    const int sampleCount = 9;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float neighborMin = m1 - gamma * sigma;
    float neighborMax = m1 + gamma * sigma;

    neighborMin = min(neighborMin, filtered);
    neighborMax = max(neighborMax, filtered);

    minMax = float2(neighborMin, neighborMax);
}

float ClipToFiltered(float2 minMax, float filtered, float history)
{
    float boxMin = minMax.x;
    float boxMax = minMax.y;

    float rayOrigin = history;
    float rayDir = filtered - history;
    rayDir = abs(rayDir) < HALF_MIN ? HALF_MIN : rayDir;
    float invDir = rcp(rayDir);
    
    float minIntersect = (boxMin - rayOrigin) * invDir;
    float maxIntersect = (boxMax - rayOrigin) * invDir;
    float enterIntersect = min(minIntersect, maxIntersect);
    float historyBlend = saturate(enterIntersect);
    return lerp(history, filtered, historyBlend);
}

#endif