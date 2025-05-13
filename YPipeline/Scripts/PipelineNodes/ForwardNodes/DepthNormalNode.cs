using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DepthNormalNode : PipelineNode
    {
        private class DepthNormalNodeData
        {
            public Camera camera;
            
            public TextureHandle depthAttachment;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DepthNormalNodeData>("Depth Normal PrePass", out var nodeData))
            {
                nodeData.camera = data.camera;
                
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
                
                nodeData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                nodeData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(nodeData.opaqueRendererList);
                builder.UseRendererList(nodeData.alphaTestRendererList);

                nodeData.depthAttachment = builder.UseDepthBuffer(data.CameraDepthAttachment, DepthAccess.Write);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((DepthNormalNodeData data, RenderGraphContext context) =>
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