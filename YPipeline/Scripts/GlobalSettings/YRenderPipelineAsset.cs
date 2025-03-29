using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace YPipeline
{
    [CreateAssetMenu(menuName = "YPipeline/YRenderPipelineAsset")]
    public partial class YRenderPipelineAsset : RenderPipelineAsset<YRenderPipeline>
    {
        // ----------------------------------------------------------------------------------------------------
        // RenderPipelineAsset
        // ----------------------------------------------------------------------------------------------------
        
        public override string renderPipelineShaderTag => string.Empty;
        public override Shader defaultShader => Shader.Find("YPipeline/PBR/Standard Forward");

        protected override RenderPipeline CreatePipeline ()
        {
            return new YRenderPipeline(this);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // 渲染设置
        // ----------------------------------------------------------------------------------------------------
        // TODO：参考 HDRP 的 Asset
        [FoldoutGroup("Rendering Settings", expanded: true)]
        public bool enableHDRFrameBufferFormat = true;

        [FoldoutGroup("Rendering Settings")]
        public YPipelineResources pipelineResources;
        
        // --------------------------------------------------------------------------------
        // 渲染路径配置
        public enum RenderPath
        {
            Forward, Deferred, Custom
        }
        
        [FoldoutGroup("Render Path Settings", expanded: true)]
        public RenderPath renderPath = RenderPath.Forward;
        
        [FoldoutGroup("Render Path Settings")]
        public List<PipelineNode> currentPipelineNodes = new List<PipelineNode>();

        public void PresetRenderPaths()
        {
            currentPipelineNodes.Clear();
            switch (renderPath)
            {
                case RenderPath.Forward: 
                    currentPipelineNodes.Add(PipelineNode.Create<ForwardLightingNode>());
                    currentPipelineNodes.Add(PipelineNode.Create<ForwardGeometryNode>());
                    currentPipelineNodes.Add(PipelineNode.Create<SkyboxNode>());
                    currentPipelineNodes.Add(PipelineNode.Create<TransparencyNode>());
                    currentPipelineNodes.Add(PipelineNode.Create<PostProcessingNode>());
                    break;
                case RenderPath.Deferred:
                    currentPipelineNodes = new List<PipelineNode>()
                    {

                    };
                    break;
                case RenderPath.Custom:
                    currentPipelineNodes = new List<PipelineNode>()
                    {

                    };
                    break;
            }
        }
        
        // --------------------------------------------------------------------------------
        // 后处理配置
        [FoldoutGroup("Post Processing Settings", expanded: true)]
        public VolumeProfile globalVolumeProfile;
        
        [FoldoutGroup("Post Processing Settings")]
        [ValueDropdown("m_LutSizes")] public int bakedLUTResolution = 32;
        
        // --------------------------------------------------------------------------------
        // 合批配置
        [FoldoutGroup("Batching Settings", expanded: true)]
        public bool enableSRPBatcher = true;
        
        [FoldoutGroup("Batching Settings")]
        public bool enableGPUInstancing = true;
        
        
        // --------------------------------------------------------------------------------
        // 其他共用字段
        private static int[] m_TextureSizes = new int[] { 256, 512, 1024, 2048, 4096, 8192 };
        private static int[] m_SampleNumbers = new int[] { 1, 4, 8, 16, 32, 64, 128 };
        private static int[] m_LutSizes = new int[] { 16, 32, 64 };
    }
}