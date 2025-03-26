#ifndef YPIPELINE_COLOR_GRADING_PASS_INCLUDED
#define YPIPELINE_COLOR_GRADING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "CopyPass.hlsl"

float4 _WhiteBalance;
float4 _ColorAdjustmentsParams; // x: hue, y: exposure, z: contrast, w: saturation
float4 _ColorFilter;
float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;

// ----------------------------------------------------------------------------------------------------
// White Balance
// ----------------------------------------------------------------------------------------------------

float3 WhiteBalance (float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}

// ----------------------------------------------------------------------------------------------------
// Color Adjustments
// ----------------------------------------------------------------------------------------------------

float3 ColorFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 Hue(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustmentsParams.x;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

float3 Exposure(float3 color)
{
    return color * _ColorAdjustmentsParams.y;
}

float3 Contrast(float3 color)
{
    color = LinearToLogC(color);
    color = lerp(ACEScc_MIDGRAY, color, _ColorAdjustmentsParams.z);
    return LogCToLinear(color);
}

float3 Saturation(float3 color)
{
    float luminance = Luminance(color);
    return lerp(float3(luminance, luminance, luminance), color, _ColorAdjustmentsParams.w);
}

float3 ColorAdjustments(float3 color)
{
    color = ColorFilter(color);
    color = Hue(color);
    color = Exposure(color);
    color = Contrast(color);
    color = Saturation(color);
    return max(color, 0.0);
}

// ----------------------------------------------------------------------------------------------------
// Shadows Midtones Highlights
// ----------------------------------------------------------------------------------------------------

float3 ShadowsMidtonesHighlights(float3 color)
{
    float luminance = Luminance(color);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHMidtones.rgb * midtonesWeight + color * _SMHHighlights.rgb * highlightsWeight;
}

// ----------------------------------------------------------------------------------------------------
// Fragment
// ----------------------------------------------------------------------------------------------------

float4 ColorGradingFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    color = WhiteBalance(color);
    color = ColorAdjustments(color);
    color = ShadowsMidtonesHighlights(color);
    
    return float4(max(color, 0.0), 1.0);
}

#endif