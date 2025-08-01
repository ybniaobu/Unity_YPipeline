Shader "YPipeline/Skybox/PureColor"
{
    Properties
    {
        [MainColor] [HDR] _Color("Skybox Color", Color) = (0.1, 0.1, 0.1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
        }
        
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"

            float4 _Color;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                return float4(_Color.xyz, 1.0);
            }
            ENDHLSL
        }
    }
}