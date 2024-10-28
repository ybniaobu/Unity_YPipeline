#ifndef YPIPELINE_IBL_LIBRARY_INCLUDED
#define YPIPELINE_IBL_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
#include "Assets/ShaderLibrary/BRDFTermsLibrary.hlsl"
#include "Assets/ShaderLibrary/SamplingLibrary.hlsl"
#include "Assets/ShaderLibrary/ToneMappingLibrary.hlsl"

// --------------------------------------------------------------------------------
// 

// --------------------------------------------------------------------------------
// Spherical Harmonics(SH)
// TODO: 先使用 UnityInput 里传递进来的 unity_SHAr...unity_SHC 以后写自己管线时再修改
float3 SampleSH(float3 N)
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


// --------------------------------------------------------------------------------
// Prefilter Environment Map
float3 PrefilterEnvMap_GGX(TEXTURECUBE(envMap), SAMPLER(envMapSampler), uint sampleNumber, float resolutionPerFace, float roughness, float3 R)
{
    float3 N = R;
    float3 V = R;

    float3 prefilteredColor = 0;
    float totalWeight = 0;
    
    for( uint i = 0; i < sampleNumber; i++ )
    {
        float2 xi = Hammersley(i, sampleNumber);
        float4 HandPDF = ImportanceSampleGGX(xi, roughness);
        float3 H = TangentCoordToWorldCoord(HandPDF.xyz, N);
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
            
            prefilteredColor += envMap.SampleLevel(envMapSampler, L, mipLevel * 1.6).rgb * NoL; //1.5是一个magic number
            totalWeight += NoL;
        }
    }
    
    return prefilteredColor / totalWeight;
}

float3 PrefilterHDREnvMap_GGX(TEXTURECUBE(envMap), SAMPLER(envMapSampler), uint sampleNumber, float resolutionPerFace, float roughness, float3 R)
{
    float3 N = R;
    float3 V = R;

    float3 prefilteredColor = 0;
    float totalWeight = 0;
    
    for( uint i = 0; i < sampleNumber; i++ )
    {
        float2 xi = Hammersley(i, sampleNumber);
        float4 HandPDF = ImportanceSampleGGX(xi, roughness);
        float3 H = TangentCoordToWorldCoord(HandPDF.xyz, N);
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
            
            prefilteredColor += ACESFilm(envMap.SampleLevel(envMapSampler, L, mipLevel).rgb) * NoL; //1.5是一个magic number
            totalWeight += NoL;
        }
    }
    
    return ACESFilm_Inv(prefilteredColor / totalWeight);
}

// --------------------------------------------------------------------------------
// Environment 2D Lut (Preintegrate BRDF)
float PreintegrateDiffuse_RenormalizedBurley(float roughness, float NoV)
{
    float3 V = float3(sqrt(1.0 - NoV * NoV), NoV, 0.0);

    float fd = 0.0;
    const uint sampleNumber = 2048;

    for (uint i = 0; i < sampleNumber; i++)
    {
        float2 xi = Hammersley(i, sampleNumber);
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
        float2 xi = Hammersley(i, sampleNumber);
        float3 H = ImportanceSampleGGX(xi, roughness, N);
        float3 L = 2.0 * dot(V, H) * H - V;

        float NoL = saturate(L.y);
        float NoH = saturate(H.y);
        float VoH = saturate(dot(V, H));

        if (NoL > 0)
        {
            float V = V_SmithGGXCorrelated(NoV, NoL, roughness);
            float G = V * 4 * NoL * NoV;
            float G_Vis = G * VoH / (NoH * NoV);
            float Fc = pow(1.0 - VoH, 5);
            
            //r += (1.0 - Fc) * G_Vis;
            //g += Fc * G_Vis;
            r += Fc * G_Vis;
            g += G_Vis;
        }
    }

    return float2(r,g) / sampleNumber;
}

#endif