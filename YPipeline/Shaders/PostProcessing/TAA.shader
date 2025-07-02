Shader "Hidden/YPipeline/TAA"
{
    HLSLINCLUDE
    #include "TAAPass.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        ZTest Always
        ZWrite Off
        Blend Off
        Cull Off

        Pass
        {
            Name "TAA"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment TAAFrag
            ENDHLSL
        }
    }
}