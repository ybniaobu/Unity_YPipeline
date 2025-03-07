Shader "Hidden/YPipeline/Copy"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Copy"
            
            HLSLPROGRAM
            #pragma target 3.5
            
            #pragma vertex CopyVert
            #pragma fragment CopyFrag

            #include "CopyPass.hlsl"
            ENDHLSL
        }
    }
}