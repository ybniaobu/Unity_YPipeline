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
        
        protected override RenderPipeline CreatePipeline ()
        {
            return new YRenderPipeline(this);
        }
        
        // --------------------------------------------------------------------------------
        // 合批配置
        [FoldoutGroup("Batching Settings", expanded: true)]
        public bool enableSRPBatcher = true;
        
        [FoldoutGroup("Batching Settings")]
        public bool enableGPUInstancing = false;
        
        // --------------------------------------------------------------------------------
        // 渲染路径配置
        public enum RenderPath
        {
            Forward, Deferred
        }
        
        [FoldoutGroup("Render Path Settings", expanded: true)]
        [PropertyOrder(-1)] public RenderPath renderPath = RenderPath.Forward;
        
        [FoldoutGroup("Render Path Settings")]
        [ReadOnly] public List<PipelineNode> currentPipelineNodes = new List<PipelineNode>();
        
        private List<PipelineNode> m_ForwardPipelineNodes;
        private List<PipelineNode> m_DeferredPipelineNodes;
        
        private Dictionary<RenderPath, List<PipelineNode>> m_PresetRenderPathsDict = new Dictionary<RenderPath, List<PipelineNode>>();
        public Dictionary<RenderPath, List<PipelineNode>> PresetRenderPathsDict => m_PresetRenderPathsDict;

        public void PresetRenderPaths()
        {
            m_PresetRenderPathsDict.Clear();
            m_ForwardPipelineNodes = new List<PipelineNode>()
            {
                PipelineNode.Create<ForwardLightingNode>(), PipelineNode.Create<ForwardNode>(), 
                PipelineNode.Create<SkyboxNode>(), PipelineNode.Create<TransparencyNode>()
            };
            m_PresetRenderPathsDict.Add(RenderPath.Forward, m_ForwardPipelineNodes);

            m_DeferredPipelineNodes = new List<PipelineNode>()
            {

            };
            m_PresetRenderPathsDict.Add(RenderPath.Deferred, m_DeferredPipelineNodes);
            
            // Set Current Render Path's PipelineNodes
            currentPipelineNodes = m_PresetRenderPathsDict[renderPath];
        }
        
        // --------------------------------------------------------------------------------
        // 后处理配置
        
        
        // --------------------------------------------------------------------------------
        // 其他共用字段
        private static int[] m_TextureSizes = new int[] { 512, 1024, 2048, 4096, 8192 };
        private static int[] m_SampleNumbers = new int[] { 1, 4, 8, 16, 32, 64, 128 };
    }
}