Shader "Hidden/Preview/ReflectionProbePreview"
{
    Properties
    {
        _Cubemap("_Cubemap", Cube) = "white" {}
        _MipLevel("_MipLevel", Range(0.0, 7.0)) = 0.0
        _Exposure("_Exposure", Range(-16.0, 16.0)) = 0.0
    }
    
    SubShader
    {
        ZWrite On
        Cull Back

        Pass
        {
            Name "ReflectionProbePreview"
            
            HLSLPROGRAM
            
            #pragma editor_sync_compilation
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Assets/YPipeline/ShaderLibrary/Core/UnityInput.hlsl"
            #include "Assets/YPipeline/ShaderLibrary/Core/UnityMatrix.hlsl"
            #include "Assets/YPipeline/ShaderLibrary/Core/YPipelineMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };
            
            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);
            float4 _Cubemap_HDR;

            float3 _CameraWorldPosition;
            float _MipLevel;
            float _Exposure;
            
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }
            
            float4 Frag(Varyings IN) : SV_TARGET
            {
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 R = reflect(-V, normalize(IN.normalWS));
                float4 color = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, R, _MipLevel).rgba;
                color.rgb = DecodeHDREnvironment(color, _Cubemap_HDR);
                color = color * exp2(_Exposure);
                return color;
            }
            ENDHLSL
        }
    }
}