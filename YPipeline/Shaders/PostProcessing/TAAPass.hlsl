#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

float4 _CameraBufferSize;
TEXTURE2D(_CameraMotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor

float4 TAAFrag(Varyings IN) : SV_TARGET
{
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;
    
    float4 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0);
    float4 current = LOAD_TEXTURE2D_LOD(_BlitTexture, IN.positionHCS.xy, 0);

    float4 color = lerp(current, history, _TAAParams.x);
    return color;
}

#endif