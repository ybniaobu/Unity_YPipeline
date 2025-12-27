#ifndef YPIPELINE_DIFFUSE_GI_COMMON_INCLUDED
#define YPIPELINE_DIFFUSE_GI_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ----------------------------------------------------------------------------------------------------
// Diffuse Fallback -- APV
// ----------------------------------------------------------------------------------------------------

void EvaluateAdaptiveProbeVolume_NoNoise(float3 positionWS, float3 normalWS, float3 viewDir, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    APVSample apvSample = SampleAPV(positionWS, normalWS, renderingLayer, viewDir);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting);
}

float3 SampleProbeVolume_NoNoise(float3 positionWS, float3 normalWS, float3 viewDir)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume_NoNoise(positionWS, normalWS, viewDir, 0, irradiance);
    return irradiance;
}

// TODO: 上传自己的全局 SH 和 APV keyword
float3 DiffuseGIFallback(float3 positionWS, float3 normalWS, float3 viewDir)
{
    // #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    //     return SampleProbeVolume_NoNoise(positionWS, normalWS, viewDir);
    // #else
    //     return EvaluateAmbientProbe(normalWS);
    // #endif
    
    return SampleProbeVolume_NoNoise(positionWS, normalWS, viewDir);
}

#endif