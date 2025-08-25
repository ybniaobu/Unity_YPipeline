#ifndef YPIPELINE_SSAO_INCLUDED
#define YPIPELINE_SSAO_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Generate Hemisphere Samples
// ----------------------------------------------------------------------------------------------------

// Left-handed Spherical and Cartesian Coordinate, Coordinate Convention Detail see SamplingLibrary.hlsl

float3 GenerateHemisphereSamples(float3 xi)
{
    float phi = PI * (2.0 * xi.y - 1.0);
    float cosTheta = 1 - xi.z;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = xi.x * xi.x; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}

float3 GenerateCosineWeightedHemisphereSamples(float3 xi)
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt(1 - xi.y);
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    
    float r = xi.z * xi.z; // distribute more samples closer to the hemisphere origin
    return float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
}




#endif