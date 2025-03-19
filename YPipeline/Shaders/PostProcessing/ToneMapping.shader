Shader "Hidden/YPipeline/ToneMapping"
{
    HLSLINCLUDE
    #include "ToneMappingPass.hlsl"
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
            Name "ToneMapping Reinhard Simple" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingReinhardSimpleFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Reinhard Extended" // 1
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingReinhardExtendedFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Reinhard Luminance" // 2
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingReinhardLuminanceFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Uncharted2 Filmic" // 3
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingUncharted2FilmicFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Khronos PBR Neutral" // 4
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingKhronosPBRNeutralFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping ACES Full" // 5
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingACESFullFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping ACES Stephen Hill Fit" // 6
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingACESStephenHillFitFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping ACES Approximation Fit" // 7
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingACESApproxFitFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping AgX Approximation Default" // 8
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingAgXDefaultFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping AgX Approximation Golden" // 9
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingAgXGoldenFrag
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping AgX Approximation Punchy" // 10
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment ToneMappingAgXPunchyFrag
            ENDHLSL
        }
    }
}