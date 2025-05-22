using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class SkyboxPass : PipelinePass
    {
        private class SkyboxPassData
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<SkyboxPassData>("Draw Skybox", out var passData))
            {
                passData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(passData.skyboxRendererList);
                
                builder.UseColorBuffer(data.CameraColorAttachment, 0);
                builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Read);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((SkyboxPassData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                });
            }
        }
    }
}