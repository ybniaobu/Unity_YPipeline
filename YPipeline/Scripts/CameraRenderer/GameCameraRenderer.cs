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
            base.Initialize(ref data);
            PresetRenderPaths(data.asset.renderPath);
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            if (!Setup(ref data)) return;
            
            VolumeManager.instance.Update(data.camera.transform, 1);
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionName = "YPipeline",
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
            };
        
            data.renderGraph.BeginRecording(renderGraphParams);
            
            PrepareBuffers(ref data);
            PipelineNode.Render(cameraPipelineNodes, ref data);
            
            PipelineNode.Record(cameraPipelineNodes, ref data);
            data.renderGraph.EndRecordingAndExecute();
            
            PipelineNode.Release(cameraPipelineNodes, ref data);
            ReleaseBuffers(ref data);
            
            CommandBufferPool.Release(data.cmd);
        }
    }
}