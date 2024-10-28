Shader "YPipeline/PBR/StandardEnergyCompensation"
{
    Properties
    {
        [Header(Base Color Settings)] [Space(8)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        [Header(Specular Color Settings)] [Space(8)]
        _Specular("Dielectrics Specular Intensity", Range(0.0, 1.0)) = 0.5
        
        [Header(Roughness Settings)] [Space(8)]
        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5
        [Toggle(_USE_ROUGHNESSTEX)] _UseRoughnessTex("Whether use roughness texture", Float) = 0
        [NoScaleOffset] _RoughnessTex("Roughness Texture", 2D) = "white" {}
        
        [Header(Metallic Settings)] [Space(8)]
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle(_USE_METALLICTEX)] _UseMetallicTex("Whether use metallic texture", Float) = 0
        [NoScaleOffset] _MetallicTex("Metallic Texture", 2D) = "white" {}
        
        [Header(Normal Settings)] [Space(8)]
        [Toggle(_USE_NORMALTEX)] _UseNormalTex("Whether use normal texture", Float) = 0
        [NoScaleOffset] [Normal] _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Float) = 1.0
        
        [Header(Ambient Occlusion Settings)] [Space(8)]
        [NoScaleOffset] _AOTex("Ambient Occlusion Texture", 2D) = "white" {}
        
        [Header(Other Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"= "Geometry"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "StandardForward"
            
            Tags { "LightMode" = "UniversalForward" }
            
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma vertex StandardVert
            #pragma fragment StandardFrag
            
            #pragma shader_feature_local_fragment _USE_ROUGHNESSTEX
            #pragma shader_feature_local_fragment _USE_METALLICTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            
            #include "StandardEnergyCompensationPass.hlsl"
            ENDHLSL
        }
    }
}