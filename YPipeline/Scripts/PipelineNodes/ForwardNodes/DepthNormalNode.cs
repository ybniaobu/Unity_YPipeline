using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DepthNormalNode : PipelineNode
    {
        private class DepthNormalNodeData
        {
            public Camera camera;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<DepthNormalNodeData>("Depth Normal PrePass", out var nodeData))
            {
                nodeData.camera = data.camera;
                
                FilteringSettings opaqueFiltering = new FilteringSettings(new RenderQueueRange(2000, 2449));
                FilteringSettings alphaTestFiltering = new FilteringSettings(new RenderQueueRange(2450, 2499));
                SortingSettings opaqueSorting = new SortingSettings(data.camera)
                {
                    criteria = SortingCriteria.CommonOpaque
                };
                SortingSettings alphaTestSorting = new SortingSettings(data.camera)
                {
                    criteria = SortingCriteria.OptimizeStateChanges
                };
                DrawingSettings opaqueDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_DepthShaderTagId, opaqueSorting)
                {
                    enableInstancing = data.asset.enableGPUInstancing,
                    perObjectData = PerObjectData.None
                };
                DrawingSettings alphaTestDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_DepthShaderTagId, alphaTestSorting)
                {
                    enableInstancing = data.asset.enableGPUInstancing,
                    perObjectData = PerObjectData.None
                };
                RendererListParams opaqueRendererListParams = new RendererListParams(data.cullingResults, opaqueDrawing, opaqueFiltering);
                RendererListParams alphaTestRendererListParams = new RendererListParams(data.cullingResults, alphaTestDrawing, alphaTestFiltering);
                
                nodeData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListParams);
                nodeData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListParams);
                builder.UseRendererList(nodeData.opaqueRendererList);
                builder.UseRendererList(nodeData.alphaTestRendererList);

                builder.SetRenderFunc((DepthNormalNodeData data, RenderGraphContext context) =>
                {
                    // 暂时先放这里
                    context.cmd.SetupCameraProperties(data.camera);
                    
                    context.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_DepthBufferID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    context.cmd.ClearRenderTarget(true, false, Color.clear);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                });
            }
        }
    }
}