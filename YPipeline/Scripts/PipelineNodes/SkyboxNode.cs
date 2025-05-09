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

        protected override void OnRender(ref YPipelineData data)
        {
            base.OnRender(ref data);
            // SkyboxRenderer(ref data);
            // data.context.ExecuteCommandBuffer(data.cmd);
            // data.cmd.Clear();
            // data.context.Submit();
        }

        private void SkyboxRenderer(ref YPipelineData data)
        {
            RendererList skyboxRendererList = data.context.CreateSkyboxRendererList(data.camera);
            data.cmd.DrawRendererList(skyboxRendererList);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<SkyboxNodeData>("Draw Skybox", out var nodeData))
            {
                nodeData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(nodeData.skyboxRendererList);

                builder.SetRenderFunc((SkyboxNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                });
            }
        }
    }
}