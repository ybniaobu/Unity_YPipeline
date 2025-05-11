using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class SkyboxNode : PipelineNode
    {
        private class SkyboxNodeData
        {
            public RendererListHandle skyboxRendererList;
        }
        
        protected override void Initialize()
        {
            
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<SkyboxNodeData>("Draw Skybox", out var nodeData))
            {
                nodeData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(nodeData.skyboxRendererList);
                
                builder.WriteTexture(data.CameraColorAttachment);
                builder.ReadTexture(data.CameraDepthAttachment);

                builder.SetRenderFunc((SkyboxNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                });
            }
        }
    }
}