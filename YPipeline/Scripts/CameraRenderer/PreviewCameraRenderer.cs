using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class PreviewCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            m_CameraPipelineNodes.Clear();
            m_CameraPipelineNodes.Add(PipelinePass.Create<CullingPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewLightSetupPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardResourcesPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardThinGBufferPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<AmbientOcclusionPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardGeometryPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<ErrorMaterialPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<SkyboxPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<TransparencyPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<PostProcessingPass>());
        }

        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            // TODO：反射探针不能用 depth prepass 渲染，效果不好 ！！！！！！！！！！！！！！
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionName = "YPipeline",
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
                rendererListCulling = true
            };
            
            data.renderGraph.BeginRecording(renderGraphParams);
            
            PipelinePass.Record(m_CameraPipelineNodes, ref data);

            data.renderGraph.EndRecordingAndExecute();
        }
    }
}