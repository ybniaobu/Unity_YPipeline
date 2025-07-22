﻿#ifndef YPIPELINE_MOTION_VECTOR_COMMON_INCLUDED
#define YPIPELINE_MOTION_VECTOR_COMMON_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    
    float3 previousPositionOS : TEXCOORD4;
    #if _ADD_PRECOMPUTED_VELOCITY
        float3 precomputedVelocity : TEXCOORD5;
    #endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nonJitterPositionHCS : TEXCOORD1;
    float4 previousNonJitterPositionHCS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings MotionVectorVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv * baseST.xy + baseST.zw;

    OUT.nonJitterPositionHCS = mul(UNITY_MATRIX_NONJITTERED_VP, mul(UNITY_MATRIX_M, float4(IN.positionOS.xyz, 1.0)));

    // float4 previousPositionOS = (unity_MotionVectorsParams.x == 1) ? float4(IN.previousPositionOS, 1.0) : IN.positionOS;
    float4 previousPositionOS = float4(IN.previousPositionOS, 1.0);
    #if _ADD_PRECOMPUTED_VELOCITY
        previousPositionOS = previousPositionOS - float4(IN.precomputedVelocity, 0);
    #endif
    OUT.previousNonJitterPositionHCS = mul(UNITY_PREV_MATRIX_NONJITTERED_VP, mul(UNITY_PREV_MATRIX_M, float4(IN.positionOS.xyz, 1.0)));

    return OUT;
}

float4 MotionVectorFrag(Varyings IN) : SV_Target
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

    // bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    float2 currentPositionNDC = IN.nonJitterPositionHCS.xy * rcp(IN.nonJitterPositionHCS.w);
    float2 previousPositionNDC = IN.previousNonJitterPositionHCS.xy * rcp(IN.previousNonJitterPositionHCS.w);

    float2 velocity = currentPositionNDC - previousPositionNDC;

    #if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
    #endif
    
    velocity *= 0.5;

    return float4(velocity, 0, 0);
}

#endif