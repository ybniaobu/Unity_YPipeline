Shader "YPipeline/PBR/Standard Forward (Separated Texture)"
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
        [Toggle(_USE_ROUGHNESSTEX)] _UseRoughnessTex("use roughness texture?", Float) = 0
        [NoScaleOffset] _RoughnessTex("Roughness Texture", 2D) = "white" {}
        
        [Header(Metallic Settings)] [Space(8)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle(_USE_METALLICTEX)] _UseMetallicTex("use metallic texture?", Float) = 0
        [NoScaleOffset] _MetallicTex("Metallic Texture", 2D) = "white" {}
        
        [Header(Normal Settings)] [Space(8)]
        [Toggle(_USE_NORMALTEX)] _UseNormalTex("use normal texture?", Float) = 0
        [NoScaleOffset] [Normal] _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Float) = 1.0
        
        [Header(Ambient Occlusion Settings)] [Space(8)]
        [NoScaleOffset] _AOTex("Ambient Occlusion Texture", 2D) = "white" {}
    	
	    [Header(Emission Settings)] [Space(8)]
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "white" {}
        
    	[Header(Alpha Clipping Settings)] [Space(8)]
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
    	[NoScaleOffset] _OpacityTex("Opacity Texture", 2D) = "white" {}
        
        [Header(Other Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }
    
    SubShader
    {
        Tags
        {
            "Queue"= "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "StandardForward"
            
            Tags { "LightMode" = "YPipelineForward" }
            
            Blend One Zero
            ZWrite Off
            ZTest Equal
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex StandardVert
            #pragma fragment StandardFrag
            
            #pragma shader_feature_local_fragment _USE_ROUGHNESSTEX
            #pragma shader_feature_local_fragment _USE_METALLICTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX
            #pragma shader_feature_local_fragment _CLIPPING
            
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _SHADOW_PCF _SHADOW_PCSS

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #include "StandardForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
        	Name "ShadowCaster"
        	
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0
			Cull [_Cull]
			// Cull Back
			// Cull Front

			HLSLPROGRAM
			#pragma target 4.5
			
			#pragma vertex ShadowCasterVert
			#pragma fragment ShadowCasterFrag

			#pragma shader_feature_local_fragment _CLIPPING
			
			#include "../ShadowCasterPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Depth"
			
			Tags { "LightMode" = "Depth" }
			
			ZWrite On
			ColorMask 0
			Cull [_Cull]
			
			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex DepthVert
			#pragma fragment DepthFrag

			#pragma shader_feature_local_fragment _CLIPPING

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "StandardForwardDepthPass.hlsl"
			ENDHLSL
		}
		
        Pass
        {
        	Name "Meta"
        	
			Tags { "LightMode" = "Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma target 4.5
			
			#pragma vertex MetaVert
			#pragma fragment MetaFrag
			
			#include "StandardForwardMetaPass.hlsl"
			ENDHLSL
		}
    }

    CustomEditor "YPipeline.Editor.StandardForwardShaderGUI"
}
