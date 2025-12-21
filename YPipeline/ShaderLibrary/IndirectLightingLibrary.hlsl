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

// TODO: 改为 Macro 形式
float3 CalculateDiffuseIndirectLighting(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        return CalculateProbeVolume(geometryParams, standardPBRParams, envBRDF_Diffuse);
    #elif defined(LIGHTMAP_ON)
        return CalculateLightMap(geometryParams.lightMapUV, standardPBRParams, envBRDF_Diffuse);
    #else
        return CalculateIBL_Diffuse(standardPBRParams, envBRDF_Diffuse);
    #endif
}

#endif