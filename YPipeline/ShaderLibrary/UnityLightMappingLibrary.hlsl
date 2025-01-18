#ifndef YPIPELINE_UNITY_LIGHT_MAPPING_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_LIGHT_MAPPING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

// ----------------------------------------------------------------------------------------------------
// Light Map

#if defined(LIGHTMAP_ON)
    #define LIGHTMAP_UV(index)                      float2 lightMapUV : TEXCOORD##index;
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)           OUT.lightMapUV = IN.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define LIGHTMAP_UV_FRAGMENT(IN)                IN.lightMapUV
#else
    #define LIGHTMAP_UV(index)
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)
    #define LIGHTMAP_UV_FRAGMENT(IN)                float2(0.0, 0.0)
#endif

float3 SampleLightMap(float2 lightMapUV)
{
    // #if defined(LIGHTMAP_ON)
    //     return SampleSingleLightmap(unity_Lightmap, LIGHTMAP_SAMPLER_NAME, lightMapUV, float4(1, 1, 0, 0), true);
    // #elif defined(DYNAMICLIGHTMAP_ON)
    //     return XXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // #else
    //     return float3(0, 0, 0);
    // #endif
    return SampleSingleLightmap(unity_Lightmap, samplerunity_Lightmap, lightMapUV, float4(1, 1, 0, 0), true);
}

float3 CalculateLightMap_Diffuse(float2 lightMapUV, StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleLightMap(lightMapUV);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return Diffuse;
}

// ----------------------------------------------------------------------------------------------------
// Shadowmask Map

float4 SampleShadowmask(float2 lightMapUV)
{
    #if defined(LIGHTMAP_ON)
        return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
    #else
        return unity_ProbesOcclusion;
    #endif
}

float MixBakedAndRealtimeShadows(float2 lightMapUV, float realtimeShadowAttenuation, float shadowFade)
{
    float bakedShadowAttenuation = SampleShadowmask(lightMapUV).r;
    float shadowAttenuation = lerp(bakedShadowAttenuation, realtimeShadowAttenuation, shadowFade);
    return shadowAttenuation;
}

#endif