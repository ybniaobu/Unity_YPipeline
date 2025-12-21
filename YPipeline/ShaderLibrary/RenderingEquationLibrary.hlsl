#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

struct RenderingEquationContent
{
    float3 directSunLight;
    float3 directPunctualLights;
    float3 indirectLightDiffuse;
    float3 indirectLightSpecular;
};

struct GeometryParams
{
    float3 positionWS;
    float3 normalWS;
    float2 uv;
    float2 pixelCoord; // Screen Pixel Coordinate 屏幕像素坐标
    float2 screenUV;
    
    #if defined(LIGHTMAP_ON)
        float2 lightMapUV;
    #endif
};

#include "../ShaderLibrary/BRDFModelLibrary.hlsl"
#include "../ShaderLibrary/IndirectLightingLibrary.hlsl"
#include "../ShaderLibrary/DirectLightingLibrary.hlsl"

#endif