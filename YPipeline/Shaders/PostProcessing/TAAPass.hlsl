#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraMotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor
float4 _Jitter; // xy: jitter

float4 TAAFrag_AABBClamp(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);
    MinMaxNeighbourhood(samples);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    history = NeighborhoodAABBClamp(samples, history);

    // ------------------------- Exponential Blending -------------------------
    
    //float3 color = LumaExponentialAccumulation(history, current, _TAAParams.x);
    float3 color = lerp(samples.M, history, _TAAParams.x);
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
    float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);
    MinMaxNeighbourhood(samples);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    history = NeighborhoodClipToAABBCenter(samples, history);

    // ------------------------- Exponential Blending -------------------------
    
    //float3 color = LumaExponentialAccumulation(history, current, _TAAParams.x);
    float3 color = lerp(samples.M, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_ClipToFiltered(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);
    MinMaxNeighbourhood(samples);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = (samples.M + samples.neighbours[0] + samples.neighbours[1] + samples.neighbours[2]
        + samples.neighbours[3] + samples.neighbours[4] + samples.neighbours[5] + samples.neighbours[6] + samples.neighbours[7]) / 9;
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    history = NeighborhoodClipToFiltered(samples, samples.filteredM, history);

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(history, samples.filteredM, _TAAParams.x);
    //float3 color = lerp(samples.filteredM, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

float4 TAAFrag_VarianceClip(Varyings IN) : SV_TARGET
{
    // ------------------------- Filter Resampled History -------------------------

    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;

    // float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    // float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    float3 history = SampleHistoryLinear(_TAAHistory, IN.uv - velocity);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);
    VarianceNeighbourhood(samples);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    history = NeighborhoodAABBClamp(samples, history);

    // ------------------------- Exponential Blending -------------------------
    
    //float3 color = LumaExponentialAccumulation(history, current, _TAAParams.x);
    float3 color = lerp(samples.M, history, _TAAParams.x);
    color = OutputColor(color);
    return float4(color, 1.0);
}

#endif