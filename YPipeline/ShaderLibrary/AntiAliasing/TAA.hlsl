#ifndef YPIPELINE_TAA_INCLUDED
#define YPIPELINE_TAA_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// ----------------------------------------------------------------------------------------------------
// TAA Utility Functions
// ----------------------------------------------------------------------------------------------------

float4 LoadOffset(TEXTURE2D(tex), float2 pixelCoord, int2 offset)
{
    return LOAD_TEXTURE2D_LOD(tex, pixelCoord + offset, 0);
}

float4 SampleLinearOffset(TEXTURE2D(tex), float2 uv, int2 offset)
{
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv + offset * _CameraBufferSize.xy, 0);
}

float3 RGB2YCoCg(float3 rgb)
{
    return float3(
             rgb.x/4.0 + rgb.y/2.0 + rgb.z/4.0,
             rgb.x/2.0 - rgb.z/2.0,
            -rgb.x/4.0 + rgb.y/2.0 - rgb.z/4.0
        );
}

float3 YCoCg2RGB(float3 YCoCg)
{
    return float3(
            YCoCg.x + YCoCg.y - YCoCg.z,
            YCoCg.x + YCoCg.z,
            YCoCg.x - YCoCg.y - YCoCg.z
        );
}

float3 LoadColor(TEXTURE2D(tex), float2 pixelCoord, int2 offset)
{
    #if _TAA_YCOCG
        return RGB2YCoCg(LoadOffset(tex, pixelCoord, offset).xyz);
    #else
        return LoadOffset(tex, pixelCoord, offset).xyz;
    #endif
}

float3 OutputColor(float3 color)
{
    #if _TAA_YCOCG
        return max(YCoCg2RGB(color), 0);
    #else
        return max(color, 0);
    #endif
}

// ----------------------------------------------------------------------------------------------------
// History Filter
// ----------------------------------------------------------------------------------------------------

float3 SampleHistoryLinear(TEXTURE2D(tex), float2 uv)
{
    #if _TAA_YCOCG
    return RGB2YCoCg(SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv, 0).xyz);
    #else
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv, 0).xyz;
    #endif
}

// ----------------------------------------------------------------------------------------------------
// Neighbourhood Samples Related
// ----------------------------------------------------------------------------------------------------

struct NeighbourhoodSamples
{
    #if _TAA_SAMPLE_3X3
    float3 neighbours[8];
    #else
    float3 neighbours[4];
    #endif
    float3 M;
    float3 filteredM;
    float3 min;
    float3 max;
};

void GetNeighbourhoodSamples(inout NeighbourhoodSamples samples, TEXTURE2D(tex), float2 pixelCoord)
{
    samples.M = LoadColor(tex, pixelCoord, int2(0, 0)).xyz;
    samples.neighbours[0] = LoadColor(tex, pixelCoord, int2(0, 1)).xyz;
    samples.neighbours[1] = LoadColor(tex, pixelCoord, int2(1, 0)).xyz;
    samples.neighbours[2] = LoadColor(tex, pixelCoord, int2(0, -1)).xyz;
    samples.neighbours[3] = LoadColor(tex, pixelCoord, int2(-1, 0)).xyz;

    #if _TAA_SAMPLE_3X3
    samples.neighbours[4] = LoadColor(tex, pixelCoord, int2(-1, 1)).xyz;
    samples.neighbours[5] = LoadColor(tex, pixelCoord, int2(1, 1)).xyz;
    samples.neighbours[6] = LoadColor(tex, pixelCoord, int2(-1, -1)).xyz;
    samples.neighbours[7] = LoadColor(tex, pixelCoord, int2(1, -1)).xyz;
    #endif
}

void MinMaxNeighbourhood(inout NeighbourhoodSamples samples)
{
    samples.min = min(samples.M, min(samples.neighbours[0], min(samples.neighbours[1], min(samples.neighbours[2], samples.neighbours[3]))));
    samples.max = max(samples.M, max(samples.neighbours[0], max(samples.neighbours[1], max(samples.neighbours[2], samples.neighbours[3]))));

    #if _TAA_SAMPLE_3X3
    samples.min = min(samples.min, min(samples.neighbours[4], min(samples.neighbours[5], min(samples.neighbours[6], samples.neighbours[7]))));
    samples.max = max(samples.max, max(samples.neighbours[4], max(samples.neighbours[5], max(samples.neighbours[6], samples.neighbours[7]))));
    #endif
}

// void VarianceNeighbourhood(inout NeighbourhoodSamples samples)
// {
//     samples.min = min(samples.M, min(samples.neighbours[0], min(samples.neighbours[1], min(samples.neighbours[2], samples.neighbours[3]))));
//     samples.max = max(samples.M, max(samples.neighbours[0], max(samples.neighbours[1], max(samples.neighbours[2], samples.neighbours[3]))));
//
//     #if _TAA_SAMPLE_3X3
//     samples.min = min(samples.min, min(samples.neighbours[4], min(samples.neighbours[5], min(samples.neighbours[6], samples.neighbours[7]))));
//     samples.max = max(samples.max, max(samples.neighbours[4], max(samples.neighbours[5], max(samples.neighbours[6], samples.neighbours[7]))));
//     #endif
// }

