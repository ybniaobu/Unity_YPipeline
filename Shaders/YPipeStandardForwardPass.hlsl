#ifndef YPIPELINE_STANDARD_INPUT_INCLUDED
#define YPIPELINE_STANDARD_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/ShaderLibrary/RenderingEquationLibrary.hlsl"
#include "Assets/ShaderLibrary/BRDFTermsLibrary.hlsl"
#include "Assets/ShaderLibrary/IBLLibrary.hlsl"
#include "Assets/ShaderLibrary/BRDFModelLibrary.hlsl"

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
TextureCube _PrefilteredEnvMap; SamplerState sampler_Trilinear_Repeat_PrefilteredEnvMap;
Texture2D _EnvBRDFLut;          SamplerState sampler_Point_Clamp_EnvBRDFLut;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float3 tangentWS    : TEXCOORD3;
    float3 binormalWS   : TEXCOORD4;
    // float3 SH           : TEXCOORD4;
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
    
    standardPBRParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), standardPBRParams.albedo, standardPBRParams.metallic);
    standardPBRParams.V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
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
    return OUT;
}

float4 StandardFrag(Varyings IN) : SV_TARGET
{
    RenderingEquationContent renderingEquationContent = (RenderingEquationContent) 0;
    
    StandardPBRParams standardPBRParams = (StandardPBRParams) 0;
    InitializeStandardPBRParams(IN, standardPBRParams);

    // --------------------------------------------------------------------------------
    // IBL
    // float3 irradiance = SampleSH(standardPBRParams.N) / PI;
    float3 irradiance = SAMPLE_TEXTURE2D_LOD(_PrefilteredEnvMap, sampler_Trilinear_Repeat_PrefilteredEnvMap, standardPBRParams.N, 8).rgb / PI;
    float3 envBRDFDiffuse = standardPBRParams.albedo * SAMPLE_TEXTURE2D(_EnvBRDFLut, sampler_Point_Clamp_EnvBRDFLut, float2(standardPBRParams.NoV, standardPBRParams.roughness)).b;
    float3 Ks = F_Schlick(1, standardPBRParams.F0, standardPBRParams.NoV);
    //float3 Ks = F_SchlickRoughness(standardPBRParams.F0, standardPBRParams.NoV, standardPBRParams.roughness);
    //float Kd = 1.0 - (Ks.r + Ks.g + Ks.b)/3;
    float3 Kd = float3(1.0, 1.0, 1.0) - Ks;
    Kd *= 1.0 - standardPBRParams.metallic;
    renderingEquationContent.indirectLight += irradiance * envBRDFDiffuse * Kd;

    float3 R = reflect(-standardPBRParams.V, standardPBRParams.N);
    float3 prefilteredColor = SAMPLE_TEXTURE2D_LOD(_PrefilteredEnvMap, sampler_Trilinear_Repeat_PrefilteredEnvMap, R, 8.0 * standardPBRParams.roughness).rgb;
    float2 envBRDFSpecular = SAMPLE_TEXTURE2D(_EnvBRDFLut, sampler_Point_Clamp_EnvBRDFLut, float2(standardPBRParams.NoV, standardPBRParams.roughness)).rg;
    renderingEquationContent.indirectLight += prefilteredColor * (standardPBRParams.F0 * envBRDFSpecular.x + envBRDFSpecular.y);
    //renderingEquationContent.indirectLight += prefilteredColor * lerp(envBRDFSpecular.xxx, envBRDFSpecular.yyy, standardPBRParams.F0);

    float3 energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDFSpecular.y - 1.0);

    // --------------------------------------------------------------------------------
    // Punctual Lights
    LightParams mainLightParams = (LightParams) 0;
    InitializeMainLightParams(mainLightParams, standardPBRParams.V);
    
    BRDFParams mainBRDFParams = (BRDFParams) 0;
    InitializeBRDFParams(mainBRDFParams, standardPBRParams.N, mainLightParams.L, standardPBRParams.V, mainLightParams.H);
    
    renderingEquationContent.directMainLight = mainLightParams.color * StandardPBR(mainBRDFParams, standardPBRParams);
    
    #ifdef _ADDITIONAL_LIGHTS
        int additionalLightsCount = GetAdditionalLightCount();
    
        for (int i = 0; i < additionalLightsCount; ++i)
        {
            int lightIndex = GetAdditionalLightIndex(i);
            LightParams additionalLightParams = (LightParams) 0;
            InitializeAdditionalLightParams(additionalLightParams, lightIndex, standardPBRParams.V, IN.positionWS);
            
            BRDFParams additionalBRDFParams = (BRDFParams) 0;
            InitializeBRDFParams(additionalBRDFParams, standardPBRParams.N, additionalLightParams.L, standardPBRParams.V, additionalLightParams.H);
    
            float3 additionalLightColor = additionalLightParams.color * (additionalLightParams.distanceAttenuation * additionalLightParams.angleAttenuation);
            renderingEquationContent.directAdditionalLight += additionalLightColor * StandardPBR(additionalBRDFParams, standardPBRParams);
        }
    #endif

    //return float4(renderingEquationContent.directMainLight + renderingEquationContent.directAdditionalLight, 1.0f);
    return float4(renderingEquationContent.directMainLight + renderingEquationContent.directAdditionalLight + renderingEquationContent.indirectLight, 1.0f);
}

#endif