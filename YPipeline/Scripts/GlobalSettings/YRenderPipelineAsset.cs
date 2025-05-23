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
        // 渲染设置
        // ----------------------------------------------------------------------------------------------------
        
        // TODO：参考 HDRP 的 Asset
        [Header("Rendering Settings")]
        public bool enableHDRFrameBufferFormat = true;
        
        [Range(0.1f, 2f)] public float renderScale = 1.0f;
        
        public AntiAliasingMode antiAliasingMode = AntiAliasingMode.FXAA;
        
        public FXAAMode fxaaMode = FXAAMode.Quality;
        
        public YPipelineResources pipelineResources;
        
        // ----------------------------------------------------------------------------------------------------
        // 渲染路径配置
        
        [Header("Render Path Settings")]
        public RenderPath renderPath = RenderPath.Forward;
        
        // ----------------------------------------------------------------------------------------------------
        // 后处理配置
        
        [Header("Post Processing Settings")]
        public VolumeProfile globalVolumeProfile;
        
        public int bakedLUTResolution = 32;
        
        // ----------------------------------------------------------------------------------------------------
        // 合批配置
        
        [Header("Batching Settings")]
        public bool enableSRPBatcher = true;
        
        // ----------------------------------------------------------------------------------------------------
        // 阴影配置
        
        [Header("Shadows Settings")]
        public ShadowMode shadowMode = ShadowMode.PCSS;
        
        public float maxShadowDistance = 80.0f;
        
        [Range(0f, 1f)] public float distanceFade = 0.1f;
        
        [Range(1, 4)] public int cascadeCount = 4;
        
        [SerializeField]
        [Range(0f, 1f)] private float spiltRatio1 = 0.15f, spiltRatio2 = 0.3f, spiltRatio3 = 0.6f;
        public Vector3 SpiltRatios => new Vector3(spiltRatio1, spiltRatio2, spiltRatio3);
        
        [Range(0f, 1f)] public float cascadeEdgeFade = 0.05f;
        
        public int sunLightShadowMapSize = 2048;
        public int spotLightShadowMapSize = 1024;
        public int pointLightShadowMapSize = 1024;
    }
}