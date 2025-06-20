using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    public enum RenderPath
    {
        Forward, Deferred, Custom
    }
    
    public enum AntiAliasingMode
    {
        None, FXAA, TAA
    }

    public enum FXAAMode
    {
        Quality, Console
    }
    
    public enum ShadowMode
    {
        PCF, PCSS
    }

    public enum ResolutionSize
    {
        [InspectorName("512")] _512 = 512,
        [InspectorName("1024")] _1024 = 1024,
        [InspectorName("2048")] _2048 = 2048,
        [InspectorName("4096")] _4096 = 4096,
    }
    
    [CreateAssetMenu(menuName = "YPipeline/YRenderPipelineAsset")]
    public class YRenderPipelineAsset : RenderPipelineAsset<YRenderPipeline>
    {
        // ----------------------------------------------------------------------------------------------------
        // RenderPipelineAsset
        // ----------------------------------------------------------------------------------------------------
        
        public override string renderPipelineShaderTag => string.Empty;
        public override Shader defaultShader => Shader.Find("YPipeline/PBR/Standard Forward (Separated Texture)");

        protected override RenderPipeline CreatePipeline()
        {
            return new YRenderPipeline(this);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // 渲染配置
        // ----------------------------------------------------------------------------------------------------
        
        // TODO：参考 HDRP 的 Asset
        [Header("Rendering Settings")]
        public YPipelineResources pipelineResources;
        
        public RenderPath renderPath = RenderPath.Forward;
        
        public bool enableHDRColorBuffer = true;
        
        public bool enableSRPBatcher = true;
        
        [Range(0.1f, 2f)] public float renderScale = 1.0f;
        
        public AntiAliasingMode antiAliasingMode = AntiAliasingMode.FXAA;
        
        public FXAAMode fxaaMode = FXAAMode.Quality;
        
        // ----------------------------------------------------------------------------------------------------
        // 光照配置
        // ----------------------------------------------------------------------------------------------------
        [Header("Lighting Settings")]
        [Tooltip("Enable light 2.5D culling, which splits depth into cells to better handle depth discontinuities")]
        public bool enableSplitDepth = true;
        
        // ----------------------------------------------------------------------------------------------------
        // 阴影配置
        // ----------------------------------------------------------------------------------------------------
        
        [Header("Shadow Settings")]
        public ShadowMode shadowMode = ShadowMode.PCSS;
        
        public float maxShadowDistance = 100.0f;
        
        [Range(0f, 1f)] public float distanceFade = 0.1f;
        
        [Range(1, 4)] public int cascadeCount = 4;
        
        [SerializeField]
        [Range(0f, 1f)] private float spiltRatio1 = 0.15f, spiltRatio2 = 0.3f, spiltRatio3 = 0.6f;
        public Vector3 SpiltRatios => new Vector3(spiltRatio1, spiltRatio2, spiltRatio3);
        
        [Range(0f, 1f)] public float cascadeEdgeFade = 0.05f;
        
        public ResolutionSize sunLightShadowMapSize = ResolutionSize._4096;
        public ResolutionSize spotLightShadowMapSize = ResolutionSize._1024;
        public ResolutionSize pointLightShadowMapSize = ResolutionSize._1024;
        
        // ----------------------------------------------------------------------------------------------------
        // 后处理配置
        // ----------------------------------------------------------------------------------------------------
        
        [Header("Post Processing Settings")]
        public VolumeProfile globalVolumeProfile;
        
        public int bakedLUTResolution = 32;
    }
}