﻿#ifndef YPIPELINE_UBER_POST_PROCESSING_PASS_INCLUDED
#define YPIPELINE_UBER_POST_PROCESSING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"
#include "CopyPass.hlsl"

float4 _ChromaticAberrationParams; // x: 0.05f * intensity, y: sample count

float4 _BloomThreshold; // x: threshold, y: -threshold + threshold * thresholdKnee, z: 2 * threshold * thresholdKnee, w: 1 / 4 * threshold * thresholdKnee
float4 _BloomParams; // x: intensity or scatter, y: mode

float4 _VignetteColor;
float4 _VignetteParams1; // xy: center
float4 _VignetteParams2; // x: 3f * intensity, y: 5f * smoothness, z: roundness, w: rounded

float4 _ColorGradingLutParams; // x: 1f / lutWidth, y: 1f / lutHeight, z: lutHeight - 1f
float4 _ExtraLutParams; // x: 1f / lutWidth, y: 1f / lutHeight, z: lutHeight - 1f, w: contribution

float4 _FilmGrainParams; // x: intensity, y: response
float4 _FilmGrainTexParams; // xy: CameraSize.xy / FilmGrainTexSize.xy, zw: random offset in uv

TEXTURE2D(_SpectralLut);
TEXTURE2D(_BloomTex);
float4 _BloomTex_TexelSize;
TEXTURE2D(_ColorGradingLutTexture);
TEXTURE2D(_ExtraLut);
TEXTURE2D(_FilmGrainTex);
SAMPLER(sampler_SpectralLut);

float3 ApplyBloomThreshold(float3 color)
{
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 UberPostProcessingFrag(Varyings IN) : SV_TARGET
{
    float4 inputColor = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0);
    float3 color = inputColor.rgb;
    
    // Chromatic Aberration
    #if _CHROMATIC_ABERRATION
        float2 coords = 2.0 * IN.uv - 1.0;
        float2 end = IN.uv - coords * dot(coords, coords) * _ChromaticAberrationParams.x;
        float2 diff = end - IN.uv;
        int samples = clamp(int(length(diff * _ScreenParams.xy / 2.0)), 3, _ChromaticAberrationParams.y);
        float2 delta = diff / samples;
        float2 pos = IN.uv;
        float3 sum = 0.0, filterSum = 0.0;
    
        for (int i = 0; i < samples; i++)
        {
            float t = (i + 0.5) / samples;
            float3 s = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, pos, 0.0).rgb;
            float3 filter = SAMPLE_TEXTURE2D_LOD(_SpectralLut, sampler_SpectralLut, float2(t, 0.0), 0).rgb;

            sum += s * filter;
            filterSum += filter;
            pos += delta;
        }
        color = sum / filterSum;
    #endif

    // Bloom
    #if _BLOOM
        #if _BLOOM_BICUBIC_UPSAMPLING
            float3 bloomTex = SampleTexture2DBicubic(_BloomTex, sampler_LinearClamp, IN.uv, _BloomTex_TexelSize.zwxy, (1.0).xx, 0.0).rgb;
        #else
            float3 bloomTex = SAMPLE_TEXTURE2D_LOD(_BloomTex, sampler_LinearClamp, IN.uv, 0).rgb;
        #endif
    
        UNITY_BRANCH
        if (_BloomParams.y > 0)
        {
            bloomTex += color - ApplyBloomThreshold(color);
            color = lerp(color, bloomTex, _BloomParams.x);
        }
        else
        {
            color = bloomTex * _BloomParams.x + color;
        }
    #endif
    
    // Vignette
    #if _VIGNETTE
        float2 distance = abs(IN.uv - _VignetteParams1.xy) * _VignetteParams2.x;
        distance.x *= _VignetteParams2.w;
        distance = pow(saturate(distance), _VignetteParams2.z);
        float vfactor = pow(saturate(1.0 - dot(distance, distance)), _VignetteParams2.y);
        color *= lerp(_VignetteColor.rgb, (1.0).xxx, vfactor);
        color = max(color, 0);
    #endif

    // Color Grading Baked Lut
    color = ApplyLut2D(_ColorGradingLutTexture, sampler_LinearClamp, saturate(LinearToLogC(color)), _ColorGradingLutParams.xyz);
    color = saturate(color);

    // Extra Lut
    #if _EXTRA_LUT
        color = LinearToSRGB(color);
        float3 outLut = ApplyLut2D(_ExtraLut, sampler_LinearClamp, color, _ExtraLutParams.xyz);
        color = lerp(color, outLut, _ExtraLutParams.w);
        color = SRGBToLinear(color);
    #endif

    // Film Grain
    #if _FILM_GRAIN
        float grain = SAMPLE_TEXTURE2D(_FilmGrainTex, sampler_LinearRepeat, IN.uv * _FilmGrainTexParams.xy + _FilmGrainTexParams.zw).w;
        grain = (grain - 0.5) * 2.0; // Remap [-1, 1]
        float lum = Luminance(color);
        lum = 1.0 - sqrt(lum);
        lum = lerp(1.0, lum, _FilmGrainParams.y);
        color += color * grain * _FilmGrainParams.x * lum;
    #endif

    color = saturate(color);
    return float4(color, inputColor.a);
}

#endif