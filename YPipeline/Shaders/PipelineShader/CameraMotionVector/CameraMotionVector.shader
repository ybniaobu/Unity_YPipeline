﻿Shader "Hidden/YPipeline/CameraMotionVector"
{
    HLSLINCLUDE
    #include "CameraMotionVectorPass.hlsl"
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
            Name "Motion Vector"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment CameraMotionVectorFrag
            ENDHLSL
        }
    }
}