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
                nodeData.colorAttachment = data.CameraColorAttachment;
                nodeData.colorTexture = data.CameraColorTexture;
                builder.ReadTexture(nodeData.colorAttachment);
                builder.WriteTexture(nodeData.colorTexture);
                
                builder.SetRenderFunc((CopyColorNodeData data, RenderGraphContext context) =>
                {
                    BlitUtility.BlitTexture(context.cmd, nodeData.colorAttachment, nodeData.colorTexture);
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_ColorTextureID, nodeData.colorTexture);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}