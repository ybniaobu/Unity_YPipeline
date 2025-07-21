#ifndef YPIPELINE_UNLIT_DEPTH_PASS_INCLUDED
#define YPIPELINE_UNLIT_DEPTH_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/YPipelineCore.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

Texture2D _BaseTex;     SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings DepthVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv * baseST.xy + baseST.zw;
    return OUT;
}

float DepthFrag(Varyings IN) : SV_DEPTH
{
    UNITY_SETUP_INSTANCE_ID(IN);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgba * baseColor;

    #if defined(_CLIPPING)
        clip(albedo.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
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