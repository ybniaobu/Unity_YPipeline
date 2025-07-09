#ifndef YPIPELINE_TAA_INCLUDED
#define YPIPELINE_TAA_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

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
// Neighborhood Clamp/Clip (History rejection)
// ----------------------------------------------------------------------------------------------------

float3 RGBClamp5(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
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

float3 RGBClamp9(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
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

float3 YCoCgClamp5(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
{
    current= RGBToYCoCg(current);
    float3 N = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, 1)).xyz);
    float3 E = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, 0)).xyz);
    float3 S = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, -1)).xyz);
    float3 W = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, 0)).xyz);

    float3 boxMin = min(current.xyz, min(N, min(E, min(S, W))));
    float3 boxMax = max(current.xyz, max(N, max(E, max(S, W))));

    history = RGBToYCoCg(history);
    history = clamp(history, boxMin, boxMax);

    return YCoCgToRGB(history);
}

float3 YCoCgClamp9(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
{
    current= RGBToYCoCg(current);
    float3 N = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, 1)).xyz);
    float3 E = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, 0)).xyz);
    float3 S = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, -1)).xyz);
    float3 W = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, 0)).xyz);
    float3 NW = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, 1)).xyz);
    float3 NE = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, 1)).xyz);
    float3 SW = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, -1)).xyz);
    float3 SE = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, -1)).xyz);

    float3 boxMin = min(current.xyz, min(N, min(E, min(S, min(W, min(NW, min(NE, min(SW, SE))))))));
    float3 boxMax = max(current.xyz, max(N, max(E, max(S, max(W, max(NW, max(NE, max(SW, SE))))))));

    history = RGBToYCoCg(history);
    history = clamp(history, boxMin, boxMax);

    return YCoCgToRGB(history);
}

float3 YCoCgClip9(TEXTURE2D(tex), int2 pixelCoord, float3 current, float3 history)
{
    current = RGBToYCoCg(current);
    float3 N = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, 1)).xyz);
    float3 E = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, 0)).xyz);
    float3 S = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(0, -1)).xyz);
    float3 W = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, 0)).xyz);
    float3 NW = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, 1)).xyz);
    float3 NE = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, 1)).xyz);
    float3 SW = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(-1, -1)).xyz);
    float3 SE = RGBToYCoCg(LoadOffset(tex, pixelCoord, int2(1, -1)).xyz);

    float3 boxMin = min(current.xyz, min(N, min(E, min(S, min(W, min(NW, min(NE, min(SW, SE))))))));
    float3 boxMax = max(current.xyz, max(N, max(E, max(S, max(W, max(NW, max(NE, max(SW, SE))))))));

    history = RGBToYCoCg(history);


    
    float3 center  = 0.5 * (boxMax + boxMin);
    float3 extents = max(0.5 * (boxMax - boxMin), HALF_MIN);
    float3 offset = history - center;

    float3 v_unit = offset.xyz / extents.xyz;
    float3 absUnit = abs(v_unit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);
    if (maxUnit > 1.0)
        return YCoCgToRGB(center + (offset / maxUnit));
    else
        return YCoCgToRGB(history);
}

// ----------------------------------------------------------------------------------------------------
// Edge Aliasing 
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

#endif