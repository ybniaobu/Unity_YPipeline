#ifndef YPIPELINE_STANDARD_PBR_DEPTH_PASS_INCLUDED
#define YPIPELINE_STANDARD_PBR_DEPTH_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/YPipelineCore.hlsl"

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
    #if defined(_CLIPPING)
        float alpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).a * _BaseColor.a;
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