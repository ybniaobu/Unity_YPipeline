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

float SampleShadowMap_Depth(float2 uv, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, uv, 0).r;
    return depth;
}

float SampleShadowMap_DepthCompare(float3 positionSS, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, positionSS.xy, 0).r;
    return step(depth, positionSS.z);
}

float SampleShadowArray_Compare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_ARRAY_SHADOW(shadowMap, shadowMapSampler, positionSS, elementIndex);
    return shadowAttenuation;
}

float SampleShadowArray_Depth(float2 uv, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(shadowMap, shadowMapSampler, uv, elementIndex, 0).r;
    return depth;
}

float SampleShadowArray_DepthCompare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(shadowMap, shadowMapSampler, positionSS.xy, elementIndex, 0).r;
    return step(depth, positionSS.z);
}

float SampleShadowCubeArray_Compare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURECUBE_ARRAY_SHADOW(shadowMap, shadowMapSampler, float4(sampleDir, z), elementIndex);
    return shadowAttenuation;
}

float SampleShadowCubeArray_Depth(float3 sampleDir, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY_LOD(shadowMap, shadowMapSampler, sampleDir, elementIndex, 0).r;
    return depth;
}

float SampleShadowCubeArray_DepthCompare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY_LOD(shadowMap, shadowMapSampler, sampleDir, elementIndex, 0).r;
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
float3 ApplyShadowBias(float3 positionWS, float4 shadowBias, float texelSize, float penumbraWS, float3 normalWS, float3 L)
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 50.0); // maxBias

    float3 depthBias = (texelSize + penumbraWS) * shadowBias.x * L;
    float3 scaledDepthBias = (texelSize + penumbraWS) * tanTheta * shadowBias.y * L;
    float3 normalBias = (texelSize + penumbraWS) * shadowBias.z * normalWS;
    float3 scaledNormalBias = (texelSize + penumbraWS) * sinTheta * shadowBias.w * normalWS;

    // float3 depthBias = texelSize * (1.0 + penumbraTexel) * shadowBias.x * L;
    // float3 scaledDepthBias = texelSize * (1.0 + penumbraTexel) * tanTheta * shadowBias.y * L;
    // float3 normalBias = texelSize * (1.0 + penumbraTexel) * shadowBias.z * normalWS;
    // float3 scaledNormalBias = texelSize * (1.0 + penumbraTexel) * sinTheta * shadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// ----------------------------------------------------------------------------------------------------
// PCF Related Functions
// ----------------------------------------------------------------------------------------------------

