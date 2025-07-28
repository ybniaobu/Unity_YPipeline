#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_MotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor, y: variance clipping critical value

float4 TAAFrag_AABBClamp(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------

    float closestDepth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, closestDepth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;
    
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

    float blendFactor = closestDepth == 0 ? 0 : lerp(_TAAParams.x + 0.025, 0.975, sqrt(1.0 - closestDepth));
    
    float velocityLength = length(velocity);
    if (velocityLength < HALF_MIN) clampedHistory = lerp(clampedHistory, history, blendFactor);
    blendFactor = lerp(blendFactor, 0, saturate(velocityLength * velocityLength / 2));

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToAABBCenter(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------

    float closestDepth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, closestDepth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;

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

    float blendFactor = closestDepth == 0 ? 0 : lerp(_TAAParams.x + 0.025, 0.975, sqrt(1.0 - closestDepth));
    
    float velocityLength = length(velocity);
    if (velocityLength < HALF_MIN) clampedHistory = lerp(clampedHistory, history, blendFactor);
    blendFactor = lerp(blendFactor, 0, saturate(velocityLength * velocityLength / 2));

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToFiltered(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------

    float closestDepth;
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy, closestDepth);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;

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
    float velocityFactor = saturate(sqrt(length(velocity)));
    
    float blendFactor = _TAAParams.x;
    
    // float clampedHistoryLuma = GetLuma(clampedHistory);
    // float minLuma = GetLuma(samples.min);
    // float maxLuma = GetLuma(samples.max);
    // float lumaContrast = max(maxLuma - minLuma, 0) / clampedHistoryLuma;
    //
    // float alpha = lerp(samples.alpha, lumaContrast, 0.1);
    // if (alpha > 0.25)
    // {
    //     blendFactor = 0.96;
    // }
    
    // float velocityFactor = saturate(sqrt(length(velocity)));
    // if (velocityFactor == 0.0) clampedHistory = history;
    // blendFactor = closestDepth == 0 ? 0 : lerp(blendFactor, 0, velocityFactor);
    blendFactor = lerp(blendFactor + 0.025, 0, velocityFactor);
    

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    
    color = OutputColor(color);
    return float4(color, 1.0);
    // return float4(color, alpha);
}

#endif