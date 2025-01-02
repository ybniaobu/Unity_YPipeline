#ifndef YPIPELINE_SHADOW_CASTER_PASS_INCLUDED
#define YPIPELINE_SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "../ShaderLibrary/ShadowsLibrary.hlsl"

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
    OUT.positionHCS.z = min(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    OUT.positionHCS.z = max(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    
    return OUT;
}

void ShadowCasterFrag(Varyings input)
{
    
}

#endif