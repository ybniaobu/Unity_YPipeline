#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

#include "../ShaderLibrary/BRDFModelLibrary.hlsl"
#include "../ShaderLibrary/IndirectLightingLibrary.hlsl"
#include "../ShaderLibrary/DirectLightingLibrary.hlsl"

struct RenderingEquationContent
{
    float3 directSunLight;
    float3 directPunctualLights;
    float3 indirectLightDiffuse;
    float3 indirectLightSpecular;
};



#endif