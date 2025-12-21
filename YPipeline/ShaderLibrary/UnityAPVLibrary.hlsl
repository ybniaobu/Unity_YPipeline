#ifndef YPIPELINE_UNITY_APV_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_APV_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

float3 SampleProbeVolume(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord)
{
    float4 probeOcclusion;
    float3 irradiance;
    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDir, pixelCoord, GetRenderingLayer(), irradiance, probeOcclusion);
    return irradiance;
}

float3 CalculateProbeVolume(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleProbeVolume(geometryParams.positionWS, standardPBRParams.N, standardPBRParams.V, geometryParams.pixelCoord);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return Diffuse;
}

#endif