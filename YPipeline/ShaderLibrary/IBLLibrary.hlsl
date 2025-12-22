#ifndef YPIPELINE_IBL_LIBRARY_INCLUDED
#define YPIPELINE_IBL_LIBRARY_INCLUDED

#include "../ShaderLibrary/BRDFModelLibrary.hlsl"
#include "../ShaderLibrary/RandomLibrary.hlsl"
#include "../ShaderLibrary/SamplingLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Spherical Harmonics(SH)
// ----------------------------------------------------------------------------------------------------

float3 SampleSphericalHarmonics(float3 N)
{
    float3 L0L1;
    float4 vA = float4(N, 1.0);
    L0L1.r = dot(unity_SHAr, vA);
    L0L1.g = dot(unity_SHAg, vA);
    L0L1.b = dot(unity_SHAb, vA);

    float3 L2;
    float4 vB = N.xyzz * N.yzzx;
    L2.r = dot(unity_SHBr, vB);
    L2.g = dot(unity_SHBg, vB);
    L2.b = dot(unity_SHBb, vB);
    
    float vC = N.x * N.x - N.y * N.y;
    L2 += unity_SHC.rgb * vC;

    return L0L1 + L2;
}

// ----------------------------------------------------------------------------------------------------
// IBL calculation
// ----------------------------------------------------------------------------------------------------

float3 SampleEnvLut(Texture2D envLut, SamplerState envLutSampler, float NoV, float roughness)
{
    return SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(NoV, roughness)).rgb;
}

// float3 SampleHDREnvironment(TextureCube envMap, SamplerState envMapSampler, float3 dir, float mipmap)
// {
//     float4 env = SAMPLE_TEXTURECUBE_LOD(envMap, envMapSampler, dir, mipmap);
//     return DecodeHDREnvironment(env, unity_SpecCube0_HDR);
// }

float3 CalculateIBL_Diffuse(in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleSphericalHarmonics(standardPBRParams.N);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 IBLDiffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return IBLDiffuse;
}

float RoughnessToMipmapLevel(float roughness, float maxMipLevel)
{
    roughness = roughness * (1.7 - 0.7 * roughness);
    return roughness * maxMipLevel;
}

float3 CalculateIBL_Specular(in StandardPBRParams standardPBRParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, 6.0 * standardPBRParams.roughness).rgb;
    //float3 prefilteredColor = SampleHDREnvironment(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, 6.0 * standardPBRParams.roughness);
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * standardPBRParams.F0 + (float3(standardPBRParams.F90, standardPBRParams.F90, standardPBRParams.F90) - standardPBRParams.F0) * envBRDF_Specular.yyy;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(standardPBRParams.NoV, standardPBRParams.ao, standardPBRParams.roughness);
    
    IBLSpecular *= ComputeHorizonSpecularOcclusion(standardPBRParams.R, standardPBRParams.N);
    return IBLSpecular;
}

float3 CalculateIBL_Specular_RemappedMipmap(in StandardPBRParams standardPBRParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler, float2 envBRDF_Specular, float3 energyCompensation)
{
    float mipmap = RoughnessToMipmapLevel(standardPBRParams.roughness, 6.0);
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, mipmap).rgb;
    //float3 prefilteredColor = SampleHDREnvironment(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, mipmap);
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * standardPBRParams.F0 + (float3(standardPBRParams.F90, standardPBRParams.F90, standardPBRParams.F90) - standardPBRParams.F0) * envBRDF_Specular.yyy;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(standardPBRParams.NoV, standardPBRParams.ao, standardPBRParams.roughness);
    
    IBLSpecular *= ComputeHorizonSpecularOcclusion(standardPBRParams.R, standardPBRParams.N);
    return IBLSpecular;
}

// float3 CalculateIBL(StandardPBRParams standardPBRParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler,
//     Texture2D envLut, SamplerState envLutSampler, out float3 energyCompensation)
// {
//     float3 envBRDF = SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(standardPBRParams.NoV, standardPBRParams.roughness)).rgb;
//     
//     float3 irradiance = SampleSH(standardPBRParams.N);
//     float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF.b;
//     float Kd = 1.0 - standardPBRParams.metallic;
//     float3 IBLDiffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
//     
//     float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, standardPBRParams.R, 8.0 * standardPBRParams.roughness).rgb;
//     //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0);
//     float3 envBRDFSpecular = envBRDF.xxx * standardPBRParams.F0 + (float3(standardPBRParams.F90, standardPBRParams.F90, standardPBRParams.F90) - standardPBRParams.F0) * envBRDF.yyy;
//     energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) / 2;
//     float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(standardPBRParams.NoV, standardPBRParams.ao, standardPBRParams.roughness);
//     
//     // Horizon specular occlusion
//     float horizon = saturate(1.0 + dot(standardPBRParams.R, standardPBRParams.N));
//     IBLSpecular *= horizon * horizon;
//
//     float3 IBL = IBLDiffuse + IBLSpecular;
//     return IBL;
// }

