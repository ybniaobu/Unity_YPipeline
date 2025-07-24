#ifndef YPIPELINE_STANDARD_PBR_FOR_TEST_INPUT_INCLUDED
#define YPIPELINE_STANDARD_PBR_FOR_TEST_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Specular;
    float _Roughness;
    float _Metallic;
    float _NormalIntensity;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _EmissionTex;
Texture2D _RoughnessTex;
Texture2D _MetallicTex;
Texture2D _NormalTex;
Texture2D _AOTex;

#endif