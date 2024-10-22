#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

#include "Assets/ShaderLibrary/PunctualLightsLibrary.hlsl"
#include "Assets/ShaderLibrary/BRDFModelLibrary.hlsl"

struct RenderingEquationContent
{
    float3 directMainLight;
    float3 directAdditionalLight;
    float3 indirectLight;
};

//TODO：计算上述结构所有内容的函数

#endif