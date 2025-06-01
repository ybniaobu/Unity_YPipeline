#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Constant Buffers
// ----------------------------------------------------------------------------------------------------

float4 _CameraBufferSize; // x: 1.0 / bufferSize.x, y: 1.0 / bufferSize.y, z: bufferSize.x, w: bufferSize.y

// TODO: Global Constant buffer 存放一些全局的只需设置一次的 constant buffer
CBUFFER_START(LightParamsPerSetting)
    float4 _CascadeSettings; // x: max shadow distance, y: shadow distance fade, z: sun light cascade count, w: cascade edge fade
    float4 _ShadowMapSizes; // x: sun light shadow map size, y: spot light shadow map size, z: point light shadow map size
CBUFFER_END

float GetMaxShadowDistance()                        { return _CascadeSettings.x; }
float GetShadowDistanceFade()                       { return _CascadeSettings.y; }
float GetSunLightCascadeCount()                     { return _CascadeSettings.z; }
float GetCascadeEdgeFade()                          { return _CascadeSettings.w; }
float GetSunLightShadowMapSize()                    { return _ShadowMapSizes.x; }
float GetSpotLightShadowMapSize()                   { return _ShadowMapSizes.y; }
float GetPointLightShadowMapSize()                  { return _ShadowMapSizes.z; }

CBUFFER_START(SunLightParams)
    float4 _SunLightColor; // xyz: color * intensity
    float4 _SunLightDirection; // xyz: sun light direction, w: whether is shadowing (1 for shadowing)
    float4 _SunLightShadowColor; // xyz: shadow color, w: shadow strengths
    float4 _SunLightPenumbraColor; // xyz: penumbra color
    float4 _SunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SunLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 _SunLightShadowParams2; // x: light diameter, y: blocker search area size z: blocker search sample number, w: min penumbra(filter) width

    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT]; // xyz: culling sphere center, w: culling sphere radius
    float4x4 _SunLightShadowMatrices[MAX_CASCADE_COUNT];
    float4 _SunLightDepthParams[MAX_CASCADE_COUNT]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
CBUFFER_END

float3 GetSunLightColor()                                   { return _SunLightColor.xyz; }
float3 GetSunLightDirection()                               { return _SunLightDirection.xyz; }
bool IsSunLightShadowing()                                  { return _SunLightDirection.w > 0.5f; }
float3 GetSunLightShadowColor()                             { return _SunLightShadowColor.xyz; }
float GetSunLightShadowStrength()                           { return _SunLightShadowColor.w; }
float3 GetSunLightPenumbraColor()                           { return _SunLightPenumbraColor.xyz; }
float4 GetSunLightShadowBias()                              { return _SunLightShadowBias; }
float GetSunLightPCFPenumbraWidth()                         { return _SunLightShadowParams.x; }
float GetSunLightPCFSampleNumber()                          { return _SunLightShadowParams.y; }
float GetSunLightPCSSPenumbraScale()                        { return _SunLightShadowParams.x; }
float GetSunLightPCSSSampleNumber()                         { return _SunLightShadowParams.y; }
float GetSunLightSize()                                     { return _SunLightShadowParams2.x; }
float GetSunLightBlockerSearchScale()                       { return _SunLightShadowParams2.y; }
float GetSunLightBlockerSampleNumber()                      { return _SunLightShadowParams2.z; }
float GetSunLightMinPenumbraWidth()                         { return _SunLightShadowParams2.w; }

float4 GetCascadeCullingSphere(int cascadeIndex)            { return _CascadeCullingSpheres[cascadeIndex]; }
float3 GetCascadeCullingSphereCenter(int cascadeIndex)      { return _CascadeCullingSpheres[cascadeIndex].xyz; }
float GetCascadeCullingSphereRadius(int cascadeIndex)       { return _CascadeCullingSpheres[cascadeIndex].w; }
float4x4 GetSunLightShadowMatrix(int cascadeIndex)          { return _SunLightShadowMatrices[cascadeIndex]; }
float4 GetSunLightDepthParams(int cascadeIndex)             { return _SunLightDepthParams[cascadeIndex]; }

// ----------------------------------------------------------------------------------------------------
// Structured Buffers
// ----------------------------------------------------------------------------------------------------

// float4 _PunctualLightCount; 
// float GetPunctualLightCount() { return _PunctualLightCount.x; }

float4 _TileParams; // xy: tileCountXY, zw: tileUVSizeXY
StructuredBuffer<uint> _TilesLightIndicesBuffer;

struct PunctualLightData
{
    float4 punctualLightColors; // xyz: light color * intensity, w: light type (point 1, spot 2)
    float4 punctualLightPositions; // xyz: light position, w: shadowing spot/point light index (non-shadowing is -1)
    float4 punctualLightDirections; // xyz: spot light direction
    float4 punctualLightParams; // x: 1.0 / light range square, y: range attenuation scale, z: invAngleRange, w: cosOuterAngle
};

StructuredBuffer<PunctualLightData> _PunctualLightData;

