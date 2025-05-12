using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyDepthNode : PipelineNode
    {
        private class CopyDepthNodeData
        {
            public TextureHandle depthAttachment;
            public TextureHandle depthTexture;
        }

        protected override void Initialize() { }
        
        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyDepthNodeData>("Copy Depth", out var nodeData))
            {
                nodeData.depthAttachment = builder.ReadTexture(data.CameraDepthAttachment);
                nodeData.depthTexture = builder.WriteTexture(data.CameraDepthTexture);
                
                builder.SetRenderFunc((CopyDepthNodeData data, RenderGraphContext context) =>
                {
                    BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.depthTexture);
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_DepthTextureID, data.depthTexture);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}