using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class CameraRenderer
    {
        protected List<PipelinePass> m_CameraPipelineNodes = new List<PipelinePass>();
        
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
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CullingPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardLightsPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardShadowsPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardResourcesPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DepthNormalPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardGeometryPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ErrorMaterialPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<SkyboxPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TransparencyPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<PostProcessingPass>());
                    
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DebugPass>());
#endif
#if UNITY_EDITOR
                    m_CameraPipelineNodes.Add(PipelinePass.Create<GizmosPass>());
#endif
                    break;
                case RenderPath.Deferred:
                    
                    break;
                case RenderPath.Custom:
                    
                    break;
            }
        }
    }
}