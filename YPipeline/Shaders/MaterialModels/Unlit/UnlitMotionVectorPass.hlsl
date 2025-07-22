#ifndef YPIPELINE_UNLIT_MOTION_VECTOR_PASS_INCLUDED
#define YPIPELINE_UNLIT_MOTION_VECTOR_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/YPipelineCore.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

Texture2D _BaseTex;     SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;

#include "../MotionVectorCommon.hlsl"

#endif