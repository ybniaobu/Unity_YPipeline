Shader "Hidden/YPipeline/ColorGrading"
{
    HLSLINCLUDE
    #include "ColorGradingPass.hlsl"
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
            Name "Color Grading" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ColorGradingFrag
            ENDHLSL
        }
    }
}