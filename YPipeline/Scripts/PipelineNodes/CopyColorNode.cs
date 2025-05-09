using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyColorNode : PipelineNode
    {
        private class CopyColorNodeData
        {
            
        }

        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyColorNodeData>("Copy Color", out var nodeData))
            {
                builder.SetRenderFunc((CopyColorNodeData data, RenderGraphContext context) =>
                {
                    BlitUtility.BlitTexture(context.cmd, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_ColorTextureID);
                });
            }
        }
    }
}