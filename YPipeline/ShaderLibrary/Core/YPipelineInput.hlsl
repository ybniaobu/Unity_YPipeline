#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Macros
// ----------------------------------------------------------------------------------------------------

#define MAX_DIRECTIONAL_LIGHT_COUNT         1 // Only Support One Directional Light - Sunlight
#define MAX_CASCADE_COUNT                   4
#define MAX_SPOT_LIGHT_COUNT                64
#define MAX_SHADOWING_SPOT_LIGHT_COUNT      32
#define MAX_POINT_LIGHT_COUNT               32
#define MAX_SHADOWING_POINT_LIGHT_COUNT     8

// ----------------------------------------------------------------------------------------------------
// Constant Buffers
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(LightParamsPerSetting)
    float4 _CascadeSettings; // x: max shadow distance, y: shadow distance fade, z: sun light cascade count, w: cascade edge fade
    float4 _ShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SunLightShadowSettings; // x: shadow map size, y: sample number, z: penumbra width, w: 0
    float4 _PunctualLightShadowSettings; // x: shadow map size, y: sample number, z: penumbra width, w: 0
CBUFFER_END

float GetMaxShadowDistance()                        { return _CascadeSettings.x; }
float GetShadowDistanceFade()                       { return _CascadeSettings.y; }
float GetSunLightCascadeCount()                     { return _CascadeSettings.z; }
float GetCascadeEdgeFade()                          { return _CascadeSettings.w; }
float4 GetShadowBias()                              { return _ShadowBias; }
float4 GetSunLightShadowSettings()                  { return _SunLightShadowSettings; }
float GetSunLightShadowArraySize()                  { return _SunLightShadowSettings.x; }
float GetSunLightShadowSampleNumber()               { return _SunLightShadowSettings.y; }
float GetSunLightShadowPenumbraWidth()              { return _SunLightShadowSettings.z; }
float4 GetPunctualLightShadowSettings()             { return _PunctualLightShadowSettings; }
float GetPunctualLightShadowArraySize()             { return _PunctualLightShadowSettings.x; }
float GetPunctualLightShadowSampleNumber()          { return _PunctualLightShadowSettings.y; }
float GetPunctualLightShadowPenumbraWidth()         { return _PunctualLightShadowSettings.z; }

CBUFFER_START(LightParamsPerFrame)
    float4 _PunctualLightCount; // x: spot light count, y: point light count, z: 0, w: 0

    // Sun Light
    float4 _SunLightColor; // xyz: color * intensity, w: shadow strength
    float4 _SunLightDirection; // xyz: sun light direction, w: shadow mask channel
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT]; // xyz: culling sphere center, w: culling sphere radius
    float4x4 _SunLightShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];

    // Spot Light
    float4 _SpotLightColors[MAX_SPOT_LIGHT_COUNT]; // xyz: color * intensity, w: shadow strength
    float4 _SpotLightPositions[MAX_SPOT_LIGHT_COUNT]; // xyz: spot light position, w: shadow mask channel
    float4 _SpotLightDirections[MAX_SPOT_LIGHT_COUNT]; // xyz: spot light direction, w: 0
    float4 _SpotLightParams[MAX_SPOT_LIGHT_COUNT]; // x: 1.0 / spot light radius square, y: invAngleRange, z: cosOuterAngle, w: shadowing spot light index
    float4x4 _SpotLightShadowMatrices[MAX_SHADOWING_SPOT_LIGHT_COUNT];
    
    // Point Light
    float4 _PointLightColors[MAX_POINT_LIGHT_COUNT]; // xyz: color * intensity, w: shadow strength
    float4 _PointLightPositions[MAX_POINT_LIGHT_COUNT]; // xyz: point light position, w: shadow mask channel
    float4 _PointLightParams[MAX_POINT_LIGHT_COUNT]; // x: 1.0 / point light radius square, y: 0, z: 0, w: shadowing point light index
    float4x4 _PointLightShadowMatrices[MAX_SHADOWING_POINT_LIGHT_COUNT * 6];
CBUFFER_END

float3 GetSunLightColor()                           { return _SunLightColor.xyz; }
float GetSunLightShadowStrength()                   { return _SunLightColor.w; }
float3 GetSunLightDirection()                       { return _SunLightDirection.xyz; }
float GetSunLightShadowMaskChannel()                { return _SunLightDirection.w; }
float4 GetCascadeCullingSphere(int index)           { return _CascadeCullingSpheres[index]; }
float3 GetCascadeCullingSphereCenter(int index)     { return _CascadeCullingSpheres[index].xyz; }
float GetCascadeCullingSphereRadius(int index)      { return _CascadeCullingSpheres[index].w; }
float4x4 GetSunLightShadowMatrix(int index)         { return _SunLightShadowMatrices[index]; }

float GetSpotLightCount()                           { return _PunctualLightCount.x; }
float GetPointLightCount()                          { return _PunctualLightCount.y; }

float3 GetSpotLightColor(int index)                 { return _SpotLightColors[index].xyz; }
float GetSpotLightShadowStrength(int index)         { return _SpotLightColors[index].w; }
float3 GetSpotLightPosition(int index)              { return _SpotLightPositions[index].xyz; }
float GetSpotLightShadowMaskChannel(int index)      { return _SpotLightPositions[index].w; }
float3 GetSpotLightDirection(int index)             { return _SpotLightDirections[index].xyz; }
float GetSpotLightInverseRadiusSquare(int index)    { return _SpotLightParams[index].x; }
float2 GetSpotLightAngleParams(int index)           { return _SpotLightParams[index].yz; }
float GetSpotLightInverseAngleRange(int index)      { return _SpotLightParams[index].y; }
float GetSpotLightCosOuterAngle(int index)          { return _SpotLightParams[index].z; }
float GetShadowingSpotLightIndex(int index)         { return _SpotLightParams[index].w; }
float4x4 GetSpotLightShadowMatrix(int index)        { return _SpotLightShadowMatrices[index]; }

float3 GetPointLightColor(int index)                { return _PointLightColors[index].xyz; }
float GetPointLightShadowStrength(int index)        { return _PointLightColors[index].w; }
float3 GetPointLightPosition(int index)             { return _PointLightPositions[index].xyz; }
float GetPointLightShadowMaskChannel(int index)     { return _PointLightPositions[index].w; }
float GetPointLightInverseRadiusSquare(int index)   { return _PointLightParams[index].x; }
float GetShadowingPointLightIndex(int index)        { return _PointLightParams[index].w; }
float4x4 GetPointLightShadowMatrix(int index)       { return _PointLightShadowMatrices[index]; }

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------
#define SUN_LIGHT_SHADOW_MAP            _SunLightShadowMap
#define SPOT_LIGHT_SHADOW_MAP           _SpotLightShadowMap
#define POINT_LIGHT_SHADOW_MAP          _PointLightShadowMap
#define SHADOW_SAMPLER                  sampler_PointClampCompare

TEXTURE2D_ARRAY_SHADOW(SUN_LIGHT_SHADOW_MAP);
TEXTURE2D_ARRAY_SHADOW(SPOT_LIGHT_SHADOW_MAP);
TEXTURECUBE_ARRAY_SHADOW(POINT_LIGHT_SHADOW_MAP);
SAMPLER_CMP(SHADOW_SAMPLER);

#define ENVIRONMENT_BRDF_LUT            _EnvBRDFLut
#define LUT_SAMPLER                     sampler_Point_Clamp_EnvBRDFLut

TEXTURE2D(ENVIRONMENT_BRDF_LUT);
SAMPLER(LUT_SAMPLER);

#endif