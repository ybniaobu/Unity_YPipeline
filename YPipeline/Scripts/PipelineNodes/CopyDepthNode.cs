using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CopyDepthNode : PipelineNode
    {
        private class CopyDepthNodeData
        {
            
        }

        protected override void Initialize() { }
        
        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CopyDepthNodeData>("Copy Depth", out var nodeData))
            {
                builder.SetRenderFunc((CopyDepthNodeData data, RenderGraphContext context) =>
                {
                    BlitUtility.CopyDepth(context.cmd, YPipelineShaderIDs.k_DepthBufferID, YPipelineShaderIDs.k_DepthTextureID);
                });
            }
        }
    }
}