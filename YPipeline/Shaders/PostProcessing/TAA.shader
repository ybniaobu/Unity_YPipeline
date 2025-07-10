Shader "Hidden/YPipeline/TAA"
{
    HLSLINCLUDE
    #pragma target 3.5
    
    #pragma multi_compile_local  _ _TAA_SAMPLE_3X3
    
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
            #pragma vertex CopyVert
            #pragma fragment TAAFrag
            ENDHLSL
        }
    }
}