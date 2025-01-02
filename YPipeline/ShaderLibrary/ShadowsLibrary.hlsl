#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "Core/YPipelineCore.hlsl"
#include "../ShaderLibrary/RandomLibrary.hlsl"
#include "../ShaderLibrary/SamplingLibrary.hlsl"

// --------------------------------------------------------------------------------
// Sample Shadow Atlas or Array
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

// --------------------------------------------------------------------------------
// Light/Shadow Space Transform
float3 TransformWorldToTiledShadowCoord(float3 positionWS, int tileIndex)
{
    float3 positionTSS = mul(_DirectionalShadowMatrices[tileIndex], float4(positionWS, 1.0)).xyz;
    return positionTSS;
}

// --------------------------------------------------------------------------------
// Shadow Atlas Related Functions
float ComputeCascadeIndex(float3 positionWS)
{
    float3 vector0 = positionWS - _CascadeCullingSpheres[0].xyz;
    float3 vector1 = positionWS - _CascadeCullingSpheres[1].xyz;
    float3 vector2 = positionWS - _CascadeCullingSpheres[2].xyz;
    float3 vector3 = positionWS - _CascadeCullingSpheres[3].xyz;
    float4 distanceSquare = float4(dot(vector0, vector0), dot(vector1, vector1), dot(vector2, vector2), dot(vector3, vector3));
    float4 radiusSquare = float4(_CascadeCullingSpheres[0].w, _CascadeCullingSpheres[1].w, _CascadeCullingSpheres[2].w, _CascadeCullingSpheres[3].w);
    
    float4 indexes = float4(distanceSquare < radiusSquare);
    indexes.yzw = saturate(indexes.yzw - indexes.xyz);
    return 4.0 - dot(indexes, float4(4.0, 3.0, 2.0, 1.0));
}

// --------------------------------------------------------------------------------
// Shadow Bias
float3 ApplyShadowBias(float3 positionWS, float cascadeIndex, float3 normalWS, float3 L) //normalWS must be normalized
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 50.0); // maxBias
    float texelSize = sqrt(_CascadeCullingSpheres[cascadeIndex].w) * 2 / _CascadeParams.y;

    float3 depthBias = texelSize * _ShadowBias.x * L;
    float3 scaledDepthBias = texelSize * tanTheta * _ShadowBias.y * L;
    float3 normalBias = texelSize * _ShadowBias.z * normalWS;
    float3 scaledNormalBias = texelSize * sinTheta * _ShadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// --------------------------------------------------------------------------------
// Shadow Attenuation Functions
float GetDirShadowFalloff_Atlas(int dirLightIndex, float3 positionWS, float3 normalWS, float3 L)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    float shadowStrength = _DirectionalLightShadowData[dirLightIndex].x;
    shadowStrength *= 1 - step(_CascadeParams.x, cascadeIndex);

    // Distance Fade
    float depth = -TransformWorldToView(positionWS).z;
    float distanceFade = saturate((1 - depth * _ShadowDistanceFade.x) * _ShadowDistanceFade.y);
    shadowStrength *= distanceFade;

    // Cascade Fade
    float isInLastSphere = cascadeIndex == _CascadeParams.x - 1;
    float3 distanceVector = positionWS - _CascadeCullingSpheres[_CascadeParams.x - 1].xyz;
    float distanceSquare = dot(distanceVector, distanceVector);
    float cascadeFade = saturate((1 - distanceSquare * 1.0 / _CascadeCullingSpheres[_CascadeParams.x - 1].w) * _ShadowDistanceFade.z);
    shadowStrength *= lerp(1, cascadeFade, isInLastSphere);
    
    float tileIndex = _DirectionalLightShadowData[dirLightIndex].y + cascadeIndex;
    float3 positionTSS = TransformWorldToTiledShadowCoord(ApplyShadowBias(positionWS, cascadeIndex, normalWS, L), tileIndex);
    //float shadowAttenuation = SampleShadowMap_Compare(positionTSS, _DirectionalShadowMap, sampler_point_clamp_compare_DirectionalShadowMap);

    // ..............................................................................
    // try

    float random = Random_NoSine(positionTSS.xy);
    float randomRadian = random * TWO_PI - PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));

    float sampleNumber = 32.0;
    float sample = 0.0;

    [unroll]
    for (float i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, poissonDisk16[i]) / 8192 * 3;
        float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i, sampleNumber))) / 8192 * 4;
        float2 uv = positionTSS.xy + offset;
        sample += _DirectionalShadowMap.SampleCmpLevelZero(sampler_linear_clamp_compare_DirectionalShadowMap, uv, positionTSS.z);
    }
    
    return lerp(1.0, sample * 1 / sampleNumber, shadowStrength);
}


#endif