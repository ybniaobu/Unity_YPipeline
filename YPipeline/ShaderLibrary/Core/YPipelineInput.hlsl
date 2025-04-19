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

float4 _CameraBufferSize; // x: 1.0 / bufferSize.x, y: 1.0 / bufferSize.y, z: bufferSize.x, w: bufferSize.y

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
    float4x4 _SunLightShadowMatrices[MAX_CASCADE_COUNT];
    float4 _SunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SunLightShadowParams; // x: light size, y: penumbra scale, z: blocker search sample number, w: filter sample number
    float4 _SunLightDepthParams[MAX_CASCADE_COUNT]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)

    // Spot Light
    float4 _SpotLightColors[MAX_SPOT_LIGHT_COUNT]; // xyz: color * intensity, w: shadow strength
    float4 _SpotLightPositions[MAX_SPOT_LIGHT_COUNT]; // xyz: spot light position, w: shadow mask channel
    float4 _SpotLightDirections[MAX_SPOT_LIGHT_COUNT]; // xyz: spot light direction, w: 0
    float4 _SpotLightParams[MAX_SPOT_LIGHT_COUNT]; // x: 1.0 / spot light range square, y: invAngleRange, z: cosOuterAngle, w: shadowing spot light index
    float4x4 _SpotLightShadowMatrices[MAX_SHADOWING_SPOT_LIGHT_COUNT];
    float4 _SpotLightShadowBias[MAX_SHADOWING_SPOT_LIGHT_COUNT]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SpotLightShadowParams[MAX_SHADOWING_SPOT_LIGHT_COUNT]; // x: light size, y: penumbra scale, z: blocker search sample number, w: filter sample number
    float4 _SpotLightDepthParams[MAX_SHADOWING_SPOT_LIGHT_COUNT]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
    
    // Point Light
    float4 _PointLightColors[MAX_POINT_LIGHT_COUNT]; // xyz: color * intensity, w: shadow strength
    float4 _PointLightPositions[MAX_POINT_LIGHT_COUNT]; // xyz: point light position, w: shadow mask channel
    float4 _PointLightParams[MAX_POINT_LIGHT_COUNT]; // x: 1.0 / point light range square, y: 0, z: 0, w: shadowing point light index
    float4x4 _PointLightShadowMatrices[MAX_SHADOWING_POINT_LIGHT_COUNT * 6];
    float4 _PointLightShadowBias[MAX_SHADOWING_POINT_LIGHT_COUNT]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _PointLightShadowParams[MAX_SHADOWING_POINT_LIGHT_COUNT]; // x: light size, y: penumbra scale, z: blocker search sample number, w: filter sample number
    float4 _PointLightDepthParams[MAX_SHADOWING_POINT_LIGHT_COUNT]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
CBUFFER_END

float3 GetSunLightColor()                                   { return _SunLightColor.xyz; }
float GetSunLightShadowStrength()                           { return _SunLightColor.w; }
float3 GetSunLightDirection()                               { return _SunLightDirection.xyz; }
float GetSunLightShadowMaskChannel()                        { return _SunLightDirection.w; }
float4 GetCascadeCullingSphere(int cascadeIndex)            { return _CascadeCullingSpheres[cascadeIndex]; }
float3 GetCascadeCullingSphereCenter(int cascadeIndex)      { return _CascadeCullingSpheres[cascadeIndex].xyz; }
float GetCascadeCullingSphereRadius(int cascadeIndex)       { return _CascadeCullingSpheres[cascadeIndex].w; }
float4x4 GetSunLightShadowMatrix(int cascadeIndex)          { return _SunLightShadowMatrices[cascadeIndex]; }
float4 GetSunLightShadowBias()                              { return _SunLightShadowBias; }
float GetSunLightSize()                                     { return _SunLightShadowParams.x; }
float GetSunLightPenumbraScale()                            { return _SunLightShadowParams.y; }
float GetSunLightBlockerSampleNumber()                      { return _SunLightShadowParams.z; }
float GetSunLightFilterSampleNumber()                       { return _SunLightShadowParams.w; }
float4 GetSunLightDepthParams(int cascadeIndex)             { return _SunLightDepthParams[cascadeIndex]; }

float GetSpotLightCount()                                   { return _PunctualLightCount.x; }
float GetPointLightCount()                                  { return _PunctualLightCount.y; }

