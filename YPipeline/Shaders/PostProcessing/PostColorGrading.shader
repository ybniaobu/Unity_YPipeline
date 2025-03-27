Shader "Hidden/YPipeline/PostColorGrading"
{
    HLSLINCLUDE
    #include "PostColorGradingPass.hlsl"
    ENDHLSL

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
            Name "Post Color Grading" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment PostColorGradingFrag
            ENDHLSL
        }
    }
}