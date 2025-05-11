using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class GameCameraRenderer : CameraRenderer
    {
        private string m_CameraName;
        
        protected override void Initialize(ref YPipelineData data)
        {
            SetRenderPaths(data.asset.renderPath);
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            if (string.IsNullOrEmpty(m_CameraName))
            {
                m_CameraName = data.camera.name;
            }
            data.cmd.BeginSample(m_CameraName);
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionName = "YPipeline",
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
                rendererListCulling = true
            };
        
            data.renderGraph.BeginRecording(renderGraphParams);
            
            PipelineNode.Record(m_CameraPipelineNodes, ref data);
            
            data.renderGraph.EndRecordingAndExecute();
            
            data.cmd.EndSample(m_CameraName);
        }
    }
}