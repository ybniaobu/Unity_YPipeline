#ifndef YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED
#define YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED

#include "../../ShaderLibrary/EncodingLibrary.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ThinGBufferVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv * baseST.xy + baseST.zw;
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

void ThinGBufferFrag(Varyings IN, out float depth: SV_DEPTH, out float4 normal: SV_TARGET0, out float4 reflectanceAndRoughness : SV_TARGET1)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv) * baseColor;

    #if defined(_CLIPPING)
        clip(albedo.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    #if _USE_ROUGHNESSTEX
        float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
        roughness *= pow(10, _RoughnessScale);
        roughness = saturate(roughness);
    #else
        float roughness = _Roughness;
    #endif

    #if _USE_METALLICTEX
        float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).r;
        metallic *= pow(10, _MetallicScale);
        metallic = saturate(metallic);
    #else
        float metallic = _Metallic;
    #endif

    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgba;
        roughness = saturate(hybrid.r * pow(10, _RoughnessScale));
        metallic = saturate(hybrid.g * pow(10, _MetallicScale));
    #endif

    float3 F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), albedo.rgb, metallic);

    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_Trilinear_Repeat_BaseTex, IN.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = normalize(IN.normalWS);
        float3 t = normalize(IN.tangentWS.xyz);
        float3 b = normalize(cross(n, t) * IN.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        float3 N = normalize(mul(normalTS, tbn));
    #else
        float3 N = normalize(IN.normalWS);
    #endif

    depth = IN.positionHCS.z;
    normal = float4(N * 0.5 + 0.5, 0);
    reflectanceAndRoughness = float4(F0, roughness);
}

#endif