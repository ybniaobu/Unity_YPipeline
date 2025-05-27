#ifndef YPIPELINE_SPACE_TRANSFORMS_INCLUDED
#define YPIPELINE_SPACE_TRANSFORMS_INCLUDED

float3 GetWorldSpaceNormalizedViewDir(float3 positionWS)
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

float GetViewDepthFromSVPosition(float4 positionHCS)
{
    if (unity_OrthoParams.w < 0.5f) // Perspective
    {
        return positionHCS.w;
    }
    else // Orthographic
    {
        float normalizedDepth = positionHCS.z;

        #if UNITY_REVERSED_Z
        normalizedDepth = 1.0 - normalizedDepth;
        #endif
        return (_ProjectionParams.z - _ProjectionParams.y) * normalizedDepth + _ProjectionParams.y;
    }
}

float GetViewDepthFromDepthTexture(float sampledDepth)
{
    if (unity_OrthoParams.w < 0.5f) // Perspective
    {
        return LinearEyeDepth(sampledDepth, _ZBufferParams);
    }
    else // Orthographic
    {
        #if UNITY_REVERSED_Z
        sampledDepth = 1.0 - sampledDepth;
        #endif
        return (_ProjectionParams.z - _ProjectionParams.y) * sampledDepth + _ProjectionParams.y;
    }
}

#endif