#ifndef YPIPELINE_CORE_INCLUDED
#define YPIPELINE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "UnityInput.hlsl"
#include "YPipelineInput.hlsl"

#if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK_NORMAL)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// ----------------------------------------------------------------------------------------------------
// Cubemap Constants or Functions
// ----------------------------------------------------------------------------------------------------
static const float3 k_CubeMapFaceDir[6] =
{
    float3(1.0, 0.0, 0.0),
    float3(-1.0, 0.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 0.0, 1.0),
    float3(0.0, 0.0, -1.0)
};

// ----------------------------------------------------------------------------------------------------
// Space Transform Functions
// ----------------------------------------------------------------------------------------------------

float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (unity_OrthoParams.w < 0.5f) // Perspective
    {
        return normalize(_WorldSpaceCameraPos - positionWS);
    }
    else // Orthographic
    {
        return UNITY_MATRIX_V[2].xyz;
    }
}



#endif