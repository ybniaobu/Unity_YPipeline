using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class TAASubPass : PostProcessingSubPass
    {
        private class TAAPassData
        {
            public TextureHandle taaHistory;
        }
        
        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TAAPassData>("TAA", out var passData))
            {
                Vector2Int bufferSize = data.BufferSize;
                
                RenderTextureDescriptor taaHistoryDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y)
                {
                    graphicsFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRColorBuffer ? DefaultFormat.HDR : DefaultFormat.LDR),
                    volumeDepth = 1,
                    msaaSamples = 1,
                    mipCount = 0,
                    autoGenerateMips = false,
                };

                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                RTHandle taaHistory = yCamera.perCameraData.GetTAAHistory(ref taaHistoryDesc);
                
                passData.taaHistory = data.renderGraph.ImportTexture(taaHistory);
                builder.ReadWriteTexture(passData.taaHistory);
                
                
                builder.AllowPassCulling(false);
                
                int frameIndex = Time.frameCount;
                
                builder.SetRenderFunc((TAAPassData data, RenderGraphContext context) =>
                {
                    
                });
            }
        }
    }
}