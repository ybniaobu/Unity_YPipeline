using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardThinGBufferPass : PipelinePass
    {
        private class ThinGBufferPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle thinGBuffer0;
            public TextureHandle thinGBuffer1;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ThinGBufferPassData>("Thin GBuffer", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ThinGBufferShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ThinGBufferShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);

                passData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Write);
                passData.thinGBuffer0 = builder.UseColorBuffer(data.ThinGBuffer0, 0);
                passData.thinGBuffer1 = builder.UseColorBuffer(data.ThinGBuffer1, 1);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((ThinGBufferPassData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_ThinGBuffer0ID, data.thinGBuffer0);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}