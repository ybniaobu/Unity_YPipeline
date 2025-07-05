#ifndef YPIPELINE_TAA_INCLUDED
#define YPIPELINE_TAA_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

float4 _CameraBufferSize;

// ----------------------------------------------------------------------------------------------------
// Utility Functions
// ----------------------------------------------------------------------------------------------------

float4 LoadOffset(TEXTURE2D(tex), int2 pixelCoord, int2 offset)
{
    return LOAD_TEXTURE2D_LOD(tex, pixelCoord + offset, 0);
}

float4 SampleLinearOffset(TEXTURE2D(tex), float2 uv, float2 offset)
{
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv + offset * _CameraBufferSize.xy, 0);
}

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

// ----------------------------------------------------------------------------------------------------
// Neighborhood Clamp/Clip or History rejection
// ----------------------------------------------------------------------------------------------------

float3 SimpleRGBBoxClamp_5(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
{
    float3 N = LoadOffset(tex, pixelCoord, int2(0, 1)).xyz;
    float3 E = LoadOffset(tex, pixelCoord, int2(1, 0)).xyz;
    float3 S = LoadOffset(tex, pixelCoord, int2(0, -1)).xyz;
    float3 W = LoadOffset(tex, pixelCoord, int2(-1, 0)).xyz;

    float3 boxMin = min(current.xyz, min(N, min(E, min(S, W))));
    float3 boxMax = max(current.xyz, max(N, max(E, max(S, W))));

    history = clamp(history, boxMin, boxMax);

    return history;
}

float3 SimpleRGBBoxClamp_9(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
{
    float3 N = LoadOffset(tex, pixelCoord, int2(0, 1)).xyz;
    float3 E = LoadOffset(tex, pixelCoord, int2(1, 0)).xyz;
    float3 S = LoadOffset(tex, pixelCoord, int2(0, -1)).xyz;
    float3 W = LoadOffset(tex, pixelCoord, int2(-1, 0)).xyz;
    float3 NW = LoadOffset(tex, pixelCoord, int2(-1, 1)).xyz;
    float3 NE = LoadOffset(tex, pixelCoord, int2(1, 1)).xyz;
    float3 SW = LoadOffset(tex, pixelCoord, int2(-1, -1)).xyz;
    float3 SE = LoadOffset(tex, pixelCoord, int2(1, -1)).xyz;

    float3 boxMin = min(current.xyz, min(N, min(E, min(S, min(W, min(NW, min(NE, min(SW, SE))))))));
    float3 boxMax = max(current.xyz, max(N, max(E, max(S, max(W, max(NW, max(NE, max(SW, SE))))))));

    history = clamp(history, boxMin, boxMax);

    return history;
}

// float3 YCoCgAABBClip()
// {
//     
// }

#endif