//index could be shadowingLightIndex or cascadeIndex
float ApplyPCF_2DArray(float index, TEXTURE2D_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionWS, float3 positionSS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_2DArray(float index, TEXTURE2D_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionSS, uint hash1, uint hash2, float2x2 rotation)
{
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionWS, float3 positionSS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        shadowAttenuation += SampleShadowCubeArray_Compare(sampleDir, positionSS.z, index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionSS, uint hash1, uint hash2, float2x2 rotation)
{
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        shadowAttenuation += SampleShadowCubeArray_Compare(sampleDir, positionSS.z, index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCF
// ----------------------------------------------------------------------------------------------------

float GetSunLightShadowAttenuation_PCF(float2 lightMapUV, float3 positionWS, float3 normalWS, float3 L)
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
        float penumbraPercent = GetSunLightShadowPenumbraWidth() / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;
        
        float3 positionWS_Bias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, GetSunLightShadowPenumbraWidth(), normalWS, L);
        float3 positionSS = TransformWorldToSunLightShadowCoord(positionWS_Bias, cascadeIndex);
        float shadowAttenuation = ApplyPCF_2DArray(cascadeIndex, SUN_LIGHT_SHADOW_MAP, GetSunLightShadowSampleNumber(), penumbraPercent, positionWS, positionSS);

        #if defined(_SHADOW_MASK_DISTANCE)
            return lerp(1.0, MixBakedAndRealtimeShadows(lightMapUV, GetSunLightShadowMaskChannel(), shadowAttenuation, shadowFade), shadowStrength);
        #elif defined(_SHADOW_MASK_NORMAL)
            return lerp(1.0, ChooseBakedAndRealtimeShadows(lightMapUV, GetSunLightShadowMaskChannel(), shadowAttenuation, shadowFade), shadowStrength);
        #else
            return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
        #endif
    }
}

float GetSpotLightShadowAttenuation_PCF(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetSpotLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingSpotLightIndex = GetShadowingSpotLightIndex(lightIndex);
    //float linearDepth = mul(GetSpotLightShadowMatrix(shadowingSpotLightIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth / GetPunctualLightShadowArraySize();
    float penumbraPercent = GetPunctualLightShadowPenumbraWidth() / 4.0;
    float penumbraWS = penumbraPercent * 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, penumbraWS, normalWS, L);
    float3 positionSS = TransformWorldToSpotLightShadowCoord(positionWS_Bias, shadowingSpotLightIndex);
    float shadowAttenuation = ApplyPCF_2DArray(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, GetPunctualLightShadowSampleNumber(), penumbraPercent, positionWS, positionSS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

float GetPointLightShadowAttenuation_PCF(int lightIndex, float faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetPointLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingPointLightIndex = GetShadowingPointLightIndex(lightIndex);
    //float linearDepth = mul(GetPointLightShadowMatrix(shadowingPointLightIndex * 6 + faceIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * linearDepth / GetPunctualLightShadowArraySize();
    float penumbraPercent = GetPunctualLightShadowPenumbraWidth() / 4.0;
    float penumbraWS = penumbraPercent * 2.0 * linearDepth;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, penumbraWS, normalWS, L);
    //float3 sampleDir = normalize(positionWS_Bias - GetPointLightPosition(lightIndex));
    float3 positionSS = TransformWorldToPointLightShadowCoord(positionWS_Bias, shadowingPointLightIndex, faceIndex);
    float shadowAttenuation = ApplyPCF_CubeArray(shadowingPointLightIndex, faceIndex, POINT_LIGHT_SHADOW_MAP, GetPunctualLightShadowSampleNumber(), penumbraPercent, positionWS, positionSS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

// ----------------------------------------------------------------------------------------------------
// PCSS Related Functions
// ----------------------------------------------------------------------------------------------------

float NonLinearToLinearDepth_Ortho(float4 depthParams, float nonLinearDepth)
{
    return (depthParams.y - 2.0 * nonLinearDepth + 1.0) / depthParams.x;
}

float NonLinearToLinearDepth_Persp(float4 depthParams, float nonLinearDepth)
{
    return depthParams.y / (2.0 * nonLinearDepth - 1.0 + depthParams.x);
}

float3 ComputeAverageBlockerDepth_2DArray_Ortho(float index, TEXTURE2D_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Ortho(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv = positionSS.xy + offset;
        float d_Blocker = SampleShadowArray_Depth(uv, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Ortho(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_2DArray_Persp(float index, TEXTURE2D_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv = positionSS.xy + offset;
        float d_Blocker = SampleShadowArray_Depth(uv, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        float d_Blocker = SampleShadowCubeArray_Depth(sampleDir, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCSS
// ----------------------------------------------------------------------------------------------------

float GetSunLightShadowAttenuation_PCSS(float3 positionWS, float3 normalWS, float3 L)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    if (cascadeIndex >= GetSunLightCascadeCount()) return 1.0;
    float shadowStrength = GetSunLightShadowStrength();
    float shadowFade = 1.0;
    shadowFade *= ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    shadowFade *= ComputeCascadeEdgeFade(cascadeIndex, GetSunLightCascadeCount(), positionWS, GetCascadeEdgeFade(), GetCascadeCullingSphere(GetSunLightCascadeCount() - 1));

    float texelSize = GetCascadeCullingSphereRadius(cascadeIndex) * 2.0 / GetSunLightShadowArraySize();
    // float searchWidthWS = GetSunLightSize();
    float searchWidthWS = GetSunLightSize();
    float searchWidthPercent = searchWidthWS / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;

    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, searchWidthWS, normalWS, L);
    float3 positionSS_Search = TransformWorldToSunLightShadowCoord(positionWS_SearchBias, cascadeIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));

    float4 depthParams = GetSunLightDepthParams(cascadeIndex);
    float blockerSampleNumber = GetSunLightBlockerSampleNumber();
    
    float3 blocker = ComputeAverageBlockerDepth_2DArray_Ortho(cascadeIndex, SUN_LIGHT_SHADOW_MAP, blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;

    //float penumbraWS = GetSunLightPenumbraScale() * (blocker.z - ald_Blocker) * 0.01;
    float penumbraWS = GetSunLightPenumbraScale() * GetSunLightSize() * (blocker.z - ald_Blocker) * 0.1;
    float penumbraPercent = penumbraWS / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;
    
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, penumbraWS, normalWS, L);
    float3 positionSS_Filter = TransformWorldToSunLightShadowCoord(positionWS_FilterBias, cascadeIndex);
    float filterSampleNumber = GetSunLightFilterSampleNumber();
    float shadowAttenuation = ApplyPCF_2DArray(cascadeIndex, SUN_LIGHT_SHADOW_MAP, filterSampleNumber, penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
}

float GetSpotLightShadowAttenuation_PCSS(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetSpotLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingSpotLightIndex = GetShadowingSpotLightIndex(lightIndex);
    float texelSize = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth / GetPunctualLightShadowArraySize();
    float searchWidthWS = GetSpotLightSize(shadowingSpotLightIndex);
    float searchWidthPercent = searchWidthWS / (2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth);
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, searchWidthWS, normalWS, L);
    float3 positionSS_Search = TransformWorldToSpotLightShadowCoord(positionWS_SearchBias, shadowingSpotLightIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float4 depthParams = GetSpotLightDepthParams(shadowingSpotLightIndex);
    float blockerSampleNumber = GetSpotLightBlockerSampleNumber(shadowingSpotLightIndex);
    float3 blocker = ComputeAverageBlockerDepth_2DArray_Persp(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;

    float penumbraWS = GetSpotLightPenumbraScale(shadowingSpotLightIndex) * GetSpotLightSize(shadowingSpotLightIndex) * (linearDepth - ald_Blocker) / ald_Blocker;
    float penumbraPercent = penumbraWS / (2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth);
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, penumbraWS, normalWS, L);
    float3 positionSS_Filter = TransformWorldToSpotLightShadowCoord(positionWS_FilterBias, shadowingSpotLightIndex);
    float filterSampleNumber = GetSpotLightFilterSampleNumber(shadowingSpotLightIndex);
    float shadowAttenuation = ApplyPCF_2DArray(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, filterSampleNumber, penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

float GetPointLightShadowAttenuation_PCSS(int lightIndex, float faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth)
{
    float shadowStrength = GetPointLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingPointLightIndex = GetShadowingPointLightIndex(lightIndex);
    float texelSize = 2.0 * linearDepth / GetPunctualLightShadowArraySize();
    float searchWidthWS = GetPointLightSize(shadowingPointLightIndex);
    float searchWidthPercent = searchWidthWS / (2.0 * linearDepth);
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, searchWidthWS, normalWS, L);
    //float3 sampleDir_Search = normalize(positionWS_SearchBias - GetPointLightPosition(lightIndex));
    float3 positionSS_Search = TransformWorldToPointLightShadowCoord(positionWS_SearchBias, shadowingPointLightIndex, faceIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    float random = floatConstruct(hash1);
    float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float4 depthParams = GetPointLightDepthParams(shadowingPointLightIndex);
    float blockerSampleNumber = GetPointLightBlockerSampleNumber(shadowingPointLightIndex);
    float3 blocker = ComputeAverageBlockerDepth_CubeArray(shadowingPointLightIndex,faceIndex, POINT_LIGHT_SHADOW_MAP,
        blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;
    
    float penumbraWS = GetPointLightPenumbraScale(shadowingPointLightIndex) * GetPointLightSize(shadowingPointLightIndex) * (linearDepth - ald_Blocker) / ald_Blocker;
    float penumbraPercent = penumbraWS / (2.0 * linearDepth);
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, penumbraWS, normalWS, L);
    //float3 sampleDir_Filter = normalize(positionWS_FilterBias - GetPointLightPosition(lightIndex));
    float3 positionSS_Filter = TransformWorldToPointLightShadowCoord(positionWS_FilterBias, shadowingPointLightIndex, faceIndex);
    float filterSampleNumber = GetPointLightFilterSampleNumber(shadowingPointLightIndex);
    float shadowAttenuation = ApplyPCF_CubeArray(shadowingPointLightIndex, faceIndex, POINT_LIGHT_SHADOW_MAP, filterSampleNumber,
        penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

#endif