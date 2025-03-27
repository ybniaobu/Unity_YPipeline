#ifndef YPIPELINE_COLOR_GRADING_LUT_PASS_INCLUDED
#define YPIPELINE_COLOR_GRADING_LUT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"
#include "../../ShaderLibrary/ToneMappingLibrary.hlsl"

#include "CopyPass.hlsl"

float4 _ColorGradingLUTParams; // x: lut_height, y: 0.5 / lut_width, z: 0.5 / lut_height, w: lut_height / lut_height - 1

float4 _WhiteBalance;
float4 _ColorAdjustmentsParams; // x: hue, y: exposure, z: contrast, w: saturation
float4 _ColorFilter;
float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;

float4 _ToneMappingParams; // x: minWhite or exposureBias

// ----------------------------------------------------------------------------------------------------
// White Balance
// ----------------------------------------------------------------------------------------------------

float3 WhiteBalance(float3 color)
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

float3 Contrast_ACES(float3 color)
{
    color = ACES_to_ACEScc(ACEScg_to_ACES(color));
    color = lerp(ACEScc_MIDGRAY, color, _ColorAdjustmentsParams.z);
    return ACES_to_ACEScg(ACEScc_to_ACES(color));
}

float3 Saturation(float3 color)
{
    float luminance = Luminance(color);
    return lerp(float3(luminance, luminance, luminance), color, _ColorAdjustmentsParams.w);
}

float3 Saturation_ACES(float3 color)
{
    float luminance = AcesLuminance(color);
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

float3 ColorAdjustments_ACES(float3 color)
{
    color = ColorFilter(color);
    color = Hue(color);
    color = Exposure(color);
    color = Contrast_ACES(color);
    color = Saturation_ACES(color);
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

float3 ShadowsMidtonesHighlights_ACES(float3 color)
{
    float luminance = AcesLuminance(color);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHMidtones.rgb * midtonesWeight + color * _SMHHighlights.rgb * highlightsWeight;
}

// ----------------------------------------------------------------------------------------------------
// Overall Color Grading
// ----------------------------------------------------------------------------------------------------

float4 ColorGrading(float3 color)
{
    color = WhiteBalance(color);
    color = ColorAdjustments(color);
    color = ShadowsMidtonesHighlights(color);
    return float4(max(color, 0.0), 1.0);
}

float4 ColorGrading_ACES(float3 color)
{
    color = WhiteBalance(color);

    color = unity_to_ACEScg(color);
    color = ColorAdjustments_ACES(color);
    color = ShadowsMidtonesHighlights_ACES(color);
    return float4(max(color, 0.0), 1.0);
}

// ----------------------------------------------------------------------------------------------------
// Fragment
// ----------------------------------------------------------------------------------------------------

float4 ColorGradingNoneFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(color, 1.0);
}

float4 ColorGradingReinhardSimpleFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard(color), 1.0);
}

float4 ColorGradingReinhardExtendedFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard_Extended(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingReinhardLuminanceFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard_ExtendedLuminance(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingUncharted2FilmicFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Uncharted2Filmic(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingKhronosPBRNeutralFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(KhronosPBRNeutral(color), 1.0);
}

float4 ColorGradingACESFullFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(AcesTonemap(ACEScg_to_ACES(color)), 1.0);
}

float4 ColorGradingACESStephenHillFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(ACESStephenHillFit(ACEScg_to_unity(color)), 1.0);
}

float4 ColorGradingACESApproxFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(ACESApproxFit(ACEScg_to_unity(color)), 1.0);
}

float4 ColorGradingAgXDefaultFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Default(color), 1.0);
}

float4 ColorGradingAgXGoldenFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Golden(color), 1.0);
}

float4 ColorGradingAgXPunchyFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Punchy(color), 1.0);
}

#endif