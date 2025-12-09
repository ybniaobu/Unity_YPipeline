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
            
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionId = data.camera.GetEntityId(),
                generateDebugData = true,
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
                renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.BottomLeft,
                rendererListCulling = true
            };
            
            try
            {
                data.renderGraph.BeginRecording(renderGraphParams);
                
                PipelinePass.Record(m_CameraPipelineNodes, ref data);
                
                data.renderGraph.EndRecordingAndExecute();
            }
            catch (Exception e)
            {
                if (data.renderGraph.ResetGraphAndLogException(e))
                    throw;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            PipelinePass.Dispose(m_CameraPipelineNodes);
        }
    }
}