#ifndef YPIPELINE_MOTION_VECTOR_PASS_INCLUDED
#define YPIPELINE_MOTION_VECTOR_PASS_INCLUDED

#include "../../PostProcessing/CopyPass.hlsl"

TEXTURE2D(_CameraDepthTexture);

float4 MotionVectorFrag(Varyings IN) : SV_TARGET
{
    float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, IN.positionHCS.xy, 0).r;

    // float3 currentPositionWS = ComputeWorldSpacePosition(IN.uv, depth, _NonJitteredInvViewProjMatrix);

    // float4 currentPositionCS = mul(_NonJitteredViewProjMatrix, float4(currentPositionWS.xyz, 1.0));
    // float4 preiousPositionCS = mul(_PrevViewProjMatrix, float4(currentPositionWS.xyz, 1.0));
    //
    // float2 currentPositionNDC = currentPositionCS.xy * rcp(currentPositionCS.w);
    // float2 preiousPositionNDC = preiousPositionCS.xy * rcp(preiousPositionCS.w);
    //
    // float2 velocity = currentPositionNDC - preiousPositionNDC;
    //
    // #if UNITY_UV_STARTS_AT_TOP
    // velocity.y = -velocity.y;
    // #endif
    //
    // velocity *= 0.5;

    return float4(0, 0, 0, 0);
}

#endif