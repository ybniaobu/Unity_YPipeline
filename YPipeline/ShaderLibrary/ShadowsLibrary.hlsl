#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "Core/YPipelineCore.hlsl"
#include "../ShaderLibrary/RandomLibrary.hlsl"
#include "../ShaderLibrary/SamplingLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Sample Shadow Map or Array
float SampleShadowMap_Compare(float3 positionSS, TEXTURE2D_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowMapSampler, positionSS);
    return shadowAttenuation;
}

float SampleShadowMap_Depth(float3 positionSS, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D(shadowMap, shadowMapSampler, positionSS.xy).r;
    return depth;
}

float SampleShadowMap_DepthCompare(float3 positionSS, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D(shadowMap, shadowMapSampler, positionSS.xy).r;
    return step(depth, positionSS.z);
}

float SampleShadowArray_Compare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_ARRAY_SHADOW(shadowMap, shadowMapSampler, positionSS, elementIndex);
    return shadowAttenuation;
}

float SampleShadowArray_Depth(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY(shadowMap, shadowMapSampler, positionSS.xy, elementIndex).r;
    return depth;
}

float SampleShadowArray_DepthCompare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY(shadowMap, shadowMapSampler, positionSS.xy, elementIndex).r;
    return step(depth, positionSS.z);
}

// ----------------------------------------------------------------------------------------------------
// Light/Shadow Space Transform
float3 TransformWorldToSunLightShadowCoord(float3 positionWS, int cascadeIndex)
{
    // SS: shadow space
    float3 positionSS = mul(_SunLightShadowMatrices[cascadeIndex], float4(positionWS, 1.0)).xyz;
    return positionSS;
}

// ----------------------------------------------------------------------------------------------------
// Cascade Shadow Related Functions
float ComputeCascadeIndex(float3 positionWS)
{
    float3 vector0 = positionWS - _CascadeCullingSpheres[0].xyz;
    float3 vector1 = positionWS - _CascadeCullingSpheres[1].xyz;
    float3 vector2 = positionWS - _CascadeCullingSpheres[2].xyz;
    float3 vector3 = positionWS - _CascadeCullingSpheres[3].xyz;
    float4 distanceSquare = float4(dot(vector0, vector0), dot(vector1, vector1), dot(vector2, vector2), dot(vector3, vector3));
    float4 radiusSquare = float4(_CascadeCullingSpheres[0].w * _CascadeCullingSpheres[0].w, _CascadeCullingSpheres[1].w * _CascadeCullingSpheres[1].w,
        _CascadeCullingSpheres[2].w * _CascadeCullingSpheres[2].w, _CascadeCullingSpheres[3].w * _CascadeCullingSpheres[3].w);
    
    float4 indexes = float4(distanceSquare < radiusSquare);
    indexes.yzw = saturate(indexes.yzw - indexes.xyz);
    return 4.0 - dot(indexes, float4(4.0, 3.0, 2.0, 1.0));
}

float ComputeDistanceFade(float3 positionWS, float reversedMaxDistance, float reversedDistanceFade)
{
    float depth = -TransformWorldToView(positionWS).z;
    return saturate((1 - depth * reversedMaxDistance) * reversedDistanceFade);
}

float ComputeCascadeEdgeFade(float cascadeIndex, int cascadeCount, float3 positionWS, float reversedCascadeEdgeFade, float4 lastSphere)
{
    //TODO:当最大距离比较小时，有点问题，建议更改
    float isInLastSphere = cascadeIndex == cascadeCount - 1;
    float3 distanceVector = positionWS - lastSphere.xyz;
    float distanceSquare = dot(distanceVector, distanceVector);
    float fade = saturate((1 - distanceSquare / (lastSphere.w * lastSphere.w)) * reversedCascadeEdgeFade);
    return lerp(1, fade, isInLastSphere);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Bias
float3 ApplyShadowBias(float3 positionWS, float texelSize, float penumbraWidth,  float3 normalWS, float3 L) //normalWS must be normalized
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 50.0); // maxBias

    float3 depthBias = texelSize * _ShadowBias.x * L;
    float3 scaledDepthBias = texelSize * tanTheta * _ShadowBias.y * L;
    float3 normalBias = texelSize * _ShadowBias.z * normalWS;
    float3 scaledNormalBias = texelSize * sinTheta * _ShadowBias.w * normalWS;

    depthBias += penumbraWidth * 0.01 * _ShadowBias.x * L;
    scaledDepthBias += penumbraWidth * 0.01 * tanTheta * _ShadowBias.y * L;
    normalBias += penumbraWidth * 0.01 * _ShadowBias.z * normalWS;
    scaledNormalBias += penumbraWidth * 0.01 * sinTheta * _ShadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions
float ApplyPCF_SunLight(float3 positionWS, float texelSize, float3 positionSS, float cascadeIndex)
{
    float random = Random_Sine(positionWS);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    float shadowAttenuation = 0.0;
    
    for (float i = 0; i < _SunLightShadowParams.z; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, 1, 2))) / _SunLightShadowParams.y;
        offset = offset / texelSize * _SunLightShadowParams.w * 0.01;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), cascadeIndex, _SunLightShadowArray, sampler_LinearClampCompare);
    }
    return shadowAttenuation / _SunLightShadowParams.z;
}

float GetSunLightShadowFalloff(float2 lightMapUV, float3 positionWS, float3 normalWS, float3 L)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    float shadowStrength = _SunLightColor.w;

    float shadowFade = 1.0 - step(_SunLightShadowParams.x, cascadeIndex);
    shadowFade *= ComputeDistanceFade(positionWS, _SunLightShadowFadeParams.x, _SunLightShadowFadeParams.y);
    shadowFade *= ComputeCascadeEdgeFade(cascadeIndex, _SunLightShadowParams.x, positionWS, _SunLightShadowFadeParams.z, _CascadeCullingSpheres[_SunLightShadowParams.x - 1]);

    float texelSize = _CascadeCullingSpheres[cascadeIndex].w * 2.0 / _SunLightShadowParams.y;
    float3 positionWS_Bias = ApplyShadowBias(positionWS, texelSize, _SunLightShadowParams.w, normalWS, L);
    float3 positionSS = TransformWorldToSunLightShadowCoord(positionWS_Bias, cascadeIndex);
    
    float shadowAttenuation = ApplyPCF_SunLight(positionWS, texelSize, positionSS, cascadeIndex);

    #if defined(_SHADOW_MASK_DISTANCE)
        return lerp(1.0, MixBakedAndRealtimeShadows(lightMapUV, shadowAttenuation, shadowFade), shadowStrength);
    #else
        return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
    #endif
}


#endif