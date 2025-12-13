using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class GameCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            m_CameraPipelineNodes.Clear();
            switch (data.asset.renderPath)
            {
                case RenderPath.TiledBasedForward: 
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CullingPass>());
                    
                    // TODO：这里增加一个 Pass，类似 CullingPass 不使用 renderGraph，
                    // TODO：将 LightSetupPass、ShadowPass 的准备工作放进来，参考 urp 的 RenderSingleCamera
                    // TODO：主要是为了解决 context.CullShadowCasters 问题
                    
                    m_CameraPipelineNodes.Add(PipelinePass.Create<LightSetupPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardResourcesPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardThinGBufferPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<AmbientOcclusionPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>());
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardGeometryPass>());
#if UNITY_EDITOR
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ErrorMaterialPass>());
#endif
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
                case RenderPath.TiledBasedDeferred:
                    
                    break;
                case RenderPath.ClusteredBasedForward:
                    
                    break;
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
        }
    }
}