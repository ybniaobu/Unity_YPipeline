﻿#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

#include "../ShaderLibrary/BRDFModelLibrary.hlsl"
#include "../ShaderLibrary/IBLLibrary.hlsl"
#include "../ShaderLibrary/PunctualLightsLibrary.hlsl"

struct RenderingEquationContent
{
    float3 directPunctualLights;
    float3 directAdditionalLight;
    float3 indirectLight;
};

// --------------------------------------------------------------------------------
// Ambient Occlusion
// TODO: 暂时先放在这里
float ComputeSpecularAO(float NoV, float ao, float roughness)
{
    return saturate(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao);
}

// --------------------------------------------------------------------------------
// IBL calculation
float3 CalculateIBL(StandardPBRParams standardPBRParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler,
    Texture2D envLut, SamplerState envLutSampler, out float3 energyCompensation)
{
    float3 envBRDF = SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(standardPBRParams.NoV, standardPBRParams.roughness)).rgb;
    
    float3 irradiance = SampleSH(standardPBRParams.N);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF.b;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 IBLDiffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, 8.0 * standardPBRParams.roughness).rgb;
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0);
    float3 envBRDFSpecular = envBRDF.xxx * standardPBRParams.F0 + (float3(standardPBRParams.F90, standardPBRParams.F90, standardPBRParams.F90) - standardPBRParams.F0) * envBRDF.yyy;
    energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) / 2;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(standardPBRParams.NoV, standardPBRParams.ao, standardPBRParams.roughness);
    
    // Horizon specular occlusion
    float horizon = saturate(1.0 + dot(standardPBRParams.R, standardPBRParams.N));
    IBLSpecular *= horizon * horizon;

    float3 IBL = IBLDiffuse + IBLSpecular;
    return IBL;
}

// --------------------------------------------------------------------------------
// Punctual light calculation
float3 CalculateLightIrradiance(LightParams lightParams)
{
    float3 irradiance = lightParams.color * lightParams.shadowAttenuation * lightParams.distanceAttenuation * lightParams.angleAttenuation;
    return irradiance;
}


#endif