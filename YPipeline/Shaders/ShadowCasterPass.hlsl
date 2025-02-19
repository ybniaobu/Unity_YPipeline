#ifndef YPIPELINE_SHADOW_CASTER_PASS_INCLUDED
#define YPIPELINE_SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Core/YPipelineCore.hlsl"

CBUFFER_START(PerShadowDraw)
    float _ShadowPancaking;
CBUFFER_END

struct Attributes
{
    float4 positionOS   : POSITION;
    // float3 normalOS     : NORMAL;
    // float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    //float2 uv           : TEXCOORD0;
};

Varyings ShadowCasterVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

    #if UNITY_REVERSED_Z
        float clamped = min(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        float clamped = max(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    OUT.positionHCS.z = lerp(OUT.positionHCS.z, clamped, _ShadowPancaking);
    
    return OUT;
}

void ShadowCasterFrag(Varyings input)
{
    
}

#endif