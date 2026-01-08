#ifndef YPIPELINE_DIFFUSE_GI_COMMON_INCLUDED
#define YPIPELINE_DIFFUSE_GI_COMMON_INCLUDED

#include "../../ShaderLibrary/IBLLibrary.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl" // IBLLibrary 改写了 EvaluateAmbientProbe 函数
#define __AMBIENTPROBE_HLSL__
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ----------------------------------------------------------------------------------------------------
// Diffuse Fallback -- APV
// ----------------------------------------------------------------------------------------------------

void EvaluateAdaptiveProbeVolume_NoNoise(float3 positionWS, float3 normalWS, float3 bentNormal, float3 noise, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    positionWS = positionWS + noise.x * _APVSamplingNoise * bentNormal;
    APVSample apvSample = SampleAPV(positionWS, normalWS, renderingLayer, bentNormal);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting); // 这里用 bent normal 感觉反而不如 normalWS
}

float3 SampleProbeVolume_NoNoise(float3 positionWS, float3 normalWS, float3 bentNormal, float3 noise)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume_NoNoise(positionWS, normalWS, bentNormal, noise, 0, irradiance);
    return irradiance;
}

float3 DiffuseGIFallback(float3 positionWS, float3 normalWS, float3 bentNormal,  float3 noise)
{
    #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        return SampleProbeVolume_NoNoise(positionWS, normalWS, bentNormal, noise);
    #else
        return EvaluateAmbientProbe(normalWS);
    #endif
}

#endif