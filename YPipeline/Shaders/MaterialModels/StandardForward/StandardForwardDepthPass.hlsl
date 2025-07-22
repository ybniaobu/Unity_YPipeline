#ifndef YPIPELINE_STANDARD_FORWARD_DEPTH_PASS_INCLUDED
#define YPIPELINE_STANDARD_FORWARD_DEPTH_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/YPipelineCore.hlsl"

CBUFFER_START (UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Specular;
    float _Roughness;
    float _Metallic;
    float _NormalIntensity;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _OpacityTex;

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings DepthVert(Attributes IN)
{
    Varyings OUT;
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    return OUT;
}

float DepthFrag(Varyings IN) : SV_DEPTH
{
    float baseTexAlpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).a;
    float opacityTexAlpha = SAMPLE_TEXTURE2D(_OpacityTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
    float alpha = baseTexAlpha * opacityTexAlpha * _BaseColor.a;

    #if defined(_CLIPPING)
        clip(alpha - _Cutoff);
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    return IN.positionHCS.z;
}

#endif