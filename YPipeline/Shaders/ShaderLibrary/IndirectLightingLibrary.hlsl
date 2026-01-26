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

// 对 Irradiance 应用漫反射反射方程得到 Radiance，即 Diffuse Indirect Lighting
float3 ApplyDiffuseBRDF(float3 irradiance, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 indirectDiffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return indirectDiffuse;
}

// TODO: 改为 Macro 形式
float3 DiffuseIndirectLighting(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    #if defined(_SCREEN_SPACE_IRRADIANCE)
        float3 irradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceTexture, sampler_LinearClamp, geometryParams.screenUV, 0).rgb;
    #elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        float3 irradiance = SampleProbeVolume(geometryParams.positionWS, standardPBRParams.N, standardPBRParams.V, geometryParams.pixelCoord);
    #elif defined(LIGHTMAP_ON)
        float3 irradiance = SampleLightMap(geometryParams.lightMapUV);
    #else
        float3 irradiance = EvaluateAmbientProbe(standardPBRParams.N);
    #endif
    
    return ApplyDiffuseBRDF(irradiance, standardPBRParams, envBRDF_Diffuse);
}

#endif