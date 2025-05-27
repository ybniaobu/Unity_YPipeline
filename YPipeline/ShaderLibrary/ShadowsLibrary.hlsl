#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "Core/YPipelineCore.hlsl"
#include "../ShaderLibrary/RandomLibrary.hlsl"
#include "../ShaderLibrary/SamplingLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Sample Shadow Map or Array
// ----------------------------------------------------------------------------------------------------

float SampleShadowMap_Compare(float3 positionSS, TEXTURE2D_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowMapSampler, positionSS);
    return shadowAttenuation;
}

float SampleShadowMap_Depth(float2 uv, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, uv, 0).r;
    return depth;
}

float SampleShadowMap_DepthCompare(float3 positionSS, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, positionSS.xy, 0).r;
    return step(depth, positionSS.z);
}

float SampleShadowArray_Compare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_ARRAY_SHADOW(shadowMap, shadowMapSampler, positionSS, elementIndex);
    return shadowAttenuation;
}

float SampleShadowArray_Depth(float2 uv, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(shadowMap, shadowMapSampler, uv, elementIndex, 0).r;
    return depth;
}

float SampleShadowArray_DepthCompare(float3 positionSS, float elementIndex, TEXTURE2D_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(shadowMap, shadowMapSampler, positionSS.xy, elementIndex, 0).r;
    return step(depth, positionSS.z);
}

float SampleShadowCubeArray_Compare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURECUBE_ARRAY_SHADOW(shadowMap, shadowMapSampler, float4(sampleDir, z), elementIndex);
    return shadowAttenuation;
}

float SampleShadowCubeArray_Depth(float3 sampleDir, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY_LOD(shadowMap, shadowMapSampler, sampleDir, elementIndex, 0).r;
    return depth;
}

