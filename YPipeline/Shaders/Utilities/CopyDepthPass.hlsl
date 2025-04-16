#ifndef YPIPELINE_COPY_DEPTH_PASS_INCLUDED
#define YPIPELINE_COPY_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"

TEXTURE2D(_CameraDepthBuffer);
SAMPLER(sampler_CameraDepthBuffer);

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings CopyDepthVert(uint vertexID : SV_VertexID)
{
    Varyings OUT;
    
    //OUT.positionHCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    //OUT.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    OUT.uv = float2((vertexID << 1) & 2, vertexID & 2);
    OUT.positionHCS = float4(OUT.uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
    
    if (_ProjectionParams.x < 0.0) OUT.uv.y = 1.0 - OUT.uv.y;
    
    // #if UNITY_UV_STARTS_AT_TOP
    //     OUT.uv.y = 1.0 - OUT.uv.y;
    // #endif
    
    return OUT;
}

float CopyDepthFrag(Varyings IN) : SV_DEPTH
{
    return SAMPLE_TEXTURE2D_LOD(_CameraDepthBuffer, sampler_CameraDepthBuffer, IN.uv, 0).r;
}

#endif