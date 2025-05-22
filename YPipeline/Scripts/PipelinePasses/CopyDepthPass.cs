using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyDepthPass : PipelinePass
    {
        private class CopyDepthPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle depthTexture;
        }

        protected override void Initialize() { }
        
        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyDepthPassData>("Copy Depth", out var passData))
            {
                passData.depthAttachment = builder.ReadTexture(data.CameraDepthAttachment);
                passData.depthTexture = builder.WriteTexture(data.CameraDepthTexture);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((CopyDepthPassData data, RenderGraphContext context) =>
                {
                    bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                    if (copyTextureSupported) context.cmd.CopyTexture(data.depthAttachment, data.depthTexture);
                    else BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.depthTexture);
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_DepthTextureID, data.depthTexture);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}