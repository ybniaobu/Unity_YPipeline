#ifndef YPIPELINE_AMBIENT_OCCLUSION_LIBRARY_INCLUDED
#define YPIPELINE_AMBIENT_OCCLUSION_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Ambient Occlusion
// ----------------------------------------------------------------------------------------------------

// From SIGGRAPH 2014, Moving Frostbite to Physically Based Rendering, Chapter 4.10 Shadow and occlusion
// https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/course-notes-moving-frostbite-to-pbr-v32.pdf
float ComputeSpecularAO(float NoV, float ao, float roughness)
{
    return saturate(pow(abs(NoV + ao), exp2(-16.0 * roughness - 1.0)) - 1.0 + ao);
}

// Could also apply to direct lighting
float ComputeHorizonSpecularOcclusion(float3 R, float3 N)
{
    float horizon = saturate(1.0 + dot(R, N));
    return horizon * horizon;
}

#endif