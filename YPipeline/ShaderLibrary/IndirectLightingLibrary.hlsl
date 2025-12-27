#ifndef YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED

#include "../ShaderLibrary/IBLLibrary.hlsl"

#if defined(LIGHTMAP_ON)
#include "../ShaderLibrary/UnityLightMappingLibrary.hlsl"
#endif

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
#include "../ShaderLibrary/UnityAPVLibrary.hlsl"
#endif

// ----------------------------------------------------------------------------------------------------
// Light Map Macros
// ----------------------------------------------------------------------------------------------------

#if defined(LIGHTMAP_ON)
    #define LIGHTMAP_UV(index)                      float2 lightMapUV : TEXCOORD##index;
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)           OUT.lightMapUV = IN.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define TRANSFER_GEOMETRY_PARAMS_LIGHTMAP_UV    geometryParams.lightMapUV = IN.lightMapUV;
#else
    #define LIGHTMAP_UV(index)
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)
    #define TRANSFER_GEOMETRY_PARAMS_LIGHTMAP_UV
#endif

// ----------------------------------------------------------------------------------------------------
// Diffuse Indirect Lighting
// ----------------------------------------------------------------------------------------------------

// TODO: 修改所以 Diffuse GI 的这个函数
float3 CalculateIrradiance(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceTexture, sampler_LinearClamp, geometryParams.screenUV, 0).rgb;
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return Diffuse;
}

// TODO: 改为 Macro 形式
float3 CalculateDiffuseIndirectLighting(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    #if defined(_SCREEN_SPACE_IRRADIANCE)
        return CalculateIrradiance(geometryParams, standardPBRParams, envBRDF_Diffuse);
    #elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        return CalculateProbeVolume(geometryParams, standardPBRParams, envBRDF_Diffuse);
    #elif defined(LIGHTMAP_ON)
        return CalculateLightMap(geometryParams.lightMapUV, standardPBRParams, envBRDF_Diffuse);
    #else
        return CalculateIBL_Diffuse(standardPBRParams, envBRDF_Diffuse);
    #endif
}

#endif