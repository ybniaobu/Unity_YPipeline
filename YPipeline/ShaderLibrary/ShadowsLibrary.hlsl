﻿#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "Core/YPipelineCore.hlsl"
#include "../ShaderLibrary/RandomLibrary.hlsl"
#include "../ShaderLibrary/SamplingLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Sample Shadow Map or Array
// ----------------------------------------------------------------------------------------------------

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

float SampleShadowCubeArray_Compare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURECUBE_ARRAY_SHADOW(shadowMap, shadowMapSampler, float4(sampleDir, z), elementIndex);
    return shadowAttenuation;
}

float SampleShadowCubeArray_Depth(float3 sampleDir, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY(shadowMap, shadowMapSampler, sampleDir, elementIndex).r;
    return depth;
}

float SampleShadowCubeArray_DepthCompare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY(shadowMap, shadowMapSampler, sampleDir, elementIndex).r;
    return step(depth, z);
}

// ----------------------------------------------------------------------------------------------------
// Light/Shadow Space Transform
// ----------------------------------------------------------------------------------------------------

float3 TransformWorldToSunLightShadowCoord(float3 positionWS, int cascadeIndex)
{
    // SS: shadow space
    float3 positionSS = mul(GetSunLightShadowMatrix(cascadeIndex), float4(positionWS, 1.0)).xyz;
    return positionSS;
}

float3 TransformWorldToSpotLightShadowCoord(float3 positionWS, int shadowingLightIndex)
{
    // SS: shadow space
    float4 positionSS_BeforeDivision = mul(GetSpotLightShadowMatrix(shadowingLightIndex), float4(positionWS, 1.0));
    float3 positionSS = positionSS_BeforeDivision.xyz / positionSS_BeforeDivision.w;
    return positionSS;
}

float3 TransformWorldToPointLightShadowCoord(float3 positionWS, int shadowingLightIndex, float faceIndex)
{
    // SS: shadow space
    float4 positionSS_BeforeDivision = mul(GetPointLightShadowMatrix(shadowingLightIndex * 6 + faceIndex), float4(positionWS, 1.0));
    float3 positionSS = positionSS_BeforeDivision.xyz / positionSS_BeforeDivision.w;
    return positionSS;
}

// ----------------------------------------------------------------------------------------------------
// Cascade Shadow Related Functions
// ----------------------------------------------------------------------------------------------------

float ComputeCascadeIndex(float3 positionWS)
{
    float3 vector0 = positionWS - GetCascadeCullingSphereCenter(0);
    float3 vector1 = positionWS - GetCascadeCullingSphereCenter(1);
    float3 vector2 = positionWS - GetCascadeCullingSphereCenter(2);
    float3 vector3 = positionWS - GetCascadeCullingSphereCenter(3);
    float4 distanceSquare = float4(dot(vector0, vector0), dot(vector1, vector1), dot(vector2, vector2), dot(vector3, vector3));
    float4 radiusSquare = float4(GetCascadeCullingSphereRadius(0) * GetCascadeCullingSphereRadius(0),
                                 GetCascadeCullingSphereRadius(1) * GetCascadeCullingSphereRadius(1),
                                 GetCascadeCullingSphereRadius(2) * GetCascadeCullingSphereRadius(2),
                                 GetCascadeCullingSphereRadius(3) * GetCascadeCullingSphereRadius(3));
    
    float4 indexes = float4(distanceSquare < radiusSquare);
    indexes.yzw = saturate(indexes.yzw - indexes.xyz);
    return 4.0 - dot(indexes, float4(4.0, 3.0, 2.0, 1.0));
}

float ComputeDistanceFade(float3 positionWS, float maxDistance, float distanceFade)
{
    float depth = -TransformWorldToView(positionWS).z;
    return saturate((1 - depth / maxDistance) / distanceFade);
}

float ComputeCascadeEdgeFade(float cascadeIndex, int cascadeCount, float3 positionWS, float cascadeEdgeFade, float4 lastSphere)
{
    //TODO: 当最大距离比较小时，有点问题，建议更改
    float isInLastSphere = cascadeIndex == cascadeCount - 1;
    float3 distanceVector = positionWS - lastSphere.xyz;
    float distanceSquare = dot(distanceVector, distanceVector);
    float fade = saturate((1 - distanceSquare / (lastSphere.w * lastSphere.w)) / cascadeEdgeFade);
    return lerp(1, fade, isInLastSphere);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Bias Related Functions
// ----------------------------------------------------------------------------------------------------

float ComputeTanHalfFOV(int spotLightIndex)
{
    float cosHalfFOV = GetSpotLightCosOuterAngle(spotLightIndex);
    float cosHalfFOVSquare = cosHalfFOV * cosHalfFOV;
    float sinHalfFOVSquare = 1.0 - cosHalfFOVSquare;
    float tanHalfFOVSquare = sinHalfFOVSquare / cosHalfFOVSquare;
    return sqrt(tanHalfFOVSquare);
}

//normalWS must be normalized
float3 ApplyShadowBias(float3 positionWS, float texelSize, float penumbraWidth,  float3 normalWS, float3 L)
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 50.0); // maxBias

    float3 depthBias = texelSize * (1.0 + penumbraWidth) * _ShadowBias.x * L;
    float3 scaledDepthBias = texelSize * (1.0 + penumbraWidth) * tanTheta * _ShadowBias.y * L;
    float3 normalBias = texelSize * (1.0 + penumbraWidth) * _ShadowBias.z * normalWS;
    float3 scaledNormalBias = texelSize * (1.0 + penumbraWidth) * sinTheta * _ShadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// ----------------------------------------------------------------------------------------------------
// PCF
// ----------------------------------------------------------------------------------------------------

