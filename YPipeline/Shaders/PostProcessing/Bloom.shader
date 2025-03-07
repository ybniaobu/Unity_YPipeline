Shader "Hidden/YPipeline/Bloom"
{
    HLSLINCLUDE
    #include "BloomPass.hlsl"
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
            Name "Bloom Gaussian Blur Horizontal"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BloomVert
            #pragma fragment BloomGaussianBlurHorizontalFrag
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Gaussian Blur Vertical"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BloomVert
            #pragma fragment BloomGaussianBlurVerticalFrag
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Upsample"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BloomVert
            #pragma fragment BloomUpsampleFrag
            #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Prefilter"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BloomVert
            #pragma fragment BloomPrefilterFrag
            // #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            ENDHLSL
        }
    }
}
