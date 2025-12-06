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
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewSetupPass>());
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewDrawPass>());
            // m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>()); // 绘制透明物体可能需要用到 color texture，之后可能需要添加
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewFinalPass>()); // 是否加个 FXAA
        }

        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                // executionId = data.camera.GetEntityId(),
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