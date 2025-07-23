#ifndef YPIPELINE_CAMERA_MOTION_VECTOR_PASS_INCLUDED
#define YPIPELINE_CAMERA_MOTION_VECTOR_PASS_INCLUDED

#include "../../PostProcessing/CopyPass.hlsl"

TEXTURE2D(_CameraDepthTexture);

float4 GetNDCFromUVAndDepth(float2 uv, float depth)
{
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0f - uv.y;
    #else
        depth = 2.0 * depth - 1.0;
    #endif
    
    return float4(2.0 * uv - 1.0, depth, 1.0);
}

float3 TransformNDCToWorld(float4 NDC, float4x4 invViewProjMatrix)
{
    float4 positionHWS = mul(invViewProjMatrix, NDC);
    return positionHWS.xyz / positionHWS.w;
}

float4 CameraMotionVectorFrag(Varyings IN) : SV_TARGET
{
    float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, IN.positionHCS.xy, 0).r;
    float4 NDC = GetNDCFromUVAndDepth(IN.uv, depth);
    float3 currentPositionWS = TransformNDCToWorld(NDC, UNITY_MATRIX_I_VP);

    float4 currentPositionCS = mul(UNITY_MATRIX_NONJITTERED_VP, float4(currentPositionWS.xyz, 1.0));
    float4 previousPositionCS = mul(UNITY_PREV_MATRIX_NONJITTERED_VP, float4(currentPositionWS.xyz, 1.0));
    
    float2 currentPositionNDC = currentPositionCS.xy / currentPositionCS.w;
    float2 previousPositionNDC = previousPositionCS.xy / previousPositionCS.w;
    
    float2 velocity = currentPositionNDC - previousPositionNDC;
    
    #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
    #endif
    
    velocity *= 0.5;

    return float4(velocity, 0, 0);
}

#endif