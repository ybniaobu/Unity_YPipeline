using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class AmbientOcclusionPass : PipelinePass
    {
        private class AmbientOcclusionPassData
        {
            
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<AmbientOcclusionPassData>("Ambient Occlusion", out var passData))
            {
                
                builder.ReadTexture(data.ThinGBuffer);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((AmbientOcclusionPassData data, RenderGraphContext context) =>
                {
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}