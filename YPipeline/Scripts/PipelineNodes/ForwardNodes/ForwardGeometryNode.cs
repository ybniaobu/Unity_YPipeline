using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardGeometryNode : PipelineNode
    {
        private class ForwardGeometryNodeData
        {
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize() { }

        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(ref YPipelineData data)
        {
            base.OnRelease(ref data);
            data.context.ExecuteCommandBuffer(data.cmd);
            data.cmd.Clear();
            data.context.Submit();
        }

        protected override void OnRender(ref YPipelineData data)
        {
            // using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.GeometryNode));
            // base.OnRender(ref data);
            // data.context.SetupCameraProperties(data.camera);
            //
            // // Filtering & Sorting
            // FilteringSettings opaqueFiltering = 
            //     new FilteringSettings(new RenderQueueRange(2000, 2449));
            //
            // FilteringSettings alphaTestFiltering =
            //     new FilteringSettings(new RenderQueueRange(2450, 2499));
            //
            // SortingSettings opaqueSorting = new SortingSettings(data.camera)
            // {
            //     criteria = SortingCriteria.CommonOpaque
            // };
            //
            // SortingSettings alphaTestSorting = new SortingSettings(data.camera)
            // {
            //     criteria = SortingCriteria.OptimizeStateChanges
            // };
            //
            // // Depth PrePass
            // data.cmd.BeginSample("Depth PrePass");
            // data.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_DepthBufferID), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // data.cmd.ClearRenderTarget(true, false, data.camera.backgroundColor.linear);
            //
            // DrawingSettings depthOpaqueDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_DepthShaderTagId, opaqueSorting)
            // {
            //     enableInstancing = data.asset.enableGPUInstancing,
            //     perObjectData = PerObjectData.None
            // };
            //
            // DrawingSettings depthAlphaTestDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_DepthShaderTagId, alphaTestSorting)
            // {
            //     enableInstancing = data.asset.enableGPUInstancing,
            //     perObjectData = PerObjectData.None
            // };
            //
            // RendererListParams depthOpaqueRendererListParams =
            //     new RendererListParams(data.cullingResults, depthOpaqueDrawing, opaqueFiltering);
            //
            // RendererListParams depthAlphaTestRendererListParams =
            //     new RendererListParams(data.cullingResults, depthAlphaTestDrawing, alphaTestFiltering);
            //
            // RendererList depthOpaqueRendererList = data.context.CreateRendererList(ref depthOpaqueRendererListParams);
            // RendererList depthAlphaTestRendererList = data.context.CreateRendererList(ref depthAlphaTestRendererListParams);
            //
            // data.cmd.DrawRendererList(depthOpaqueRendererList);
            // data.cmd.DrawRendererList(depthAlphaTestRendererList);
            //
            // data.cmd.EndSample("Depth PrePass");
            //
            // // Copy Depth
            // data.cmd.BeginSample("Copy Depth");
            // BlitUtility.CopyDepth(data.cmd, YPipelineShaderIDs.k_DepthBufferID, YPipelineShaderIDs.k_DepthTextureID);
            // data.cmd.EndSample("Copy Depth");
            //
            // // Draw Opaque & AlphaTest
            // data.cmd.BeginSample("Draw Opaque & AlphaTest");
            // data.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_ColorBufferID), 
            //     RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            //     new RenderTargetIdentifier(YPipelineShaderIDs.k_DepthBufferID),
            //     RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            //
            // data.cmd.ClearRenderTarget(false, true, data.camera.backgroundColor.linear);
            //
            // DrawingSettings opaqueDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_SRPDefaultShaderTagId, opaqueSorting)
            // {
            //     enableInstancing = data.asset.enableGPUInstancing,
            //     perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            // };
            // opaqueDrawing.SetShaderPassName(1, YPipelineShaderTagIDs.k_ForwardLitShaderTagId);
            //
            // DrawingSettings alphaTestDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_SRPDefaultShaderTagId, alphaTestSorting)
            // {
            //     enableInstancing = data.asset.enableGPUInstancing,
            //     perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            // };
            // alphaTestDrawing.SetShaderPassName(1, YPipelineShaderTagIDs.k_ForwardLitShaderTagId);
            //
            // RendererListParams opaqueRendererListParams =
            //     new RendererListParams(data.cullingResults, opaqueDrawing, opaqueFiltering);
            //
            // RendererListParams alphaTestRendererListParams =
            //     new RendererListParams(data.cullingResults, alphaTestDrawing, alphaTestFiltering);
            //     
            // RendererList opaqueRendererList = data.context.CreateRendererList(ref opaqueRendererListParams);
            // RendererList alphaTestRendererList = data.context.CreateRendererList(ref alphaTestRendererListParams);
            //
            // data.cmd.DrawRendererList(opaqueRendererList);
            // data.cmd.DrawRendererList(alphaTestRendererList);
            //
            // data.cmd.EndSample("Draw Opaque & AlphaTest");
            //
            // // Submit
            // data.context.ExecuteCommandBuffer(data.cmd);
            // data.cmd.Clear();
            // data.context.Submit();
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardGeometryNodeData>("Draw Opaque & AlphaTest", out var nodeData))
            {
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
                DrawingSettings opaqueDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_SRPDefaultShaderTagId, opaqueSorting)
                {
                    enableInstancing = data.asset.enableGPUInstancing,
                    perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
                };
                opaqueDrawing.SetShaderPassName(1, YPipelineShaderTagIDs.k_ForwardLitShaderTagId);
                DrawingSettings alphaTestDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_SRPDefaultShaderTagId, alphaTestSorting)
                {
                    enableInstancing = data.asset.enableGPUInstancing,
                    perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
                };
                alphaTestDrawing.SetShaderPassName(1, YPipelineShaderTagIDs.k_ForwardLitShaderTagId);
                RendererListParams opaqueRendererListParams = new RendererListParams(data.cullingResults, opaqueDrawing, opaqueFiltering);
                RendererListParams alphaTestRendererListParams = new RendererListParams(data.cullingResults, alphaTestDrawing, alphaTestFiltering);
                
                nodeData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListParams);
                nodeData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListParams);
                builder.UseRendererList(nodeData.opaqueRendererList);
                builder.UseRendererList(nodeData.alphaTestRendererList);

                builder.SetRenderFunc((ForwardGeometryNodeData data, RenderGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_ColorBufferID), 
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        new RenderTargetIdentifier(YPipelineShaderIDs.k_DepthBufferID),
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            
                    context.cmd.ClearRenderTarget(false, true, Color.clear);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                });
            }
        }
    }
}