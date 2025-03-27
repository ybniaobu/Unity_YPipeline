#ifndef YPIPELINE_POST_COLOR_GRADING_PASS_INCLUDED
#define YPIPELINE_POST_COLOR_GRADING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "CopyPass.hlsl"

float4 _PostColorGradingParams; // x: 1f / lutWidth, y: 1f / lutHeight, z: lutHeight - 1f

TEXTURE2D(_ColorGradingLutTexture);

float4 PostColorGradingFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    color = ApplyLut2D(_ColorGradingLutTexture, sampler_LinearClamp, saturate(LinearToLogC(color)), _PostColorGradingParams.xyz);
    color = max(color, 0);
    return float4(color, 1.0f);
}

#endif