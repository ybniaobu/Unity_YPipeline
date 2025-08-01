﻿#ifndef YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED
#define YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ThinGBufferVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv * baseST.xy + baseST.zw;
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

float4 ThinGBufferFrag(Varyings IN, out float depth: SV_DEPTH) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(IN);

    #if defined(_CLIPPING)
        float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
        float alpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).a * baseColor.a;
        clip(alpha - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    #if _USE_ROUGHNESSTEX
        float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
    #else
        float roughness = _Roughness;
    #endif

    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_Trilinear_Repeat_BaseTex, IN.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = normalize(IN.normalWS);
        float3 t = normalize(IN.tangentWS.xyz);
        float3 b = normalize(cross(n, t) * IN.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        float3 N = normalize(mul(normalTS, tbn));
    #else
        float3 N = normalize(IN.normalWS);
    #endif

    depth = IN.positionHCS.z;
    float3 normal = (N + 1) * 0.5;

    return float4(normal, roughness);
}

#endif