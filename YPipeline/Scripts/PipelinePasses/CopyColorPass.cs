using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyColorPass : PipelinePass
    {
        private class CopyColorPassData
        {
            public TextureHandle colorAttachment;
            public TextureHandle colorTexture;
        }

        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyColorPassData>("Copy Color", out var passData))
            {
                passData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                passData.colorTexture = builder.WriteTexture(data.CameraColorTexture);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((CopyColorPassData data, RenderGraphContext context) =>
                {
                    bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                    if (copyTextureSupported) context.cmd.CopyTexture(data.colorAttachment, data.colorTexture);
                    else BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.colorTexture);
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_ColorTextureID, data.colorTexture);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}