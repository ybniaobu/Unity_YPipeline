#ifndef YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED

#include "../ShaderLibrary/ShadowsLibrary.hlsl"

// --------------------------------------------------------------------------------
// Light Falloff / Attenuation functions
float GetDistanceFalloff(float3 lightVector, float invLightRangeSqr) // lightVector is unnormalized light direction(L).
{
    float distanceSquare = dot(lightVector, lightVector);
    float factor = distanceSquare * invLightRangeSqr;
    float smoothFactor = max(1.0 - factor * factor, 0.0);
    return (smoothFactor * smoothFactor) / max(distanceSquare, 1e-4);
}

float GetAngleFalloff(float3 L, float3 spotDirection, float2 spotAngleFalloffParams) // only for spot lights
{
    float SdotL = dot(spotDirection, L);
    float attenuation = saturate(SdotL * spotAngleFalloffParams.x + spotAngleFalloffParams.y);
    return attenuation * attenuation;
}

// --------------------------------------------------------------------------------
// Light Parameters for computing the BRDF Data and the Rendering Equation
struct LightParams
{
    float3 color;
    float4 positionWS;
    float3 L;
    float3 H;
    float distanceAttenuation;
    float angleAttenuation;
    float shadowAttenuation;
    //uint layerMask;
};

float3 CalculateLightIrradiance(LightParams lightParams)
{
    float3 irradiance = lightParams.color * lightParams.shadowAttenuation * lightParams.distanceAttenuation * lightParams.angleAttenuation;
    return irradiance;
}

// --------------------------------------------------------------------------------
// Initialize Directional Light Parameters
void InitializeSunLightParams(out LightParams sunLightParams, float2 lightMapUV, float3 V, float3 normalWS, float3 positionWS)
{
    sunLightParams.color = _SunLightColor.rgb;
    sunLightParams.positionWS = _SunLightDirection; //Directional Light has no position
    sunLightParams.L = normalize(sunLightParams.positionWS.xyz);
    sunLightParams.H = normalize(sunLightParams.L + V);
    sunLightParams.distanceAttenuation = 1.0;
    sunLightParams.angleAttenuation = 1.0;
    sunLightParams.shadowAttenuation = GetSunLightShadowFalloff(lightMapUV, positionWS, normalWS, sunLightParams.L);
    //directionalLightParams.layerMask = _DirectionalLightLayerMask;
}

// --------------------------------------------------------------------------------
// Initialize Additional Light Parameters
// int GetAdditionalLightCount()
// {
//     return int(min(_AdditionalLightsCount.x, unity_LightData.y));
// }
//
// int GetAdditionalLightIndex(uint loopIndex)
// {
//     float4 indices = unity_LightIndices[loopIndex / 4];
//     return int(indices[loopIndex % 4]);
// }
//
//
// void InitializeAdditionalLightParams(out LightParams additionalLightParams, int lightIndex, float3 V, float3 positionWS)
// {
//     additionalLightParams.color = _AdditionalLightsColor[lightIndex].rgb;
//     additionalLightParams.positionWS = _AdditionalLightsPosition[lightIndex];
//     
//     float3 lightVector = additionalLightParams.positionWS.xyz - positionWS * additionalLightParams.positionWS.w;
//     additionalLightParams.L = normalize(lightVector);
//     additionalLightParams.H = normalize(additionalLightParams.L + V);
//
//     float4 attenuationParams = _AdditionalLightsAttenuation[lightIndex];
//     additionalLightParams.distanceAttenuation = GetDistanceFalloff(lightVector, attenuationParams.x);
//     float3 spotDirection = _AdditionalLightsSpotDir[lightIndex].xyz;
//     additionalLightParams.angleAttenuation = GetAngleFalloff(additionalLightParams.L, spotDirection, attenuationParams.zw);
//
//     additionalLightParams.shadowAttenuation = 1.0;
//     additionalLightParams.layerMask = asuint(_AdditionalLightsLayerMasks[lightIndex]);
// }



#endif