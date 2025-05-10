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
            SetRenderPaths(data.asset.renderPath);
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            //if (!Culling(ref data)) return;
            
            VolumeManager.instance.Update(data.camera.transform, 1);
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionName = "YPipeline",
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
            };
        
            data.renderGraph.BeginRecording(renderGraphParams);
            
            PipelineNode.Record(m_CameraPipelineNodes, ref data);
            
            data.renderGraph.EndRecordingAndExecute();
            
            PipelineNode.Release(m_CameraPipelineNodes, ref data);
        }
    }
}