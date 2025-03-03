using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace YPipeline
{
    [CreateAssetMenu(menuName = "YPipeline/YRenderPipelineAsset")]
    public partial class YRenderPipelineAsset : RenderPipelineAsset<YRenderPipeline>
    {
        // --------------------------------------------------------------------------------
        // RenderPipelineAsset
        public override string renderPipelineShaderTag => string.Empty;
        public override Shader defaultShader => Shader.Find("YPipeline/PBR/Standard Forward");

        protected override RenderPipeline CreatePipeline ()
        {
            return new YRenderPipeline(this);
        }
        
        // --------------------------------------------------------------------------------
        // 渲染路径配置
        public enum RenderPath
        {
            Forward, Deferred, Custom
        }
        
        [FoldoutGroup("Render Path Settings", expanded: true)]
        [PropertyOrder(-1)] public RenderPath renderPath = RenderPath.Forward;
        
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
        public float a = 0;
        
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
    }
}