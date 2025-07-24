#ifndef YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED

#include "../ShaderLibrary/UnityLightMappingLibrary.hlsl"
#include "../ShaderLibrary/IBLLibrary.hlsl"

float3 IndirectLighting_Diffuse(float2 lightMapUV, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    #if defined(LIGHTMAP_ON)
        return CalculateLightMap_Diffuse(lightMapUV, standardPBRParams, envBRDF_Diffuse);
    #else
        return CalculateIBL_Diffuse(standardPBRParams, envBRDF_Diffuse);
    #endif
}



#endif