float SampleShadowCubeArray_DepthCompare(float3 sampleDir, float z, float elementIndex, TEXTURECUBE_ARRAY(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURECUBE_ARRAY_LOD(shadowMap, shadowMapSampler, sampleDir, elementIndex, 0).r;
    return step(depth, z);
}

// ----------------------------------------------------------------------------------------------------
// Light/Shadow Space Transform
// ----------------------------------------------------------------------------------------------------

float3 TransformWorldToSunLightShadowCoord(float3 positionWS, int cascadeIndex)
{
    // SS: shadow space
    float3 positionSS = mul(GetSunLightShadowMatrix(cascadeIndex), float4(positionWS, 1.0)).xyz;
    return positionSS;
}

float3 TransformWorldToSpotLightShadowCoord(float3 positionWS, int shadowingLightIndex)
{
    // SS: shadow space
    float4 positionSS_BeforeDivision = mul(GetSpotLightShadowMatrix(shadowingLightIndex), float4(positionWS, 1.0));
    float3 positionSS = positionSS_BeforeDivision.xyz / positionSS_BeforeDivision.w;
    return positionSS;
}

float3 TransformWorldToPointLightShadowCoord(float3 positionWS, int shadowingLightIndex, float faceIndex)
{
    // SS: shadow space
    float4 positionSS_BeforeDivision = mul(GetPointLightShadowMatrix(shadowingLightIndex * 6 + faceIndex), float4(positionWS, 1.0));
    float3 positionSS = positionSS_BeforeDivision.xyz / positionSS_BeforeDivision.w;
    return positionSS;
}

// ----------------------------------------------------------------------------------------------------
// Cascade Shadow Related Functions
// ----------------------------------------------------------------------------------------------------

float ComputeCascadeIndex(float3 positionWS)
{
    float3 vector0 = positionWS - GetCascadeCullingSphereCenter(0);
    float3 vector1 = positionWS - GetCascadeCullingSphereCenter(1);
    float3 vector2 = positionWS - GetCascadeCullingSphereCenter(2);
    float3 vector3 = positionWS - GetCascadeCullingSphereCenter(3);
    float4 distanceSquare = float4(dot(vector0, vector0), dot(vector1, vector1), dot(vector2, vector2), dot(vector3, vector3));
    float4 radiusSquare = float4(GetCascadeCullingSphereRadius(0) * GetCascadeCullingSphereRadius(0),
                                 GetCascadeCullingSphereRadius(1) * GetCascadeCullingSphereRadius(1),
                                 GetCascadeCullingSphereRadius(2) * GetCascadeCullingSphereRadius(2),
                                 GetCascadeCullingSphereRadius(3) * GetCascadeCullingSphereRadius(3));
    
    float4 indexes = float4(distanceSquare < radiusSquare);
    indexes.yzw = saturate(indexes.yzw - indexes.xyz);
    return 4.0 - dot(indexes, float4(4.0, 3.0, 2.0, 1.0));
}

float ComputeDistanceFade(float3 positionWS, float maxDistance, float distanceFade)
{
    float depth = -TransformWorldToView(positionWS).z;
    return saturate((1 - depth / maxDistance) / distanceFade);
}

float ComputeCascadeEdgeFade(float cascadeIndex, int cascadeCount, float3 positionWS, float cascadeEdgeFade, float4 lastSphere)
{
    //TODO: 当最大距离比较小时，有点问题，建议更改
    float isInLastSphere = cascadeIndex == cascadeCount - 1;
    float3 distanceVector = positionWS - lastSphere.xyz;
    float distanceSquare = dot(distanceVector, distanceVector);
    float fade = saturate((1 - distanceSquare / (lastSphere.w * lastSphere.w)) / cascadeEdgeFade);
    return lerp(1, fade, isInLastSphere);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Bias Related Functions
// ----------------------------------------------------------------------------------------------------

float ComputeTanHalfFOV(int spotLightIndex)
{
    float cosHalfFOV = GetSpotLightCosOuterAngle(spotLightIndex);
    float cosHalfFOVSquare = cosHalfFOV * cosHalfFOV;
    float sinHalfFOVSquare = 1.0 - cosHalfFOVSquare;
    float tanHalfFOVSquare = sinHalfFOVSquare / cosHalfFOVSquare;
    return sqrt(tanHalfFOVSquare);
}

//normalWS must be normalized
float3 ApplyShadowBias(float3 positionWS, float4 shadowBias, float texelSize, float penumbraWS, float3 normalWS, float3 L)
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 50.0); // maxBias

    float3 depthBias = (texelSize + penumbraWS) * shadowBias.x * L;
    float3 scaledDepthBias = (texelSize + penumbraWS) * tanTheta * shadowBias.y * L;
    float3 normalBias = (texelSize + penumbraWS) * shadowBias.z * normalWS;
    float3 scaledNormalBias = (texelSize + penumbraWS) * sinTheta * shadowBias.w * normalWS;

    // float3 depthBias = texelSize * (1.0 + penumbraTexel) * shadowBias.x * L;
    // float3 scaledDepthBias = texelSize * (1.0 + penumbraTexel) * tanTheta * shadowBias.y * L;
    // float3 normalBias = texelSize * (1.0 + penumbraTexel) * shadowBias.z * normalWS;
    // float3 scaledNormalBias = texelSize * (1.0 + penumbraTexel) * sinTheta * shadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// ----------------------------------------------------------------------------------------------------
// PCF Related Functions
// ----------------------------------------------------------------------------------------------------

#define DISK_SAMPLE_COUNT 64
static const float2 fibonacciSpiralDirection[DISK_SAMPLE_COUNT] =
{
    float2 (1, 0),
    float2 (-0.7373688780783197, 0.6754902942615238),
    float2 (0.08742572471695988, -0.9961710408648278),
    float2 (0.6084388609788625, 0.793600751291696),
    float2 (-0.9847134853154288, -0.174181950379311),
    float2 (0.8437552948123969, -0.5367280526263233),
    float2 (-0.25960430490148884, 0.9657150743757782),
    float2 (-0.46090702471337114, -0.8874484292452536),
    float2 (0.9393212963241182, 0.3430386308741014),
    float2 (-0.924345556137805, 0.3815564084749356),
    float2 (0.423845995047909, -0.9057342725556143),
    float2 (0.29928386444487326, 0.9541641203078969),
    float2 (-0.8652112097532296, -0.501407581232427),
    float2 (0.9766757736281757, -0.21471942904125949),
    float2 (-0.5751294291397363, 0.8180624302199686),
    float2 (-0.12851068979899202, -0.9917081236973847),
    float2 (0.764648995456044, 0.6444469828838233),
    float2 (-0.9991460540072823, 0.04131782619737919),
    float2 (0.7088294143034162, -0.7053799411794157),
    float2 (-0.04619144594036213, 0.9989326054954552),
    float2 (-0.6407091449636957, -0.7677836880006569),
    float2 (0.9910694127331615, 0.1333469877603031),
    float2 (-0.8208583369658855, 0.5711318504807807),
    float2 (0.21948136924637865, -0.9756166914079191),
    float2 (0.4971808749652937, 0.8676469198750981),
    float2 (-0.952692777196691, -0.30393498034490235),
    float2 (0.9077911335843911, -0.4194225289437443),
    float2 (-0.38606108220444624, 0.9224732195609431),
    float2 (-0.338452279474802, -0.9409835569861519),
    float2 (0.8851894374032159, 0.4652307598491077),
    float2 (-0.9669700052147743, 0.25489019011123065),
    float2 (0.5408377383579945, -0.8411269468800827),
    float2 (0.16937617250387435, 0.9855514761735877),
    float2 (-0.7906231749427578, -0.6123030256690173),
    float2 (0.9965856744766464, -0.08256508601054027),
    float2 (-0.6790793464527829, 0.7340648753490806),
    float2 (0.0048782771634473775, -0.9999881011351668),
    float2 (0.6718851669348499, 0.7406553331023337),
    float2 (-0.9957327006438772, -0.09228428288961682),
    float2 (0.7965594417444921, -0.6045602168251754),
    float2 (-0.17898358311978044, 0.9838520605119474),
    float2 (-0.5326055939855515, -0.8463635632843003),
    float2 (0.9644371617105072, 0.26431224169867934),
    float2 (-0.8896863018294744, 0.4565723210368687),
    float2 (0.34761681873279826, -0.9376366819478048),
    float2 (0.3770426545691533, 0.9261958953890079),
    float2 (-0.9036558571074695, -0.4282593745796637),
    float2 (0.9556127564793071, -0.2946256262683552),
    float2 (-0.50562235513749, 0.8627549095688868),
    float2 (-0.2099523790012021, -0.9777116131824024),
    float2 (0.8152470554454873, 0.5791133210240138),
    float2 (-0.9923232342597708, 0.12367133357503751),
    float2 (0.6481694844288681, -0.7614961060013474),
    float2 (0.036443223183926, 0.9993357251114194),
    float2 (-0.7019136816142636, -0.7122620188966349),
    float2 (0.998695384655528, 0.05106396643179117),
    float2 (-0.7709001090366207, 0.6369560596205411),
    float2 (0.13818011236605823, -0.9904071165669719),
    float2 (0.5671206801804437, 0.8236347091470047),
    float2 (-0.9745343917253847, -0.22423808629319533),
    float2 (0.8700619819701214, -0.49294233692210304),
    float2 (-0.30857886328244405, 0.9511987621603146),
    float2 (-0.4149890815356195, -0.9098263912451776),
    float2 (0.9205789302157817, 0.3905565685566777)
};

static const float2 poissonDisk[16] = {
    float2( -0.94201624, -0.39906216 ), float2( 0.94558609, -0.76890725 ), float2( -0.094184101, -0.92938870 ), float2( 0.34495938, 0.29387760 ),
    float2( -0.91588581, 0.45771432 ), float2( -0.81544232, -0.87912464 ), float2( -0.38277543, 0.27676845 ), float2( 0.97484398, 0.75648379 ),
    float2( 0.44323325, -0.97511554 ), float2( 0.53742981, -0.47373420 ), float2( -0.26496911, -0.41893023 ), float2( 0.79197514, 0.19090188 ),
    float2( -0.24188840, 0.99706507 ), float2( -0.81409955, 0.91437590 ), float2( 0.19984126, 0.78641367 ), float2( 0.14383161, -0.14100790 )
};

//index could be shadowingLightIndex or cascadeIndex
float ApplyPCF_2DArray(float index, TEXTURE2D_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionWS, float3 positionSS, float3 positionHCS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    //float randomRadian = random * TWO_PI;
    float randomRadian = SAMPLE_TEXTURE2D(_BlueNoise64, sampler_PointRepeat, positionHCS.xy * _CameraBufferSize.xy * 100).r * TWO_PI;
    //float randomRadian = InterleavedGradientNoise(positionHCS, 0) * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i+1, hash1, hash2))) * 0.5;
        //float2 offset = InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2)) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Bits(i + 1))) * 0.5;
        //float2 offset = mul(rotation, poissonDisk[i]) * 0.5;
        //float2 offset = mul(rotation, InverseSampleCircle(Halton_Float(i))) * 0.5;
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Bits(i))) * 0.5;
        //float2 offset = mul(rotation, fibonacciSpiralDirection[i]) * 0.5 * (i * rcp(sampleNumber));
        offset = offset * penumbraPercent;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_2DArray(float index, TEXTURE2D_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionSS, uint hash1, uint hash2, float2x2 rotation)
{
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Bits(i + 1))) * 0.5;
        //float2 offset = (float2(rotation[1]) + InverseSampleCircle(Sobol_Bits(i + 1))) * 0.5;
        //float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        //float2 offset = mul(rotation, fibonacciSpiralDirection[i]) * 0.5 * (i * rcp(sampleNumber) + rcp(sampleNumber)*0.5);
        offset = offset * penumbraPercent;
        float2 uv = positionSS.xy + offset;
        shadowAttenuation += SampleShadowArray_Compare(float3(uv, positionSS.z), index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionWS, float3 positionSS, float3 positionHCS)
{
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS));
    float random = floatConstruct(hash1);
    //float randomRadian = random * TWO_PI;
    float randomRadian = InterleavedGradientNoise(positionHCS, 0) * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        shadowAttenuation += SampleShadowCubeArray_Compare(sampleDir, positionSS.z, index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

float ApplyPCF_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY_SHADOW(shadowMap), float sampleNumber, float penumbraPercent, float3 positionSS, uint hash1, uint hash2, float2x2 rotation)
{
    float shadowAttenuation = 0.0;
    for (float i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        //float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Sobol_Bits(i + 2))) * 0.5;
        offset = offset * penumbraPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        shadowAttenuation += SampleShadowCubeArray_Compare(sampleDir, positionSS.z, index, shadowMap, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleNumber;
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCF
// ----------------------------------------------------------------------------------------------------

float GetSunLightShadowAttenuation_PCF(float3 positionWS, float3 normalWS, float3 L, float3 positionHCS)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    float shadowStrength = GetSunLightShadowStrength();
    
    if (cascadeIndex >= GetSunLightCascadeCount()) return 1.0;
    
    float shadowFade = 1.0;
    shadowFade *= ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    shadowFade *= ComputeCascadeEdgeFade(cascadeIndex, GetSunLightCascadeCount(), positionWS, GetCascadeEdgeFade(), GetCascadeCullingSphere(GetSunLightCascadeCount() - 1));

    float texelSize = GetCascadeCullingSphereRadius(cascadeIndex) * 2.0 / GetSunLightShadowMapSize();
    float penumbraPercent = GetSunLightPCFPenumbraWidth() / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, GetSunLightPCFPenumbraWidth(), normalWS, L);
    float3 positionSS = TransformWorldToSunLightShadowCoord(positionWS_Bias, cascadeIndex);
    float shadowAttenuation = ApplyPCF_2DArray(cascadeIndex, SUN_LIGHT_SHADOW_MAP, GetSunLightPCFSampleNumber(), penumbraPercent, positionWS, positionSS, positionHCS);
    
    return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
}

float GetSpotLightShadowAttenuation_PCF(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float3 positionHCS)
{
    float shadowStrength = GetSpotLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingSpotLightIndex = GetShadowingSpotLightIndex(lightIndex);
    //float linearDepth = mul(GetSpotLightShadowMatrix(shadowingSpotLightIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth / GetSpotLightShadowMapSize();
    float penumbraPercent = GetSpotLightPCFPenumbraWidth(shadowingSpotLightIndex) / 4.0;
    float penumbraWS = penumbraPercent * 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, penumbraWS, normalWS, L);
    float3 positionSS = TransformWorldToSpotLightShadowCoord(positionWS_Bias, shadowingSpotLightIndex);
    float shadowAttenuation = ApplyPCF_2DArray(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, GetSpotLightPCFSampleNumber(shadowingSpotLightIndex), penumbraPercent, positionWS, positionSS, positionHCS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

float GetPointLightShadowAttenuation_PCF(int lightIndex, float faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float3 positionHCS)
{
    float shadowStrength = GetPointLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingPointLightIndex = GetShadowingPointLightIndex(lightIndex);
    //float linearDepth = mul(GetPointLightShadowMatrix(shadowingPointLightIndex * 6 + faceIndex), float4(positionWS, 1.0)).w;
    float texelSize = 2.0 * linearDepth / GetPointLightShadowMapSize();
    float penumbraPercent = GetPointLightPCFPenumbraWidth(shadowingPointLightIndex) / 4.0;
    float penumbraWS = penumbraPercent * 2.0 * linearDepth;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, penumbraWS, normalWS, L);
    //float3 sampleDir = normalize(positionWS_Bias - GetPointLightPosition(lightIndex));
    float3 positionSS = TransformWorldToPointLightShadowCoord(positionWS_Bias, shadowingPointLightIndex, faceIndex);
    float shadowAttenuation = ApplyPCF_CubeArray(shadowingPointLightIndex, faceIndex, POINT_LIGHT_SHADOW_MAP, GetPointLightPCFSampleNumber(shadowingPointLightIndex), penumbraPercent, positionWS, positionSS, positionHCS);
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

// ----------------------------------------------------------------------------------------------------
// PCSS Related Functions
// ----------------------------------------------------------------------------------------------------

float NonLinearToLinearDepth_Ortho(float4 depthParams, float nonLinearDepth)
{
    return (depthParams.y - 2.0 * nonLinearDepth + 1.0) / depthParams.x;
}

float NonLinearToLinearDepth_Persp(float4 depthParams, float nonLinearDepth)
{
    return depthParams.y / (2.0 * nonLinearDepth - 1.0 + depthParams.x);
}

float3 ComputeAverageBlockerDepth_2DArray_Ortho(float index, TEXTURE2D_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Ortho(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv = positionSS.xy + offset;
        float d_Blocker = SampleShadowArray_Depth(uv, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Ortho(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_2DArray_Persp(float index, TEXTURE2D_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv = positionSS.xy + offset;
        float d_Blocker = SampleShadowArray_Depth(uv, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_CubeArray(float index, float faceIndex, TEXTURECUBE_ARRAY(shadowMap), float sampleNumber,
    float searchWidthPercent, float3 positionSS, float4 depthParams, uint hash1, uint hash2, float2x2 rotation)
{
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-8; // avoid division by zero

    for (int i = 0; i < sampleNumber; i++)
    {
        //float2 offset = mul(rotation, InverseSampleCircle(Sobol_Scrambled(i, hash1, hash2))) * 0.5;
        float2 offset = mul(rotation, InverseSampleCircle(Hammersley_Bits(i + 1, sampleNumber + 1))) * 0.5;
        offset = offset * searchWidthPercent;
        float2 uv_Offset = positionSS.xy + offset;
        float3 sampleDir = CubeMapping(faceIndex, uv_Offset);
        float d_Blocker = SampleShadowCubeArray_Depth(sampleDir, index, shadowMap, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCSS
// ----------------------------------------------------------------------------------------------------

float GetSunLightShadowAttenuation_PCSS(float3 positionWS, float3 normalWS, float3 L, float3 positionHCS)
{
    float cascadeIndex = ComputeCascadeIndex(positionWS);
    if (cascadeIndex >= GetSunLightCascadeCount()) return 1.0;
    float shadowStrength = GetSunLightShadowStrength();
    float shadowFade = 1.0;
    shadowFade *= ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    shadowFade *= ComputeCascadeEdgeFade(cascadeIndex, GetSunLightCascadeCount(), positionWS, GetCascadeEdgeFade(), GetCascadeCullingSphere(GetSunLightCascadeCount() - 1));

    float texelSize = GetCascadeCullingSphereRadius(cascadeIndex) * 2.0 / GetSunLightShadowMapSize();
    // float searchWidthWS = GetSunLightSize();
    float searchWidthWS = GetSunLightSize();
    float searchWidthPercent = searchWidthWS / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;

    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, searchWidthWS, normalWS, L);
    float3 positionSS_Search = TransformWorldToSunLightShadowCoord(positionWS_SearchBias, cascadeIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionHCS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    //float random = floatConstruct(hash1);
    //float randomRadian = InterleavedGradientNoise(positionHCS * 1, 0) * TWO_PI;
    float randomRadian = SAMPLE_TEXTURE2D(_BlueNoise64, sampler_PointRepeat, positionHCS.xy * _CameraBufferSize.xy * 100).r * TWO_PI;
    //float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));

    float4 depthParams = GetSunLightDepthParams(cascadeIndex);
    float blockerSampleNumber = GetSunLightBlockerSampleNumber();
    
    float3 blocker = ComputeAverageBlockerDepth_2DArray_Ortho(cascadeIndex, SUN_LIGHT_SHADOW_MAP, blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;

    //float penumbraWS = GetSunLightPenumbraScale() * (blocker.z - ald_Blocker) * 0.01;
    float penumbraWS = GetSunLightPenumbraScale() * GetSunLightSize() * (blocker.z - ald_Blocker) * 0.1;
    float penumbraPercent = penumbraWS / GetCascadeCullingSphereRadius(cascadeIndex) * 0.5;
    
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, penumbraWS, normalWS, L);
    float3 positionSS_Filter = TransformWorldToSunLightShadowCoord(positionWS_FilterBias, cascadeIndex);
    float filterSampleNumber = GetSunLightFilterSampleNumber();
    float shadowAttenuation = ApplyPCF_2DArray(cascadeIndex, SUN_LIGHT_SHADOW_MAP, filterSampleNumber, penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    return lerp(1.0, shadowAttenuation, shadowStrength * shadowFade);
}

float GetSpotLightShadowAttenuation_PCSS(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float3 positionHCS)
{
    float shadowStrength = GetSpotLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingSpotLightIndex = GetShadowingSpotLightIndex(lightIndex);
    float texelSize = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth / GetSpotLightShadowMapSize();
    float searchWidthWS = GetSpotLightSize(shadowingSpotLightIndex);
    float searchWidthPercent = searchWidthWS / (2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth);
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, searchWidthWS, normalWS, L);
    float3 positionSS_Search = TransformWorldToSpotLightShadowCoord(positionWS_SearchBias, shadowingSpotLightIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    float random = floatConstruct(hash1);
    float randomRadian = InterleavedGradientNoise(positionHCS, 0) * TWO_PI;
    //float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float4 depthParams = GetSpotLightDepthParams(shadowingSpotLightIndex);
    float blockerSampleNumber = GetSpotLightBlockerSampleNumber(shadowingSpotLightIndex);
    float3 blocker = ComputeAverageBlockerDepth_2DArray_Persp(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;

    float penumbraWS = GetSpotLightPenumbraScale(shadowingSpotLightIndex) * GetSpotLightSize(shadowingSpotLightIndex) * (linearDepth - ald_Blocker) / ald_Blocker;
    float penumbraPercent = penumbraWS / (2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth);
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetSpotLightShadowBias(shadowingSpotLightIndex), texelSize, penumbraWS, normalWS, L);
    float3 positionSS_Filter = TransformWorldToSpotLightShadowCoord(positionWS_FilterBias, shadowingSpotLightIndex);
    float filterSampleNumber = GetSpotLightFilterSampleNumber(shadowingSpotLightIndex);
    float shadowAttenuation = ApplyPCF_2DArray(shadowingSpotLightIndex, SPOT_LIGHT_SHADOW_MAP, filterSampleNumber, penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

float GetPointLightShadowAttenuation_PCSS(int lightIndex, float faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float3 positionHCS)
{
    float shadowStrength = GetPointLightShadowStrength(lightIndex);
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    
    float shadowingPointLightIndex = GetShadowingPointLightIndex(lightIndex);
    float texelSize = 2.0 * linearDepth / GetPointLightShadowMapSize();
    float searchWidthWS = GetPointLightSize(shadowingPointLightIndex);
    float searchWidthPercent = searchWidthWS / (2.0 * linearDepth);
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, searchWidthWS, normalWS, L);
    //float3 sampleDir_Search = normalize(positionWS_SearchBias - GetPointLightPosition(lightIndex));
    float3 positionSS_Search = TransformWorldToPointLightShadowCoord(positionWS_SearchBias, shadowingPointLightIndex, faceIndex);
    
    uint hash1 = Hash_Jenkins(asuint(positionWS));
    uint hash2 = Hash_Jenkins(asuint(positionSS_Search));
    //float random = floatConstruct(hash1);
    float randomRadian = InterleavedGradientNoise(positionHCS, 0) * TWO_PI;
    //float randomRadian = SAMPLE_TEXTURE2D(_BlueNoise64, sampler_PointRepeat, positionHCS.xy * _CameraBufferSize.xy * 100).r * TWO_PI;
    //float randomRadian = random * TWO_PI;
    float2x2 rotation = float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
    
    float4 depthParams = GetPointLightDepthParams(shadowingPointLightIndex);
    float blockerSampleNumber = GetPointLightBlockerSampleNumber(shadowingPointLightIndex);
    float3 blocker = ComputeAverageBlockerDepth_CubeArray(shadowingPointLightIndex,faceIndex, POINT_LIGHT_SHADOW_MAP,
        blockerSampleNumber, searchWidthPercent, positionSS_Search, depthParams, hash1, hash2, rotation);
    float ald_Blocker = blocker.x;
    float blockerCount = blocker.y;
    
    if (blockerCount < 1.0) return 1.0;
    
    float penumbraWS = GetPointLightPenumbraScale(shadowingPointLightIndex) * GetPointLightSize(shadowingPointLightIndex) * (linearDepth - ald_Blocker) / ald_Blocker;
    float penumbraPercent = penumbraWS / (2.0 * linearDepth);
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetPointLightShadowBias(shadowingPointLightIndex), texelSize, penumbraWS, normalWS, L);
    //float3 sampleDir_Filter = normalize(positionWS_FilterBias - GetPointLightPosition(lightIndex));
    float3 positionSS_Filter = TransformWorldToPointLightShadowCoord(positionWS_FilterBias, shadowingPointLightIndex, faceIndex);
    float filterSampleNumber = GetPointLightFilterSampleNumber(shadowingPointLightIndex);
    float shadowAttenuation = ApplyPCF_CubeArray(shadowingPointLightIndex, faceIndex, POINT_LIGHT_SHADOW_MAP, filterSampleNumber,
        penumbraPercent, positionSS_Filter, hash1, hash2, rotation);
    
    return lerp(1.0, shadowAttenuation, shadowStrength * distanceFade);
}

#endif