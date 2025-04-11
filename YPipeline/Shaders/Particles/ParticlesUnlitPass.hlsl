#ifndef YPIPELINE_PARTICLES_UNLIT_PASS_INCLUDED
#define YPIPELINE_PARTICLES_UNLIT_PASS_INCLUDED

#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

Texture2D _BaseTex;     SamplerState sampler_Trilinear_Repeat_BaseTex;

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    
    #if defined(_FLIPBOOK_BLENDING)
        float4 uv : TEXCOORD0;
        float uvBlend : TEXCOORD1;
    #else
        float2 uv : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;

    #if defined(_FLIPBOOK_BLENDING)
        float3 uv2AndBlend : TEXCOORD1;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ParticlesUnlitVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.color = IN.color;
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv.xy * baseST.xy + baseST.zw;
    #if defined(_FLIPBOOK_BLENDING)
        OUT.uv2AndBlend.xy = IN.uv.zw * baseST.xy + baseST.zw;
        OUT.uv2AndBlend.z = IN.uvBlend;
    #endif
    return OUT;
}

float4 ParticlesUnlitFrag(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgba * baseColor;

    #if defined(_FLIPBOOK_BLENDING)
        albedo = lerp(albedo, SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv2AndBlend.xy), IN.uv2AndBlend.z);
    #endif
    
    #if defined(_CAMERA_NEAR_FADE)
        float depth = GetViewDepthFromSVPosition(IN.positionHCS);
        float nearFadeDistance = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeDistance);
        float nearFadeRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeRange);
        float nearAttenuation = (depth - nearFadeDistance) / nearFadeRange;
        albedo.a *= saturate(nearAttenuation);
    #endif
    
    #if defined(_CLIPPING)
        clip(albedo.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    
    return float4(albedo.rgb * IN.color.rgb, albedo.a * IN.color.a);
}

#endif