using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DepthNormalPass : PipelinePass
    {
        private class DepthNormalPassData
        {
            public Camera camera;
            
            public TextureHandle depthAttachment;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DepthNormalPassData>("Depth Normal PrePass", out var passData))
            {
                passData.camera = data.camera;
                
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
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);

                passData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Write);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((DepthNormalPassData data, RenderGraphContext context) =>
                {
                    // 暂时先放这里
                    context.cmd.SetupCameraProperties(data.camera);
                    
                    context.cmd.SetRenderTarget(data.depthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}