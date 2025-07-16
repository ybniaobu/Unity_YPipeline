#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraMotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor, y: variance clipping critical value
float4 _Jitter; // xy: jitter

float4 TAAFrag_AABBClamp(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    // float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    float3 history = SampleHistoryBicubic(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);

    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodAABBClamp(samples, history);

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, _TAAParams.x);
    // float3 color = lerp(samples.filteredM, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToAABBCenter(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    // float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    float3 history = SampleHistoryBicubic(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);
    
    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodClipToAABBCenter(samples, history);

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, _TAAParams.x);
    // float3 color = lerp(samples.filteredM, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToFiltered(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    // float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    float3 history = SampleHistoryBicubic(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);

    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodClipToFiltered(samples, history);

    // ------------------------- Adaptive Blending Factor -------------------------

    // float velocityFactor = length(velocity) - HALF_MIN > 0 ? 0 : 0.95;
    // //
    // // //float velocityFactor = any(velocity) >= HALF_MIN;
    // float blendFactor = lerp(_TAAParams.x - 0.1, 1.0, velocityFactor);
    //
    // //TODO: 根据深度增加 blendFactor
    // //TODO：根据 history 和 current 的差值减少 blendFactor
    float depth = LoadOffset(_CameraDepthTexture, IN.positionHCS.xy, int2(0, 0)).x;
    float blendFactor = lerp(_TAAParams.x, 0.975, (1.0 - depth));
    

    // ------------------------- Exponential Blending -------------------------
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    // float3 color = lerp(samples.filteredM, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

#endif