//index could be lightIndex or cascadeIndex
float ApplyPCF_2DArray(float index, TEXTURE2D_ARRAY_SHADOW(shadowMap), float shadowArraySize, float sampleNumber, float penumbra, float3 positionWS, float3 positionSS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) / shadowArraySize;
        offset = offset * penumbra;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), index, shadowMap, SHADOW_SAMPLER);
    }
    return shadowAttenuation / sampleNumber;
}

float3 GetCubeMapOffset(float faceIndex, float2 offset)
{
    float3 cubeMapOffset;
    if (faceIndex >= -0.1f && faceIndex <= 1.1f)
    {
        cubeMapOffset = float3(0.0, offset.x, offset.y);
    }
    else if (faceIndex >= 1.9f && faceIndex <= 3.1f)
    {
        cubeMapOffset = float3(offset.x, 0, offset.y);
    }
    else
    {
        cubeMapOffset = float3(offset.y, offset.x, 0);
    }
    return cubeMapOffset;
}

float ApplyPCF_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), float shadowArraySize, float sampleNumber, float penumbra, float3 sampleDir, float3 positionWS, float3 positionSS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) / shadowArraySize;
        offset = offset * penumbra;
        float3 sampleDir_Offset = sampleDir + GetCubeMapOffset(faceIndex, offset) * 2;
        shadowAttenuation += SampleShadowCubeArray_Compare(sampleDir_Offset, positionSS.z, index, shadowMap, SHADOW_SAMPLER);
    }
    return shadowAttenuation / sampleNumber;
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions
// ----------------------------------------------------------------------------------------------------

float GetSunLightShadowAttenuation(float2 lightMapUV, float3 positionWS, float3 normalWS, float3 L)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    float shadowStrength = GetSunLightShadowStrength();

    UNITY_BRANCH
    if (cascadeIndex >= GetSunLightCascadeCount())
    {
        #if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK_NORMAL)
            return lerp(1.0, SampleShadowmask(lightMapUV, GetSunLightShadowMaskChannel()), shadowStrength);
        #else
            return 1.0;
        #endif
    }
    else
    {
        float shadowFade = 1.0;
        shadowFade *= ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
        shadowFade *= ComputeCascadeEdgeFade(cascadeIndex, GetSunLightCascadeCount(), positionWS, GetCascadeEdgeFade(), GetCascadeCullingSphere(GetSunLightCascadeCount() - 1));

        float texelSize = GetCascadeCullingSphereRadius(cascadeIndex) * 2.0 / GetSunLightShadowArraySize();
        float penumbraWidth = GetSunLightShadowPenumbraWidth() / texelSize;
        float3 positionWS_Bias = ApplyShadowBias(positionWS, texelSize, penumbraWidth, normalWS, L);
        float3 positionSS = TransformWorldToSunLightShadowCoord(positionWS_Bias, cascadeIndex);
        float shadowAttenuation = ApplyPCF_2DArray(cascadeIndex, SUN_LIGHT_SHADOW_MAP, GetSunLightShadowArraySize(), GetSunLightShadowSampleNumber(), penumbraWidth, positionWS, positionSS);

        #if defined(_SHADOW_MASK_DISTANCE)
            return lerp(1.0, MixBakedAndRealtimeShadows(lightMapUV, GetSunLightShadowMaskChannel(), shadowAttenuation, shadowFade), shadowStrength);
        #elif defined(_SHADOW_MASK_NORMAL)
            return lerp(1.0, ChooseBakedAndRealtimeShadows(lightMapUV, GetSunLightShadowMaskChannel(), shadowAttenuation, shadowFade), shadowStrength);
        #else
            return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
        #endif
    }
}

float GetSpotLightShadowAttenuation(int lightIndex, float2 lightMapUV, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetSpotLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingSpotLightIndex = GetShadowingSpotLightIndex(lightIndex);
    //float linearDepth = mul(GetSpotLightShadowMatrix(shadowingSpotLightIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth / GetPunctualLightShadowArraySize();
    float penumbraWidth = GetPunctualLightShadowPenumbraWidth() * GetPunctualLightShadowArraySize();
    float3 positionWS_Bias = ApplyShadowBias(positionWS, texelSize, penumbraWidth, normalWS, L);
    float3 positionSS = TransformWorldToSpotLightShadowCoord(positionWS_Bias, shadowingSpotLightIndex);
    float shadowAttenuation = ApplyPCF_2DArray(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, GetPunctualLightShadowArraySize(), GetPunctualLightShadowSampleNumber(), penumbraWidth, positionWS, positionSS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

float GetPointLightShadowAttenuation(int lightIndex, float faceIndex, float2 lightMapUV, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetPointLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingPointLightIndex = GetShadowingPointLightIndex(lightIndex);
    //float linearDepth = mul(GetPointLightShadowMatrix(shadowingPointLightIndex * 6 + faceIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * linearDepth / GetPunctualLightShadowArraySize();
    float penumbraWidth = GetPunctualLightShadowPenumbraWidth() * GetPunctualLightShadowArraySize();
    float3 positionWS_Bias = ApplyShadowBias(positionWS, texelSize, penumbraWidth, normalWS, L);
    float3 sampleDir = normalize(positionWS_Bias - GetPointLightPosition(lightIndex));
    float3 positionSS = TransformWorldToPointLightShadowCoord(positionWS_Bias, shadowingPointLightIndex, faceIndex);
    float shadowAttenuation = ApplyPCF_CubeArray(shadowingPointLightIndex, faceIndex, POINT_LIGHT_SHADOW_MAP, GetPunctualLightShadowArraySize(), GetPunctualLightShadowSampleNumber(), penumbraWidth, sampleDir, positionWS, positionSS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

#endif