using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class CameraRenderer
    {
        protected List<PipelineNode> m_CameraPipelineNodes = new List<PipelineNode>();
        
        public static T Create<T>(ref YPipelineData data) where T : CameraRenderer, new()
        {
            T node = new T();
            node.Initialize(ref data);
            return node;
        }

        protected abstract void Initialize(ref YPipelineData data);

        public virtual void Dispose() { }

        public virtual void Render(ref YPipelineData data) { }
        
        protected void SetRenderPaths(RenderPath renderPath)
        {
            m_CameraPipelineNodes.Clear();
            switch (renderPath)
            {
                case RenderPath.Forward: 
                    m_CameraPipelineNodes.Add(PipelineNode.Create<CullingNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<ForwardBuffersNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<ForwardLightingNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<DepthNormalNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<CopyDepthNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<ForwardGeometryNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<ErrorMaterialNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<SkyboxNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<CopyColorNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<TransparencyNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<PostProcessingNode>());
                    m_CameraPipelineNodes.Add(PipelineNode.Create<GizmosNode>());
                    break;
                case RenderPath.Deferred:
                    
                    break;
                case RenderPath.Custom:
                    
                    break;
            }
        }
        
        protected bool Culling(ref YPipelineData data)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.FrustumCulling));
            
            if (!data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                return false;
            }
            
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            data.cullingResults = data.context.Cull(ref cullingParameters);
            
            return true;
        }
    }
}