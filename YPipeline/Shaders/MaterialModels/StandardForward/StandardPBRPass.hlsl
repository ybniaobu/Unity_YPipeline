#ifndef YPIPELINE_STANDARD_PBR_PASS_INCLUDED
#define YPIPELINE_STANDARD_PBR_PASS_INCLUDED

#include "../../../ShaderLibrary/RenderingEquationLibrary.hlsl"

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
    float4 tangentWS    : TEXCOORD3;
    LIGHTMAP_UV(5)
};

void InitializeGeometryParams(Varyings IN, out GeometryParams geometryParams)
{
    geometryParams.positionWS = IN.positionWS;
    
    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, IN.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = normalize(IN.normalWS);
        float3 t = normalize(IN.tangentWS.xyz);
        float3 b = normalize(cross(n, t) * IN.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        geometryParams.normalWS = normalize(mul(normalTS, tbn));
    #else
        geometryParams.normalWS = normalize(IN.normalWS);
    #endif
    
    geometryParams.uv = IN.uv;
    geometryParams.pixelCoord = IN.positionHCS.xy;
    geometryParams.screenUV = geometryParams.pixelCoord * _CameraBufferSize.xy;
    TRANSFER_GEOMETRY_PARAMS_LIGHTMAP_UV
}

void InitializeStandardPBRParams(in GeometryParams geometryParams, out StandardPBRParams standardPBRParams)
{
    float4 color = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, geometryParams.uv).rgba * _BaseColor.rgba;
    standardPBRParams.albedo = color.rgb;
    standardPBRParams.alpha = color.a;
    standardPBRParams.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, geometryParams.uv).rgb * _EmissionColor.rgb;
    
    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, geometryParams.uv).rgba;
        standardPBRParams.roughness = saturate(hybrid.r * pow(10, _RoughnessScale));
        standardPBRParams.metallic = saturate(hybrid.g * pow(10, _MetallicScale));
        standardPBRParams.ao = saturate(hybrid.a * pow(0.1, _AOScale));
    #else
        standardPBRParams.roughness = _Roughness;
        standardPBRParams.metallic = _Metallic;
        standardPBRParams.ao = 1.0;
    #endif

    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
        standardPBRParams.ao = min(standardPBRParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geometryParams.screenUV, 0).r);
    #endif
    
    standardPBRParams.N = geometryParams.normalWS;
    standardPBRParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), standardPBRParams.albedo, standardPBRParams.metallic);
    standardPBRParams.F90 = saturate(dot(standardPBRParams.F0, 50.0 * 0.3333));
    standardPBRParams.V = GetWorldSpaceNormalizedViewDir(geometryParams.positionWS);
    standardPBRParams.R = reflect(-standardPBRParams.V, standardPBRParams.N);
    standardPBRParams.NoV = saturate(dot(standardPBRParams.N, standardPBRParams.V)) + 1e-3; //防止小黑点
}

Varyings StandardPBRVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    TRANSFER_LIGHTMAP_UV(IN, OUT)
    return OUT;
}

float4 StandardPBRFrag(Varyings IN) : SV_TARGET
{
    // ------------------------- Clipping -------------------------
    
    #if defined(_CLIPPING)
        clip(standardPBRParams.alpha - _Cutoff);
    #endif
    
    // ------------------------- LOD Fade -------------------------
    
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif
    
    // ------------------------- PBR Parameters -------------------------
    
    RenderingEquationContent renderingEquationContent = (RenderingEquationContent) 0;
    
    GeometryParams geometryParams = (GeometryParams) 0;
    InitializeGeometryParams(IN, geometryParams);
    
    StandardPBRParams standardPBRParams = (StandardPBRParams) 0;
    InitializeStandardPBRParams(geometryParams, standardPBRParams);
    
    // ------------------------- Indirect Lighting -------------------------
    
    float3 envBRDF = SampleEnvLut(ENVIRONMENT_BRDF_LUT, LUT_SAMPLER, standardPBRParams.NoV, standardPBRParams.roughness);
    float3 energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) * 0.5; // 0.5 is a magic number
    
    renderingEquationContent.indirectLightDiffuse += CalculateDiffuseIndirectLighting(geometryParams, standardPBRParams, envBRDF.b);

    //renderingEquationContent.indirectLightSpecular += CalculateIBL_Specular(standardPBRParams, unity_SpecCube0, samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    renderingEquationContent.indirectLightSpecular += CalculateIBL_Specular_RemappedMipmap(standardPBRParams, unity_SpecCube0,
        samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    
    // ------------------------- Direct Lighting - Sun Light -------------------------
    
    LightParams sunLightParams = (LightParams) 0;
    InitializeSunLightParams(sunLightParams, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);

    BRDFParams sunBRDFParams = (BRDFParams) 0;
    InitializeBRDFParams(sunBRDFParams, standardPBRParams.N, sunLightParams.L, standardPBRParams.V, sunLightParams.H);

    renderingEquationContent.directSunLight += CalculateLightIrradiance(sunLightParams) * StandardPBR_EnergyCompensation(sunBRDFParams, standardPBRParams, energyCompensation);
    
    // ------------------------- Direct Lighting - Punctual Light -------------------------

    LightsTileParams lightsTileParams = (LightsTileParams) 0;
    InitializeLightsTileParams(lightsTileParams, geometryParams.pixelCoord);
    
    for (int i = lightsTileParams.headerIndex + 1; i <= lightsTileParams.lastLightIndex; i++)
    {
        uint lightIndex = _TilesLightIndicesBuffer[i];
        
        LightParams punctualLightParams = (LightParams) 0;
        
        UNITY_BRANCH
        if (GetPunctualLightType(lightIndex) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, lightIndex, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
        else if (GetPunctualLightType(lightIndex) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, lightIndex, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    
        BRDFParams punctualBRDFParams = (BRDFParams) 0;
        InitializeBRDFParams(punctualBRDFParams, standardPBRParams.N, punctualLightParams.L, standardPBRParams.V, punctualLightParams.H);
    
        renderingEquationContent.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardPBR_EnergyCompensation(punctualBRDFParams, standardPBRParams, energyCompensation);
    }
    
    // int punctualLightCount = GetPunctualLightCount();
    //
    // for (int i = 0; i < punctualLightCount; i++)
    // {
    //     LightParams punctualLightParams = (LightParams) 0;
    //     
    //     UNITY_BRANCH
    //     if (GetPunctualLightType(i) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, i, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    //     else if (GetPunctualLightType(i) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, i, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    //     
    //     BRDFParams punctualBRDFParams = (BRDFParams) 0;
    //     InitializeBRDFParams(punctualBRDFParams, standardPBRParams.N, punctualLightParams.L, standardPBRParams.V, punctualLightParams.H);
    //     
    //     renderingEquationContent.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardPBR_EnergyCompensation(punctualBRDFParams, standardPBRParams, energyCompensation);
    // }
    
    return float4(renderingEquationContent.directSunLight + renderingEquationContent.directPunctualLights + renderingEquationContent.indirectLightDiffuse + renderingEquationContent.indirectLightSpecular + standardPBRParams.emission, 1.0);
}

#endif