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
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionName = data.camera.name,
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
            };
            data.renderGraph.BeginRecording(renderGraphParams);
            
            // 好像在这个版本中，要自己调用 Update，否则无法获取到 VolumeComponent 序列化后的数据。可能以后的版本要删除这段代码。
            VolumeManager.instance.Update(data.camera.transform, 1);
            
            PrepareBuffers(ref data);
            PipelineNode.Render(cameraPipelineNodes, ref data);
            PipelineNode.Release(cameraPipelineNodes, ref data);
            ReleaseBuffers(ref data);
            
            data.renderGraph.EndRecordingAndExecute();
            
            CommandBufferPool.Release(data.cmd);
        }
    }
}