float3 GetSpotLightColor(int lightIndex)                    { return _SpotLightColors[lightIndex].xyz; }
float GetSpotLightShadowStrength(int lightIndex)            { return _SpotLightColors[lightIndex].w; }
float3 GetSpotLightPosition(int lightIndex)                 { return _SpotLightPositions[lightIndex].xyz; }
float GetSpotLightShadowMaskChannel(int lightIndex)         { return _SpotLightPositions[lightIndex].w; }
float3 GetSpotLightDirection(int lightIndex)                { return _SpotLightDirections[lightIndex].xyz; }
float GetSpotLightInverseRangeSquare(int lightIndex)        { return _SpotLightParams[lightIndex].x; }
float2 GetSpotLightAngleParams(int lightIndex)              { return _SpotLightParams[lightIndex].yz; }
float GetSpotLightInverseAngleRange(int lightIndex)         { return _SpotLightParams[lightIndex].y; }
float GetSpotLightCosOuterAngle(int lightIndex)             { return _SpotLightParams[lightIndex].z; }
float GetShadowingSpotLightIndex(int lightIndex)            { return _SpotLightParams[lightIndex].w; }
float4x4 GetSpotLightShadowMatrix(int shadowIndex)          { return _SpotLightShadowMatrices[shadowIndex]; }
float4 GetSpotLightShadowBias(int shadowIndex)              { return _SpotLightShadowBias[shadowIndex]; }
float GetSpotLightSize(int shadowIndex)                     { return _SpotLightShadowParams[shadowIndex].x; }
float GetSpotLightPenumbraScale(int shadowIndex)            { return _SpotLightShadowParams[shadowIndex].y; }
float GetSpotLightBlockerSampleNumber(int shadowIndex)      { return _SpotLightShadowParams[shadowIndex].z; }
float GetSpotLightFilterSampleNumber(int shadowIndex)       { return _SpotLightShadowParams[shadowIndex].w; }
float4 GetSpotLightDepthParams(int shadowIndex)             { return _SpotLightDepthParams[shadowIndex]; }

float3 GetPointLightColor(int lightIndex)                   { return _PointLightColors[lightIndex].xyz; }
float GetPointLightShadowStrength(int lightIndex)           { return _PointLightColors[lightIndex].w; }
float3 GetPointLightPosition(int lightIndex)                { return _PointLightPositions[lightIndex].xyz; }
float GetPointLightShadowMaskChannel(int lightIndex)        { return _PointLightPositions[lightIndex].w; }
float GetPointLightInverseRangeSquare(int lightIndex)       { return _PointLightParams[lightIndex].x; }
float GetShadowingPointLightIndex(int lightIndex)           { return _PointLightParams[lightIndex].w; }
float4x4 GetPointLightShadowMatrix(int faceIndex)         { return _PointLightShadowMatrices[faceIndex]; }
float4 GetPointLightShadowBias(int shadowIndex)             { return _PointLightShadowBias[shadowIndex]; }
float GetPointLightSize(int shadowIndex)                    { return _PointLightShadowParams[shadowIndex].x; }
float GetPointLightPenumbraScale(int shadowIndex)           { return _PointLightShadowParams[shadowIndex].y; }
float GetPointLightBlockerSampleNumber(int shadowIndex)     { return _PointLightShadowParams[shadowIndex].z; }
float GetPointLightFilterSampleNumber(int shadowIndex)      { return _PointLightShadowParams[shadowIndex].w; }
float4 GetPointLightDepthParams(int shadowIndex)            { return _PointLightDepthParams[shadowIndex]; }

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------

// Shadow Texture
#define SUN_LIGHT_SHADOW_MAP            _SunLightShadowMap
#define SPOT_LIGHT_SHADOW_MAP           _SpotLightShadowMap
#define POINT_LIGHT_SHADOW_MAP          _PointLightShadowMap
#define SHADOW_SAMPLER_COMPARE          sampler_PointClampCompare
#define SHADOW_SAMPLER                  sampler_PointClamp

TEXTURE2D_ARRAY_SHADOW(SUN_LIGHT_SHADOW_MAP);
TEXTURE2D_ARRAY_SHADOW(SPOT_LIGHT_SHADOW_MAP);
TEXTURECUBE_ARRAY_SHADOW(POINT_LIGHT_SHADOW_MAP);
SAMPLER_CMP(SHADOW_SAMPLER_COMPARE);
SAMPLER(SHADOW_SAMPLER);

// BRDF LUT
#define ENVIRONMENT_BRDF_LUT            _EnvBRDFLut
#define LUT_SAMPLER                     sampler_Point_Clamp_EnvBRDFLut

TEXTURE2D(ENVIRONMENT_BRDF_LUT);
SAMPLER(LUT_SAMPLER);

// Depth & Normal & Opaque Texture
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);

// General Samplers
SAMPLER(sampler_PointRepeat);
SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_LinearRepeat);

#endif