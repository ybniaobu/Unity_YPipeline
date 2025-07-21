#ifndef YPIPELINE_UNLIT_META_PASS_INCLUDED
#define YPIPELINE_UNLIT_META_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "../../../ShaderLibrary/UnityMetaPassLibrary.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float2 lightMapUV   : TEXCOORD1;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings MetaVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformMetaPosition(IN.positionOS.xyz, IN.lightMapUV);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

float4 MetaFrag(Varyings IN) : SV_TARGET
{
    UnityMetaParams meta = (UnityMetaParams) 0.0;
    meta.albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgb * _BaseColor.rgb;
    meta.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgb * _EmissionColor.rgb;
    return TransportMetaColor(meta);
}

#endif