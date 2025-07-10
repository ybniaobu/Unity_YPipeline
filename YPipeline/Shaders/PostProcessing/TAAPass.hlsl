#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraMotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor
float4 _Jitter; // xy: jitter

float4 TAAFrag(Varyings IN) : SV_TARGET
{
    








    
    // float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    // float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, velocityPixelCoord, 0).rg;
    float2 velocity = LOAD_TEXTURE2D_LOD(_CameraMotionVectorTexture, IN.positionHCS.xy, 0).rg;
    // float2 velocity = SAMPLE_TEXTURE2D_LOD(_CameraMotionVectorTexture, sampler_LinearClamp, IN.uv - _Jitter.xy * _CameraBufferSize.xy, 0).rg;
    
    //float3 history = LOAD_TEXTURE2D_LOD(_TAAHistory, IN.positionHCS.xy - _CameraBufferSize.zw * velocity, 0).xyz;
    float3 history = SAMPLE_TEXTURE2D_LOD(_TAAHistory, sampler_LinearClamp, IN.uv - velocity, 0).xyz;
    float3 current = LOAD_TEXTURE2D_LOD(_BlitTexture, IN.positionHCS.xy, 0).xyz;
    //float3 current = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).xyz;

    // history = YCoCgClamp9(_BlitTexture, IN.positionHCS.xy, current, history);
    // history = YCoCgClip9(_BlitTexture, IN.positionHCS.xy, current, history);

    //float3 color = LumaExponentialAccumulation(history, current, _TAAParams.x);
    float3 color = lerp(current, history, _TAAParams.x);

    return float4(color, 1.0);
}

#endif