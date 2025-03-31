#ifndef YPIPELINE_UNITY_INPUT_INCLUDED
#define YPIPELINE_UNITY_INPUT_INCLUDED

// Unity Engine built-in shader input variables.

// ----------------------------------------------------------------------------------------------------
// SRP Batcher
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams;

    // Occlusion Probes
    float4 unity_ProbesOcclusion;

    // HDR environment map decode instruction
    // float4 unity_SpecCube0_HDR;

    // Lightmap
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // SH
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// Other variables
// ----------------------------------------------------------------------------------------------------

float3 _WorldSpaceCameraPos;

// x = 1 or -1 (-1 if projection is flipped)
// y = near plane
// z = far plane
// w = 1/far plane
float4 _ProjectionParams;

// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
float4 _ScreenParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;

CBUFFER_START(UnityPerFrame)
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixInvP;
    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixInvVP;
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);


// ----------------------------------------------------------------------------------------------------
// for SpaceTransforms.hlsl compatibility
// ----------------------------------------------------------------------------------------------------

#define UNITY_MATRIX_M          unity_ObjectToWorld
#define UNITY_MATRIX_I_M        unity_WorldToObject
#define UNITY_MATRIX_V          unity_MatrixV
#define UNITY_MATRIX_I_V        unity_MatrixInvV
#define UNITY_MATRIX_P          glstate_matrix_projection
#define UNITY_MATRIX_I_P        unity_MatrixInvP
#define UNITY_MATRIX_VP         unity_MatrixVP
#define UNITY_MATRIX_I_VP       unity_MatrixInvVP
#define UNITY_MATRIX_MV         mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV       transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV      transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP        mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M     unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M   unity_MatrixPreviousMI

#endif