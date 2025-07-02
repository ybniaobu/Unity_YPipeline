#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor

float4 TAAFrag(Varyings IN) : SV_TARGET
{
    float4 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_PointClamp, IN.uv, 0);
    float4 current = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0);

    return lerp(current, history, _TAAParams.x);
}

#endif