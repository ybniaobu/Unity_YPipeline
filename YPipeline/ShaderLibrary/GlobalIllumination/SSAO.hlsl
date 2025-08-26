#ifndef YPIPELINE_SSAO_INCLUDED
#define YPIPELINE_SSAO_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float SampleLinearDepth(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_LinearClamp, screenUV, 0).r;
}

inline float3 LoadAndDecodeNormal(float2 pixelCoord)
{
    #ifdef _HALF_RESOLUTION
        float3 packedNormalWS = LOAD_TEXTURE2D_LOD(_ThinGBuffer, pixelCoord * 2, 0).rgb;
    #else
        float3 packedNormalWS = LOAD_TEXTURE2D_LOD(_ThinGBuffer, pixelCoord, 0).rgb;
    #endif
    
    return DecodeNormalFrom888(packedNormalWS);
}

// ----------------------------------------------------------------------------------------------------
// Generate Hemisphere Samples
// ----------------------------------------------------------------------------------------------------

// Left-handed Spherical and Cartesian Coordinate, Coordinate Convention Detail see SamplingLibrary.hlsl

inline float3 GenerateHemisphereSamples(float2 xi, float scale)
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = scale * scale; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}

inline float3 GenerateCosineWeightedHemisphereSamples(float2 xi, float scale)
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt(1 - xi.y);
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = scale * scale; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}




#endif