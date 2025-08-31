#ifndef YPIPELINE_SSAO_INCLUDED
#define YPIPELINE_SSAO_INCLUDED

// ----------------------------------------------------------------------------------------------------
// SSAO Utility Functions
// ----------------------------------------------------------------------------------------------------

// inline float SampleLinearDepth(float2 screenUV)
// {
//     return SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_LinearClamp, screenUV, 0).r;
// }

inline float LoadDepth(int2 pixelCoord)
{
    #ifdef _HALF_RESOLUTION
        float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, pixelCoord * 2, 0).r;
    #else
        float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, pixelCoord, 0).r;
    #endif
    
    return depth;
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
    // const float depthThreshold = 0.5;
    // return exp(-radiusDelta * radiusDelta * rcp(2.0 * sigma.x * sigma.x)) * (depthDelta < sigma.y);
    return exp(-radiusDelta * radiusDelta * rcp(2.0 * sigma.x * sigma.x) - depthDelta * depthDelta * rcp(0.5 * sigma.y * sigma.y));
}
//
// inline float2 BilateralBlur(float2 pixelCoord, float2 pixelOffset)
// {
//     int radius = int(GetSpatialBlurKernelRadius());
//     float2 center = LoadAOandDepth(pixelCoord,0);
//     float weightSum = BilateralWeight(0, 0);
//     float aoFactor = center.r * weightSum;
//     
//     for (int i = -radius; i <= radius && i != 0; i++)
//     {
//         float2 sample = LoadAOandDepth(pixelCoord, i * pixelOffset);
//         float weight = BilateralWeight(i, sample.g - center.g);
//         aoFactor += sample.r * weight;
//         weightSum += weight;
//     }
//
//     aoFactor /= weightSum;
//     return float2(aoFactor, center.g);
// }

// ----------------------------------------------------------------------------------------------------
// Temporal Filter
// ----------------------------------------------------------------------------------------------------

float GaussianFilterMiddleColor(in float2 samples[9])
{
    // const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    // sigma = 0.8
    // const float weights[9] = { 1.0, 0.4578, 0.4578, 0.4578, 0.4578, 0.2097, 0.2097, 0.2097, 0.2097 };
    
    // sigma = 0.6
    const float weights[9] = { 1.0, 0.2493, 0.2493, 0.2493, 0.2493, 0.0625, 0.0625, 0.0625, 0.0625 };
    
    float weightSum = 4.0;
    float filtered = weightSum * samples[0].r;

    for (int i = 0; i < 8; i++)
    {
        float weight = weights[i + 1];
        weightSum += weight;
        filtered += weight * samples[i + 1].r;
    }
    filtered *= rcp(weightSum);
    return filtered;
}

void VarianceNeighbourhood(in float2 samples[9], float filtered, float gamma, out float2 minMax)
{
    float m1 = 0;
    float m2 = 0;
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

float NeighborhoodClipToFiltered(float2 minMax, float filtered, float history)
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

inline float2 TemporalBlur(float2 pixelCoord, float2 screenUV)
{
    float2 velocity = SAMPLE_TEXTURE2D_LOD(_MotionVectorTexture, sampler_PointClamp, screenUV, 0).rg;
    float2 historyUV = screenUV - velocity;
    float2 history = SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionHistory, sampler_LinearClamp, historyUV, 0);

    float2 neighbours[9];
    neighbours[0] = LoadAOandDepth(pixelCoord + 0);
    neighbours[1] = LoadAOandDepth(pixelCoord + int2(0, 1));
    neighbours[2] = LoadAOandDepth(pixelCoord + int2(1, 0));
    neighbours[3] = LoadAOandDepth(pixelCoord + int2(0, -1));
    neighbours[4] = LoadAOandDepth(pixelCoord + int2(-1, 0));
    neighbours[5] = LoadAOandDepth(pixelCoord + int2(-1, 1));
    neighbours[6] = LoadAOandDepth(pixelCoord + int2(1, 1));
    neighbours[7] = LoadAOandDepth(pixelCoord + int2(-1, -1));
    neighbours[8] = LoadAOandDepth(pixelCoord + int2(1, -1));

    // float prefiltered = neighbours[0].r;
    float prefiltered = GaussianFilterMiddleColor(neighbours);

    float2 minMax;
    VarianceNeighbourhood(neighbours, prefiltered, 0.5, minMax);
    
    history.r = NeighborhoodClipToFiltered(minMax, prefiltered, history.r);
    
    return float2(lerp(neighbours[0].r, history.r, 0.9), neighbours[0].g);
}


#endif