#ifndef YPIPELINE_PUNCTUAL_LIGHTS_LIBRARY_INCLUDED
#define YPIPELINE_PUNCTUAL_LIGHTS_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl" 

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
struct LightParams //Sunlight
{
    float3 color;
    float4 positionWS;
    float3 L;
    float3 H;
    float distanceAttenuation;
    float angleAttenuation;
    float shadowAttenuation;
    uint layerMask;
};

// --------------------------------------------------------------------------------
// Initialize Main Light Parameters
void InitializeMainLightParams(out LightParams mainLightParams, float3 V)
{
    mainLightParams.color = _MainLightColor.rgb;
    mainLightParams.positionWS = _MainLightPosition;
    mainLightParams.L = normalize(mainLightParams.positionWS.xyz);
    mainLightParams.H = normalize(mainLightParams.L + V);
    mainLightParams.distanceAttenuation = 1.0;
    mainLightParams.angleAttenuation = 1.0;
    mainLightParams.shadowAttenuation = 1.0;
    mainLightParams.layerMask = _MainLightLayerMask;
}

// --------------------------------------------------------------------------------
// Initialize Additional Light Parameters
int GetAdditionalLightCount()
{
    return int(min(_AdditionalLightsCount.x, unity_LightData.y));
}

int GetAdditionalLightIndex(uint loopIndex)
{
    float4 indices = unity_LightIndices[loopIndex / 4];
    return int(indices[loopIndex % 4]);
}


void InitializeAdditionalLightParams(out LightParams additionalLightParams, int lightIndex, float3 V, float3 positionWS)
{
    additionalLightParams.color = _AdditionalLightsColor[lightIndex].rgb;
    additionalLightParams.positionWS = _AdditionalLightsPosition[lightIndex];
    
    float3 lightVector = additionalLightParams.positionWS.xyz - positionWS * additionalLightParams.positionWS.w;
    additionalLightParams.L = normalize(lightVector);
    additionalLightParams.H = normalize(additionalLightParams.L + V);

    float4 attenuationParams = _AdditionalLightsAttenuation[lightIndex];
    additionalLightParams.distanceAttenuation = GetDistanceFalloff(lightVector, attenuationParams.x);
    float3 spotDirection = _AdditionalLightsSpotDir[lightIndex].xyz;
    additionalLightParams.angleAttenuation = GetAngleFalloff(additionalLightParams.L, spotDirection, attenuationParams.zw);

    additionalLightParams.shadowAttenuation = 1.0;
    additionalLightParams.layerMask = asuint(_AdditionalLightsLayerMasks[lightIndex]);
}



#endif