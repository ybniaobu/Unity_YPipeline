#ifndef YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED

#include "../ShaderLibrary/ShadowsLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Light Falloff / Attenuation functions
// ----------------------------------------------------------------------------------------------------

float GetDistanceAttenuation(float3 lightVector, float invLightRadiusSqr) // lightVector is unnormalized light direction(L).
{
    float distanceSquare = dot(lightVector, lightVector);
    float factor = distanceSquare * invLightRadiusSqr;
    float smoothFactor = saturate(1.0 - factor * factor);
    return (smoothFactor * smoothFactor) / max(distanceSquare, 1e-4);
}

float GetAngleAttenuation(float3 L, float3 spotDirection, float2 spotAngleParams) // only for spot lights
{
    float SdotL = dot(spotDirection, L);
    float attenuation = saturate(SdotL * spotAngleParams.x - spotAngleParams.y * spotAngleParams.x);
    return attenuation * attenuation;
}

// ----------------------------------------------------------------------------------------------------
// Light Parameters for computing the BRDF Data and the Rendering Equation
// ----------------------------------------------------------------------------------------------------

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

// ----------------------------------------------------------------------------------------------------
// Initialize Directional Light Parameters
// ----------------------------------------------------------------------------------------------------

void InitializeSunLightParams(out LightParams sunLightParams, float2 lightMapUV, float3 V, float3 normalWS, float3 positionWS)
{
    sunLightParams.color = GetSunLightColor();
    sunLightParams.positionWS = float4(GetSunLightDirection(), 0.0); //Directional Light has no position
    sunLightParams.L = normalize(sunLightParams.positionWS.xyz);
    sunLightParams.H = normalize(sunLightParams.L + V);
    sunLightParams.distanceAttenuation = 1.0;
    sunLightParams.angleAttenuation = 1.0;
    sunLightParams.shadowAttenuation = GetSunLightShadowAttenuation(lightMapUV, positionWS, normalWS, sunLightParams.L);
}

// ----------------------------------------------------------------------------------------------------
// Initialize Punctual Lights Parameters
// ----------------------------------------------------------------------------------------------------

void InitializeSpotLightParams(out LightParams spotLightParams, int lightIndex, float2 lightMapUV, float3 V, float3 normalWS, float3 positionWS)
{
    spotLightParams.color = GetSpotLightColor(lightIndex);
    spotLightParams.positionWS = float4(GetSpotLightPosition(lightIndex), 1.0);
    
    float3 lightVector = spotLightParams.positionWS.xyz - positionWS;
    spotLightParams.L = normalize(lightVector);
    spotLightParams.H = normalize(spotLightParams.L + V);
    
    spotLightParams.distanceAttenuation = GetDistanceAttenuation(lightVector, GetSpotLightInverseRadiusSquare(lightIndex));
    float3 spotDirection = GetSpotLightDirection(lightIndex);
    float2 spotAngleParams = GetSpotLightAngleParams(lightIndex);
    spotLightParams.angleAttenuation = GetAngleAttenuation(spotLightParams.L, spotDirection, spotAngleParams);
    
    [branch]
    if (spotLightParams.distanceAttenuation * spotLightParams.angleAttenuation <= 0.0 || GetShadowingSpotLightIndex(lightIndex) < 0.0)
    {
        spotLightParams.shadowAttenuation = 1.0;
    }
    else
    {
        spotLightParams.shadowAttenuation = GetSpotLightShadowAttenuation(lightIndex, lightMapUV, positionWS, normalWS, spotLightParams.L);
    }
}



#endif