#ifndef YPIPELINE_STANDARD_FORWARD_PASS_INCLUDED
#define YPIPELINE_STANDARD_FORWARD_PASS_INCLUDED

#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "../../ShaderLibrary/RenderingEquationLibrary.hlsl"

CBUFFER_START (UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float _Specular;
    float _Roughness;
    float _Metallic;
    float _NormalIntensity;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _RoughnessTex;
Texture2D _MetallicTex;
Texture2D _NormalTex;
Texture2D _AOTex;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    LIGHTMAP_UV(1)
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float3 tangentWS    : TEXCOORD3;
    float3 binormalWS   : TEXCOORD4;
    LIGHTMAP_UV(5)
};

void InitializeStandardPBRParams(Varyings IN, out StandardPBRParams standardPBRParams)
{
    standardPBRParams.albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgb * _BaseColor.rgb;

    #if _USE_ROUGHNESSTEX
        standardPBRParams.roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
    #else
        standardPBRParams.roughness = _Roughness;
    #endif

    #if _USE_METALLICTEX
        standardPBRParams.metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
    #else
        standardPBRParams.metallic = _Metallic;
    #endif

    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_Trilinear_Repeat_BaseTex, IN.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = normalize(IN.normalWS);
        float3 t = normalize(IN.tangentWS);
        float3 b = normalize(IN.binormalWS);
        float3x3 tbn = float3x3(t, b, n);
        standardPBRParams.N = normalize(mul(normalTS, tbn));
    #else
        standardPBRParams.N = normalize(IN.normalWS);
    #endif

    standardPBRParams.ao = SAMPLE_TEXTURE2D(_AOTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
    standardPBRParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), standardPBRParams.albedo, standardPBRParams.metallic);
    standardPBRParams.F90 = saturate(dot(standardPBRParams.F0, 50.0 * 0.3333));
    standardPBRParams.V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
    standardPBRParams.R = reflect(-standardPBRParams.V, standardPBRParams.N);
    standardPBRParams.NoV = saturate(dot(standardPBRParams.N, standardPBRParams.V)) + 1e-3; //防止小黑点
}

Varyings StandardVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
    OUT.binormalWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
    TRANSFER_LIGHTMAP_UV(IN, OUT)
    return OUT;
}

float4 StandardFrag(Varyings IN) : SV_TARGET
{
    RenderingEquationContent renderingEquationContent = (RenderingEquationContent) 0;
    
    StandardPBRParams standardPBRParams = (StandardPBRParams) 0;
    InitializeStandardPBRParams(IN, standardPBRParams);

    // ----------------------------------------------------------------------------------------------------
    // Indirect Lighting
    // ----------------------------------------------------------------------------------------------------
    float3 envBRDF = SampleEnvLut(ENVIRONMENT_BRDF_LUT, LUT_SAMPLER, standardPBRParams.NoV, standardPBRParams.roughness);
    float3 energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) * 0.5; // 0.5 is a magic number
    
    renderingEquationContent.indirectLightDiffuse += IndirectLighting_Diffuse(LIGHTMAP_UV_FRAGMENT(IN), standardPBRParams, envBRDF.b);

    //renderingEquationContent.indirectLightSpecular += CalculateIBL_Specular(standardPBRParams, unity_SpecCube0, samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    renderingEquationContent.indirectLightSpecular += CalculateIBL_Specular_RemappedMipmap(standardPBRParams, unity_SpecCube0,
        samplerunity_SpecCube0, envBRDF.rg, energyCompensation);

    // ----------------------------------------------------------------------------------------------------
    // Direct Lighting - Sun Light
    // ----------------------------------------------------------------------------------------------------
    LightParams sunLightParams = (LightParams) 0;
    InitializeSunLightParams(sunLightParams, LIGHTMAP_UV_FRAGMENT(IN), standardPBRParams.V, normalize(IN.normalWS), IN.positionWS);

    BRDFParams sunBRDFParams = (BRDFParams) 0;
    InitializeBRDFParams(sunBRDFParams, standardPBRParams.N, sunLightParams.L, standardPBRParams.V, sunLightParams.H);

    renderingEquationContent.directSunLight += CalculateLightIrradiance(sunLightParams) * StandardPBR_EnergyCompensation(sunBRDFParams, standardPBRParams, energyCompensation);
    
    // ----------------------------------------------------------------------------------------------------
    // Direct Lighting - Punctual Light
    // ----------------------------------------------------------------------------------------------------
    int spotLightCount = GetSpotLightCount();
    
    for (int i = 0; i < spotLightCount; i++)
    {
        LightParams spotLightParams = (LightParams) 0;
        InitializeSpotLightParams(spotLightParams, i, LIGHTMAP_UV_FRAGMENT(IN), standardPBRParams.V, normalize(IN.normalWS), IN.positionWS);
        
        BRDFParams spotBRDFParams = (BRDFParams) 0;
        InitializeBRDFParams(spotBRDFParams, standardPBRParams.N, spotLightParams.L, standardPBRParams.V, spotLightParams.H);
        
        renderingEquationContent.directPunctualLights += CalculateLightIrradiance(spotLightParams) * StandardPBR_EnergyCompensation(spotBRDFParams, standardPBRParams, energyCompensation);
    }

    int pointLightCount = GetPointLightCount();

    for (int j = 0; j < pointLightCount; j++)
    {
        LightParams pointLightParams = (LightParams) 0;
        InitializePointLightParams(pointLightParams, j, LIGHTMAP_UV_FRAGMENT(IN), standardPBRParams.V, normalize(IN.normalWS), IN.positionWS);

        BRDFParams pointBRDFParams = (BRDFParams) 0;
        InitializeBRDFParams(pointBRDFParams, standardPBRParams.N, pointLightParams.L, standardPBRParams.V, pointLightParams.H);

        renderingEquationContent.directPunctualLights += CalculateLightIrradiance(pointLightParams) * StandardPBR_EnergyCompensation(pointBRDFParams, standardPBRParams, energyCompensation);
    }

    // ----------------------------------------------------------------------------------------------------
    // LOD Fade
    // ----------------------------------------------------------------------------------------------------
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    
    return float4(renderingEquationContent.directSunLight + renderingEquationContent.directPunctualLights + renderingEquationContent.indirectLightDiffuse + renderingEquationContent.indirectLightSpecular, 1.0);
}

#endif