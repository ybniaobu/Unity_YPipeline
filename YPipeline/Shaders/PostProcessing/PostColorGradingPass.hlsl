#ifndef YPIPELINE_POST_COLOR_GRADING_PASS_INCLUDED
#define YPIPELINE_POST_COLOR_GRADING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "CopyPass.hlsl"

float4 _VignetteColor;
float4 _VignetteParams1; // xy: center
float4 _VignetteParams2; // x: 3f * intensity, y: 5f * smoothness, z: roundness, w: rounded

float4 _PostColorGradingParams; // x: 1f / lutWidth, y: 1f / lutHeight, z: lutHeight - 1f
float4 _ExtraLutParams; // x: 1f / lutWidth, y: 1f / lutHeight, z: lutHeight - 1f, w: contribution

TEXTURE2D(_ColorGradingLutTexture);
TEXTURE2D(_ExtraLut);

float4 PostColorGradingFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;

    // Vignette
    float2 distance = abs(IN.uv - _VignetteParams1.xy) * _VignetteParams2.x;
    distance.x *= _VignetteParams2.w;
    distance = pow(saturate(distance), _VignetteParams2.z);
    float vfactor = pow(saturate(1.0 - dot(distance, distance)), _VignetteParams2.y);
    color *= lerp(_VignetteColor, (1.0).xxx, vfactor);

    // Color Grading Baked Lut
    color = ApplyLut2D(_ColorGradingLutTexture, sampler_LinearClamp, saturate(LinearToLogC(color)), _PostColorGradingParams.xyz);
    color = saturate(color);

    // Extra Lut
    UNITY_BRANCH
    if (_ExtraLutParams.w > 0.0)
    {
        color = LinearToSRGB(color);
        float3 outLut = ApplyLut2D(_ExtraLut, sampler_LinearClamp, color, _ExtraLutParams.xyz);
        color = lerp(color, outLut, _ExtraLutParams.w);
        color = SRGBToLinear(color);
    }

    color = saturate(color);
    return float4(color, 1.0f);
}

#endif