float3 GetPunctualLightColor(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightColors.xyz; }
float GetPunctualLightType(int lightIndex)                  { return _PunctualLightData[lightIndex].punctualLightColors.w; }
float3 GetPunctualLightPosition(int lightIndex)             { return _PunctualLightData[lightIndex].punctualLightPositions.xyz; }
float GetShadowingLightIndex(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightPositions.w; }
float3 GetSpotLightDirection(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightDirections.xyz; }
float GetPunctualLightInverseRangeSquare(int lightIndex)    { return _PunctualLightData[lightIndex].punctualLightParams.x; }
float GetPunctualLightRangeAttenuationScale(int lightIndex) { return _PunctualLightData[lightIndex].punctualLightParams.y; }
float2 GetSpotLightAngleParams(int lightIndex)              { return _PunctualLightData[lightIndex].punctualLightParams.zw; }
float GetSpotLightInverseAngleRange(int lightIndex)         { return _PunctualLightData[lightIndex].punctualLightParams.z; }
float GetSpotLightCosOuterAngle(int lightIndex)             { return _PunctualLightData[lightIndex].punctualLightParams.w; }

struct PointLightShadowData
{
    float4 pointLightShadowColors; // xyz: shadow color, w: shadow strengths
    float4 pointLightPenumbraColors; // xyz: penumbra color
    float4 pointLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 pointLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 pointLightShadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
    float4 pointLightDepthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
};

StructuredBuffer<PointLightShadowData> _PointLightShadowData;
StructuredBuffer<float4x4> _PointLightShadowMatrices;

float3 GetPointLightShadowColor(int shadowIndex)                     { return _PointLightShadowData[shadowIndex].pointLightShadowColors.xyz; }
float GetPointLightShadowStrength(int shadowIndex)                   { return _PointLightShadowData[shadowIndex].pointLightShadowColors.w; }
float3 GetPointLightPenumbraColor(int shadowIndex)                   { return _PointLightShadowData[shadowIndex].pointLightPenumbraColors.xyz; }
float4 GetPointLightShadowBias(int shadowIndex)                      { return _PointLightShadowData[shadowIndex].pointLightShadowBias; }
float GetPointLightPCFPenumbraWidth(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams.x; }
float GetPointLightPCFSampleNumber(int shadowIndex)                  { return _PointLightShadowData[shadowIndex].pointLightShadowParams.y; }
float GetPointLightPCSSPenumbraScale(int shadowIndex)                { return _PointLightShadowData[shadowIndex].pointLightShadowParams.x; }
float GetPointLightPCSSSampleNumber(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams.y; }
float GetPointLightSize(int shadowIndex)                             { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.x; }
float GetPointLightBlockerSearchScale(int shadowIndex)               { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.y; }
float GetPointLightBlockerSampleNumber(int shadowIndex)              { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.z; }
float GetPointLightMinPenumbraWidth(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.w; }
float4 GetPointLightDepthParams(int shadowIndex)                     { return _PointLightShadowData[shadowIndex].pointLightDepthParams; }
float4x4 GetPointLightShadowMatrix(int shadowFaceIndex)              { return _PointLightShadowMatrices[shadowFaceIndex]; }

struct SpotLightShadowData
{
    float4 spotLightShadowColors; // xyz: shadow color, w: shadow strengths
    float4 spotLightPenumbraColors; // xyz: penumbra color
    float4 spotLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 spotLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 spotLightShadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
    float4 spotLightDepthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
};

StructuredBuffer<SpotLightShadowData> _SpotLightShadowData;
StructuredBuffer<float4x4> _SpotLightShadowMatrices;

float3 GetSpotLightShadowColor(int shadowIndex)                     { return _SpotLightShadowData[shadowIndex].spotLightShadowColors.xyz; }
float GetSpotLightShadowStrength(int shadowIndex)                   { return _SpotLightShadowData[shadowIndex].spotLightShadowColors.w; }
float3 GetSpotLightPenumbraColor(int shadowIndex)                   { return _SpotLightShadowData[shadowIndex].spotLightPenumbraColors.xyz; }
float4 GetSpotLightShadowBias(int shadowIndex)                      { return _SpotLightShadowData[shadowIndex].spotLightShadowBias; }
float GetSpotLightPCFPenumbraWidth(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.x; }
float GetSpotLightPCFSampleNumber(int shadowIndex)                  { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.y; }
float GetSpotLightPCSSPenumbraScale(int shadowIndex)                { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.x; }
float GetSpotLightPCSSSampleNumber(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.y; }
float GetSpotLightSize(int shadowIndex)                             { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.x; }
float GetSpotLightBlockerSearchScale(int shadowIndex)               { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.y; }
float GetSpotLightBlockerSampleNumber(int shadowIndex)              { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.z; }
float GetSpotLightMinPenumbraWidth(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.w; }
float4 GetSpotLightDepthParams(int shadowIndex)                     { return _SpotLightShadowData[shadowIndex].spotLightDepthParams; }
float4x4 GetSpotLightShadowMatrix(int shadowIndex)                  { return _SpotLightShadowMatrices[shadowIndex]; }

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

// Blue Noise
TEXTURE2D(_BlueNoise64);

// General Samplers
SAMPLER(sampler_PointRepeat);
SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_LinearRepeat);

#endif