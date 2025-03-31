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

            #pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION
            #pragma multi_compile_local_fragment _ _VIGNETTE
            #pragma multi_compile_local_fragment _ _EXTRA_LUT
            #pragma multi_compile_local_fragment _ _FILM_GRAIN
            
            ENDHLSL
        }
    }
}