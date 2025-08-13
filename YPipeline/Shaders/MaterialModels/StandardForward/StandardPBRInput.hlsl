#ifndef YPIPELINE_STANDARD_PBR_INPUT_INCLUDED
#define YPIPELINE_STANDARD_PBR_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Specular;
    float _Roughness;
    float _RoughnessScale;
    float _Metallic;
    float _MetallicScale;
    float _NormalIntensity;
    float _AOScale;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;
Texture2D _HybridTex;
Texture2D _NormalTex;

#endif