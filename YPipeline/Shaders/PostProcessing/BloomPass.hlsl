#ifndef YPIPELINE_BLOOM_PASS_INCLUDED
#define YPIPELINE_BLOOM_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "../../ShaderLibrary/Core/UnityInput.hlsl"

float4 _BloomThreshold;
float _BloomIntensity;

TEXTURE2D(_BlitTexture);
float4 _BlitTexture_TexelSize;
TEXTURE2D(_BloomLowerTexture);
float4 _BloomLowerTexture_TexelSize;

SAMPLER(sampler_LinearClamp);

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

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

Varyings BloomVert(uint vertexID : SV_VertexID)
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

float4 BloomGaussianBlurHorizontalFrag(Varyings IN) : SV_TARGET
{
    float3 color = float3(0.0, 0.0, 0.0);
    float offsets[9] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };
    float weights[9] = { 0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703, 0.19459459, 0.12162162, 0.05405405, 0.01621622 };

    // 9×9 Gaussian filter
    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float offset = offsets[i] * _BlitTexture_TexelSize.x * 2.0;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv + float2(offset, 0), 0).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

float4 BloomGaussianBlurVerticalFrag(Varyings IN) : SV_TARGET
{
    float3 color = float3(0.0, 0.0, 0.0);
    float offsets[5] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };
    float weights[5] = { 0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027 };

    // 5×5 Gaussian filter
    UNITY_UNROLL
    for (int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * _BlitTexture_TexelSize.y;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv + float2(0, offset), 0).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

float4 BloomUpsampleFrag(Varyings IN) : SV_TARGET
{
    #if _BLOOM_BICUBIC_UPSAMPLING
        float3 lowerTex = SampleTexture2DBicubic(_BloomLowerTexture, sampler_LinearClamp, IN.uv, _BloomLowerTexture_TexelSize.zwxy, (1.0).xx, 0.0).rgb;
    #else
        float3 lowerTex = SAMPLE_TEXTURE2D_LOD(_BloomLowerTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    #endif
    
    float3 higherTex = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(lowerTex * _BloomIntensity + higherTex, 1.0);
}

float4 BloomPrefilterFrag(Varyings IN) : SV_TARGET
{
    #if _BLOOM_BICUBIC_UPSAMPLING
        float3 color = ApplyBloomThreshold(SampleTexture2DBicubic(_BlitTexture, sampler_LinearClamp, IN.uv, _BlitTexture_TexelSize.zwxy, (1.0).xx, 0.0).rgb);
    #else
        float3 color = ApplyBloomThreshold(SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb);
    #endif
    return float4(color, 1.0);
}

#endif