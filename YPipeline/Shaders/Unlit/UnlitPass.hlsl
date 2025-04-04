﻿#ifndef YPIPELINE_UNLIT_PASS_INCLUDED
#define YPIPELINE_UNLIT_PASS_INCLUDED

#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;     SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Varyings UnlitVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

float4 UnlitFrag(Varyings IN) : SV_Target
{
    float3 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgb * _EmissionColor.rgb;
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgba * _BaseColor.rgba;
    
    #if defined(_CLIPPING)
        clip(albedo.a - _Cutoff);
    #endif

    return float4(albedo.rgb + emission, 1.0);
}

#endif