#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "Core/YPipelineCore.hlsl"

float3 TransformWorldToTiledShadowCoord(float3 positionWS, int tileIndex)
{
    float3 positionTSS = mul(_DirectionalShadowMatrices[tileIndex], float4(positionWS, 1.0)).xyz;
    return positionTSS;
}

float SampleShadowmap(float3 positionTSS)
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowMap, sampler_point_clamp_compare_DirectionalShadowMap, positionTSS);
    
    return shadowAttenuation;
}

float GetDirShadowFalloff(int dirLightIndex, float3 positionWS)
{
    int cascadeIndex;

    for (cascadeIndex = 0; cascadeIndex < _CascadeCount; cascadeIndex++)
    {
        float4 sphere = _CascadeCullingSpheres[cascadeIndex];
        float distanceSqr = dot(positionWS - sphere.xyz, positionWS - sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            break;
        }
    }
    
    float shadowStrength = _DirectionalLightShadowData[dirLightIndex].x;
    if (cascadeIndex == _CascadeCount) shadowStrength = 0;
    int depth = -TransformWorldToView(positionWS).z;
    //if (depth > _ShadowDistance) shadowStrength = 0;

    
    float tileIndex = _DirectionalLightShadowData[dirLightIndex].y + cascadeIndex;
    float3 positionTSS = TransformWorldToTiledShadowCoord(positionWS, tileIndex);
    float shadowAttenuation = SampleShadowmap(positionTSS);
    
    return lerp(1.0, shadowAttenuation, shadowStrength);
}


#endif