// ----------------------------------------------------------------------------------------------------
// Prefilter Environment Map
// ----------------------------------------------------------------------------------------------------

float3 PrefilterEnvMap_GGX(TEXTURECUBE(envMap), SAMPLER(envMapSampler), uint sampleNumber, float resolutionPerFace, float roughness, float3 R)
{
    float3 N = R;
    float3 V = R;

    float3 prefilteredColor = 0;
    float totalWeight = 0;
    
    for( uint i = 0; i < sampleNumber; i++ )
    {
        float2 xi = Hammersley_Bits(i, sampleNumber);
        float4 HandPDF = ImportanceSampleGGX(xi, roughness);
        float3 H = TangentCoordToNormalizedWorldCoord(HandPDF.xyz, N);
        float PDF = HandPDF.w;
        float3 L = 2 * dot( V, H ) * H - V;

        float NoL = saturate(dot(N, L));
        
        if (NoL > 0)
        {
            // Reduce artifacts due to high frequency details by sampling a mip level of the environment map based on the integral's PDF and the roughness
            // resolutionPerFace is the resolution of source cubemap (per face)
            float saTexel  = FOUR_PI / (6.0 * resolutionPerFace * resolutionPerFace);
            float saSample = 1.0 / (float(sampleNumber) * PDF + 0.0001);
            float mipLevel = roughness == 0.0 ? 0.0 : 0.5 * log2(saSample / saTexel);
            
            prefilteredColor += envMap.SampleLevel(envMapSampler, L, mipLevel * 1.5).rgb * NoL; //1.5 is a magic/empirical number
            totalWeight += NoL;
        }
    }
    
    return prefilteredColor / totalWeight;
}

// ----------------------------------------------------------------------------------------------------
// Environment 2D Lut (Preintegrate BRDF)
// ----------------------------------------------------------------------------------------------------

float PreintegrateDiffuse_RenormalizedBurley(float roughness, float NoV)
{
    float3 V = float3(sqrt(1.0 - NoV * NoV), NoV, 0.0);

    float fd = 0.0;
    const uint sampleNumber = 2048;

    for (uint i = 0; i < sampleNumber; i++)
    {
        float2 xi = Hammersley_Bits(i, sampleNumber);
        float3 L = InverseSampleHemisphere(xi).xyz;
        float3 H = normalize(L + V);

        float NoL = saturate(L.y);
        float LoH = saturate(dot(L, H));

        if (NoL > 0)
        {
            float diffuse = Fd_RenormalizedBurley_Disney_NoPI(NoV, NoL, LoH, roughness);
            fd += diffuse;
        }
    }
    
    return fd / sampleNumber;
}

float PreintegrateDiffuse_RenormalizedBurley_CosineVersion(float roughness, float NoV)
{
    float3 V = float3(sqrt(1.0 - NoV * NoV), NoV, 0.0);

    float fd = 0.0;
    const uint sampleNumber = 2048;

    for (uint i = 0; i < sampleNumber; i++)
    {
        float2 xi = Hammersley_Bits(i, sampleNumber);
        float3 L = CosineSampleHemisphere(xi).xyz;
        float3 H = normalize(L + V);

        float NoL = saturate(L.y);
        float LoH = saturate(dot(L, H));

        if (NoL > 0)
        {
            float diffuse = Fd_RenormalizedBurley_Disney_NoPI(NoV, NoL, LoH, roughness);
            fd += diffuse;
        }
    }
    
    return fd / sampleNumber;
}

float2 PreintegrateSpecular_SmithGGXCorrelated(float roughness, float NoV)
{
    float3 V = float3(sqrt(1.0 - NoV * NoV), NoV, 0.0);
    float3 N = float3(0.0, 1.0, 0.0);

    float r = 0.0;
    float g = 0.0;
    const uint sampleNumber = 2048;

    for (uint i = 0; i < sampleNumber; i++)
    {
        float2 xi = Hammersley_Bits(i, sampleNumber);
        float3 H = ImportanceSampleGGX(xi, roughness, N);
        float3 L = 2.0 * dot(V, H) * H - V;

        float NoL = saturate(L.y);
        float NoH = saturate(H.y);
        float VoH = saturate(dot(V, H));

        if (NoL > 0)
        {
            float Vis = V_SmithGGXCorrelated(NoV, NoL, roughness);
            float G = Vis * 4 * NoL * NoV;
            float G_Vis = G * VoH / (NoH * NoV);
            float Fc = pow(1.0 - VoH, 5);
            
            //r += (1.0 - Fc) * G_Vis;
            //g += Fc * G_Vis;
            r += G_Vis;
            g += Fc * G_Vis;
        }
    }

    return float2(r, g) / sampleNumber;
}

#endif