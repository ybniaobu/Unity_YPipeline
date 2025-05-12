using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyColorNode : PipelineNode
    {
        private class CopyColorNodeData
        {
            public TextureHandle colorAttachment;
            public TextureHandle colorTexture;
        }

        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyColorNodeData>("Copy Color", out var nodeData))
            {
                nodeData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                nodeData.colorTexture = builder.WriteTexture(data.CameraColorTexture);
                
                builder.SetRenderFunc((CopyColorNodeData data, RenderGraphContext context) =>
                {
                    BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.colorTexture);
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_ColorTextureID, data.colorTexture);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}