// ----------------------------------------------------------------------------------------------------
// Neighborhood AABB Clamp/AABB Clip/Variance Clip (Color Rejection)
// ----------------------------------------------------------------------------------------------------

float3 NeighborhoodAABBClamp(in NeighbourhoodSamples samples, float3 history)
{
    history = clamp(history, samples.min, samples.max);
    return history;
}

// From "Temporal Reprojection Antialiasing in INSIDE (Playdead Studio)" at GDC 2016
// https://github.com/playdeadgames/temporal/blob/4795aa0007d464371abe60b7b28a1cf893a4e349/Assets/Shaders/TemporalReprojection.shader
// https://www.gdcvault.com/play/1022970/Temporal-Reprojection-Anti-Aliasing-in
float3 NeighborhoodClipToAABBCenter(in NeighbourhoodSamples samples, float3 history)
{
    // note: only clips towards aabb center (but fast!)
    float3 center  = 0.5 * (samples.max + samples.min);
    float3 extents = 0.5 * (samples.max - samples.min) + HALF_MIN;
    
    float3 vOffset = history - center;
    float3 vUnit = vOffset / extents;
    float3 absUnit = abs(vUnit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);

    if (maxUnit > 1.0)
        return center + (vOffset / maxUnit);
    else
        return history;
}

// UE4 Version
// Here the ray referenced goes from history to (filtered) center color
float3 NeighborhoodClipToFiltered(in NeighbourhoodSamples samples, float3 filtered, float3 history)
{
    float3 center  = 0.5 * (samples.max + samples.min);
    float3 extents = 0.5 * (samples.max - samples.min);

    float3 rayDir = filtered - history;
    rayDir = abs(rayDir) < HALF_MIN ? HALF_MIN : rayDir;
    float3 rayPos = history - center;
    float3 invDir = rcp(rayDir);
    float3 t0 = (extents - rayPos)  * invDir;
    float3 t1 = -(extents + rayPos) * invDir;
    float intersection = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
    float historyBlend = saturate(intersection);
    return lerp(history, filtered, historyBlend);
}


// ----------------------------------------------------------------------------------------------------
// Velocity Rejection
// ----------------------------------------------------------------------------------------------------

float2 GetClosestDepthPixelCoord(TEXTURE2D(depthTex), int2 pixelCoord)
{
    float M = LoadOffset(depthTex, pixelCoord, int2(0, 0)).x;
    float N = LoadOffset(depthTex, pixelCoord, int2(0, 1)).x;
    float E = LoadOffset(depthTex, pixelCoord, int2(1, 0)).x;
    float S = LoadOffset(depthTex, pixelCoord, int2(0, -1)).x;
    float W = LoadOffset(depthTex, pixelCoord, int2(-1, 0)).x;
    float NW = LoadOffset(depthTex, pixelCoord, int2(-1, 1)).x;
    float NE = LoadOffset(depthTex, pixelCoord, int2(1, 1)).x;
    float SW = LoadOffset(depthTex, pixelCoord, int2(-1, -1)).x;
    float SE = LoadOffset(depthTex, pixelCoord, int2(1, -1)).x;

    float3 offset = float3(0, 0, M);
    offset = lerp(offset, float3(0, 1, N), COMPARE_DEVICE_DEPTH_CLOSER(N, offset.z));
    offset = lerp(offset, float3(1, 0, E), COMPARE_DEVICE_DEPTH_CLOSER(E, offset.z));
    offset = lerp(offset, float3(0, -1, S), COMPARE_DEVICE_DEPTH_CLOSER(S, offset.z));
    offset = lerp(offset, float3(-1, 0, W), COMPARE_DEVICE_DEPTH_CLOSER(W, offset.z));
    offset = lerp(offset, float3(-1, 1, NW), COMPARE_DEVICE_DEPTH_CLOSER(NW, offset.z));
    offset = lerp(offset, float3(1, 1, NE), COMPARE_DEVICE_DEPTH_CLOSER(NE, offset.z));
    offset = lerp(offset, float3(-1, -1, SW), COMPARE_DEVICE_DEPTH_CLOSER(SW, offset.z));
    offset = lerp(offset, float3(1, -1, SE), COMPARE_DEVICE_DEPTH_CLOSER(SE, offset.z));

    return pixelCoord + offset.xy;
}

// ----------------------------------------------------------------------------------------------------
// Prefilter
// ----------------------------------------------------------------------------------------------------


// ----------------------------------------------------------------------------------------------------
// Exponential Moving Average
// ----------------------------------------------------------------------------------------------------

float3 SimpleExponentialAccumulation(float3 history, float3 current, float blendFactor)
{
    return lerp(current, history, blendFactor);
}

float3 LumaExponentialAccumulation(float3 history, float3 current, float blendFactor)
{
    float historyLuma = Luminance(history);
    float currentLuma = Luminance(current);
    float historyLumaWeight = rcp(historyLuma + 1.0);
    float currentLumaWeight = rcp(currentLuma + 1.0);
    float weightSum = lerp(currentLumaWeight, historyLumaWeight, blendFactor);
    float3 blendColor = lerp(current * currentLumaWeight, history * historyLumaWeight, blendFactor);
    return blendColor / weightSum;
}

#endif