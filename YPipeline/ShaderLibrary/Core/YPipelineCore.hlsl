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

float3 CubeMapping(int faceID, float2 uv)
{
    float3 dir = 0;

    switch (faceID)
    {
    case 0: //+X
        dir.x = 1.0;
        dir.y = uv.y * 2.0f - 1.0f;
        dir.z = uv.x * -2.0f + 1.0f;
        break;

    case 1: //-X
        dir.x = -1.0;
        dir.yz = uv.yx * 2.0f - 1.0f;
        break;

    case 2: //+Y
        dir.x = uv.x * 2.0f - 1.0f;
        dir.z = uv.y * -2.0f + 1.0f;
        dir.y = 1.0f;
        break;
        
    case 3: //-Y
        dir.xz = uv * 2.0f - 1.0f;
        dir.y = -1.0f;
        break;

    case 4: //+Z
        dir.xy = uv * 2.0f - 1.0f;
        dir.z = 1;
        break;

    case 5: //-Z
        dir.x = uv.x * -2.0f + 1.0f;
        dir.y = uv.y * 2.0f - 1.0f;
        dir.z = -1;
        break;
    }
    return normalize(dir);
}

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