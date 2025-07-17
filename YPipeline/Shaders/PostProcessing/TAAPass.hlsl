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
    // ------------------------- Get closest motion vector -------------------------

    float depth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, depth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    
    // ------------------------- Filter Resampled History -------------------------

    float3 history = SampleHistory(_TAAHistory, IN.uv - velocity);

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

    // ------------------------- Adaptive Blending Factor -------------------------

    

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToAABBCenter(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------

    float depth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, depth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;

    // ------------------------- Filter Resampled History -------------------------

    float3 history = SampleHistory(_TAAHistory, IN.uv - velocity);

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

    // ------------------------- Adaptive Blending Factor -------------------------

    

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToFiltered(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------

    float depth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, depth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;

    // ------------------------- Filter Resampled History -------------------------

    float3 history = SampleHistory(_TAAHistory, IN.uv - velocity);

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
    // if (all(velocity) == 0) clampedHistory = history;
    float velocitySqr = dot(velocity, velocity);
    float blendFactor = lerp(_TAAParams.x + 0.025, 0, saturate(velocitySqr));
    blendFactor = depth == 0 ? 0 : lerp(blendFactor, 0.975, sqrt(1.0 - depth));
    

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

#endif