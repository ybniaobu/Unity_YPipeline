#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// --------------------------------------------------------------------------------
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// --------------------------------------------------------------------------------
// Constant Buffers
CBUFFER_START(PunctualLights)
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    int _DirectionalLightCount;
    uint _DirectionalLightLayerMask;
CBUFFER_END

TEXTURE2D_SHADOW(_DirectionalShadowMap);
SAMPLER_CMP(sampler_linear_clamp_compare_DirectionalShadowMap);

CBUFFER_START(Shadows)
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
    float4 _ShadowBias;
    float4 _CascadeParams;
CBUFFER_END


#endif