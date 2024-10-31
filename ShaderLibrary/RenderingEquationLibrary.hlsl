#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

#include "Assets/ShaderLibrary/PunctualLightsLibrary.hlsl"
#include "Assets/ShaderLibrary/BRDFModelLibrary.hlsl"
#include "Assets/ShaderLibrary/IBLLibrary.hlsl"

struct RenderingEquationContent
{
    float3 directMainLight;
    float3 directAdditionalLight;
    float3 indirectLight;
};

// --------------------------------------------------------------------------------
// IBL calculation
float3 calculateIBL(StandardPBRParams standardPBRParams,TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler,
    Texture2D envLut, SamplerState envLutSampler, out float3 energyCompensation)
{
    float3 envBRDF = SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(standardPBRParams.NoV, standardPBRParams.roughness)).rgb;
    
    float3 irradiance = SampleSH(standardPBRParams.N);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF.b;
    float3 Kd = 1.0 - standardPBRParams.metallic;

    float3 R = reflect(-standardPBRParams.V, standardPBRParams.N);
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, R, 8.0 * standardPBRParams.roughness).rgb;
    energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) / 2;

    float3 IBL = irradiance * envBRDFDiffuse * Kd + prefilteredColor * lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0) * energyCompensation;
    return IBL;
}

// --------------------------------------------------------------------------------
// Punctual light calculation
float3 calculatePunctualLight(LightParams lightParams)
{
    float3 irradiance = lightParams.color * lightParams.distanceAttenuation * lightParams.angleAttenuation;
    return irradiance;
}


#endif