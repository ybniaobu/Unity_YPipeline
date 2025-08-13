using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DepthOnlyPass : PipelinePass
    {
        private class DepthOnlyPassData
        {
            public TextureHandle depthAttachment;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DepthOnlyPassData>("Depth PrePass", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_DepthShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_DepthShaderTagId, data.cullingResults, data.camera)
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
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((DepthOnlyPassData data, RenderGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.depthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}