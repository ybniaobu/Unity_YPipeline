#ifndef YPIPELINE_TONE_MAPPING_PASS_INCLUDED
#define YPIPELINE_TONE_MAPPING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"
#include "../../ShaderLibrary/ToneMappingLibrary.hlsl"

#include "CopyPass.hlsl"

float4 _ToneMappingParams; // x: minWhite or exposureBias

float4 ToneMappingReinhardSimpleFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(Reinhard(color), 1.0);
}

float4 ToneMappingReinhardExtendedFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(Reinhard_Extended(color, _ToneMappingParams.x), 1.0);
}

float4 ToneMappingReinhardLuminanceFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(Reinhard_ExtendedLuminance(color, _ToneMappingParams.x), 1.0);
}

// float4 ToneMappingNeutralFrag(Varyings IN) : SV_TARGET
// {
//     float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
//     
//     color = NeutralTonemap(color);
//     return float4(color, 1.0);
// }

float4 ToneMappingUncharted2FilmicFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(Uncharted2Filmic(color, _ToneMappingParams.x), 1.0);
}

float4 ToneMappingKhronosPBRNeutralFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(KhronosPBRNeutral(color), 1.0);
}

float4 ToneMappingACESFullFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(AcesTonemap(unity_to_ACES(color)), 1.0);
}

float4 ToneMappingACESStephenHillFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(ACESStephenHillFit(color), 1.0);
}

float4 ToneMappingACESApproxFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(ACESApproxFit(color), 1.0);
}

float4 ToneMappingAgXDefaultFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(AgXApprox_Default(color), 1.0);
}

float4 ToneMappingAgXGoldenFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(AgXApprox_Golden(color), 1.0);
}

float4 ToneMappingAgXPunchyFrag(Varyings IN) : SV_TARGET
{
    float3 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(AgXApprox_Punchy(color), 1.0);
}
#endif