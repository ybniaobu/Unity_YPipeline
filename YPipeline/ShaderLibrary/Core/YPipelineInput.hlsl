#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Macros
#define MAX_DIRECTIONAL_LIGHT_COUNT 1 // Only Support One Directional Light - Sunlight
#define MAX_CASCADE_COUNT 4

// ----------------------------------------------------------------------------------------------------
// Constant Buffers
CBUFFER_START(DirectLighting)
    // Sun Light
    float4 _SunLightColor; // xyz: color * intensity, w: shadow strength
    float4 _SunLightDirection; // xyz: sunlight direction, w: 0
    float4 _SunLightShadowParams; // x: cascade count, y: shadow array size, z: sample number, w: penumbra width
    float4 _SunLightShadowFadeParams; // x: 1.0 / max shadow distance, y: 1.0 / distance fade, z: 1.0 / cascade edge fade, w: 0
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _SunLightShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];

    // Punctual Lights
    

    // Other Shared Params
    float4 _ShadowBias;
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
TEXTURE2D_ARRAY_SHADOW(_SunLightShadowArray);
SAMPLER_CMP(sampler_LinearClampCompare);


#endif