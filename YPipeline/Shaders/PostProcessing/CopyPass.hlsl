﻿#ifndef YPIPELINE_SIMPLE_COPY_PASS_INCLUDED
#define YPIPELINE_SIMPLE_COPY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_LinearClamp);

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings CopyVert(uint vertexID : SV_VertexID)
{
    Varyings OUT;
    OUT.positionHCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    OUT.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    
    if (_ProjectionParams.x < 0.0)
    {
        OUT.uv.y = 1.0 - OUT.uv.y;
    }
    
    return OUT;
}

float4 CopyFrag(Varyings IN) : SV_TARGET
{
    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0);
}

#endif