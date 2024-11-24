#ifndef YPIPELINE_SAMPLING_LIBRARY_INCLUDED
#define YPIPELINE_SAMPLING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// --------------------------------------------------------------------------------
// Low-discrepancy sequence
float RadicalInverseVdC(uint bits) 
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10; // / 0x100000000
}

float2 Hammersley(uint index, uint sampleNumber)
{
    return float2(float(index) / float(sampleNumber), RadicalInverseVdC(index));
}

// --------------------------------------------------------------------------------
// Coordinate conversion

// Convention: Cartesian coordinate and Spherical coordinate are both left-handed.
// the order of Spherical coordinate is r, theta, phi.
// phi: angle of rotation from x in xz plane, the range of phi is [-π, π];
// theta: angle of rotation with respect to y-axis, the range of theta is [0, π];
// phi can be derived from atan2(z/x), the range of atan2() is also [-π, π];
// theta can be derived from acos(y), the range of acos() is also [0, π];
float3 SphericalCoordToCartesianCoord(float theta, float phi)
{
    return float3(sin(theta) * cos(phi), cos(theta), sin(theta) * sin(phi));
}

float3 SphericalCoordToCartesianCoord(float2 sphericalCoord)
{
    return SphericalCoordToCartesianCoord(sphericalCoord.x, sphericalCoord.y);
}

float2 CartesianCoordToSphericalCoord(float x, float y, float z)
{
    return float2(acos(y), atan2(z, x));
}

float2 CartesianCoordToSphericalCoord(float3 cartesianCoord)
{
    return CartesianCoordToSphericalCoord(cartesianCoord.x, cartesianCoord.y, cartesianCoord.z);
}

float3 TangentCoordToWorldCoord(float3 tangentCoord, float3 N)
{
    float3 up = abs(N.y) > 0.9999 ? float3(0, 0, 1) : float3(0, 1, 0);
    float3 tangent = normalize(cross(up, N));
    float3 binormal = normalize(cross(tangent, N));
    return normalize(tangent * tangentCoord.x + normalize(N) * tangentCoord.y + binormal * tangentCoord.z);
}

// --------------------------------------------------------------------------------
// Environment Mapping
float3 LatLongUVToCartesian(float2 uv)
{
    float phi = (uv.x - 0.5f) * TWO_PI;
    float theta = uv.y * PI;
    return SphericalCoordToCartesianCoord(theta, phi);
}

float2 CartesianToLatLongUV(float3 cartesianCoord)
{
    cartesianCoord = normalize(cartesianCoord);
    float2 sphericalCoord = CartesianCoordToSphericalCoord(cartesianCoord);
    return float2(sphericalCoord.y * INV_TWO_PI + 0.5, sphericalCoord.x * INV_PI);
}

// --------------------------------------------------------------------------------
// Inverse transform sampling
float4 InverseSampleSphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - 2 * xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = INV_FOUR_PI;
    return float4(N, PDF);
}

float4 InverseSampleHemisphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = INV_TWO_PI;
    return float4(N, PDF);
}

// --------------------------------------------------------------------------------
// Cosine-weighted inverse transform sampling
float4 CosineSampleHemisphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt(1 - xi.y);
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = cosTheta * INV_PI;
    return float4(N, PDF);
}

// --------------------------------------------------------------------------------
// Importance sampling
float4 ImportanceSampleGGX(float2 xi, float roughness) // Left-handed Spherical and Cartesian Coordinate
{
    float a = roughness * roughness;
    float a2 = a * a;
    
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt((1 - xi.y) / (1 + (a2 - 1) * xi.y));
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 H = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));

    float d = (cosTheta * a2 - cosTheta) * cosTheta + 1;
    float D = a2 / (PI * d * d);
    float PDF = D * cosTheta;
    
    return float4(H, PDF);
}

float3 ImportanceSampleGGX(float2 xi, float roughness, float3 N) // Left-handed Spherical and Cartesian Coordinate
{
    float a = roughness * roughness;
    float a2 = a * a;
    
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt((1 - xi.y) / (1 + (a2 - 1) * xi.y));
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 H = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    
    return TangentCoordToWorldCoord(H, N);
}

#endif