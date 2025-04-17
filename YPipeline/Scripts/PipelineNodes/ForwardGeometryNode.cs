using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ForwardGeometryNode : PipelineNode
    {
        private static ShaderTagId k_UnlitShaderTagId;
        private static ShaderTagId k_ForwardLitShaderTagId;
        private static ShaderTagId k_DepthShaderTagId;

        protected override void Initialize()
        {
            k_UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"); 
            k_ForwardLitShaderTagId = new ShaderTagId("YPipelineForward");
            k_DepthShaderTagId = new ShaderTagId("Depth");
        }

        protected override void Dispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_ColorBufferId);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_DepthBufferId);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_DepthTextureId);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileId.GeometryNode));
            base.OnRender(asset, ref data);
            data.context.SetupCameraProperties(data.camera);
            
            // RT
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_ColorBufferId, data.camera.pixelWidth, data.camera.pixelHeight, 0, FilterMode.Bilinear, 
                asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_DepthBufferId, data.camera.pixelWidth, data.camera.pixelHeight, 32, FilterMode.Point, 
                RenderTextureFormat.Depth);
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_DepthTextureId, data.camera.pixelWidth, data.camera.pixelHeight, 32, FilterMode.Point, 
                RenderTextureFormat.Depth);
            
            // Filtering & Sorting
            FilteringSettings opaqueFiltering = 
                new FilteringSettings(new RenderQueueRange(2000, 2449));
            
            FilteringSettings alphaTestFiltering =
                new FilteringSettings(new RenderQueueRange(2450, 2499));

            SortingSettings opaqueSorting = new SortingSettings(data.camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            
            SortingSettings alphaTestSorting = new SortingSettings(data.camera)
            {
                criteria = SortingCriteria.OptimizeStateChanges
            };
            
            // Depth PrePass
            data.buffer.BeginSample("Depth PrePass");
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(RenderTargetIDs.k_DepthBufferId), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            data.buffer.ClearRenderTarget(true, false, data.camera.backgroundColor.linear);
            
            DrawingSettings depthOpaqueDrawing = new DrawingSettings(k_DepthShaderTagId, opaqueSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.None
            };
            
            DrawingSettings depthAlphaTestDrawing = new DrawingSettings(k_DepthShaderTagId, alphaTestSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.None
            };
            
            RendererListParams depthOpaqueRendererListParams =
                new RendererListParams(data.cullingResults, depthOpaqueDrawing, opaqueFiltering);
            
            RendererListParams depthAlphaTestRendererListParams =
                new RendererListParams(data.cullingResults, depthAlphaTestDrawing, alphaTestFiltering);
            
            RendererList depthOpaqueRendererList = data.context.CreateRendererList(ref depthOpaqueRendererListParams);
            RendererList depthAlphaTestRendererList = data.context.CreateRendererList(ref depthAlphaTestRendererListParams);
            
            data.buffer.DrawRendererList(depthOpaqueRendererList);
            data.buffer.DrawRendererList(depthAlphaTestRendererList);
            
            data.buffer.EndSample("Depth PrePass");
            
            // Copy Depth
            data.buffer.BeginSample("Copy Depth");
            BlitUtility.CopyDepth(data.buffer, RenderTargetIDs.k_DepthBufferId, RenderTargetIDs.k_DepthTextureId);
            data.buffer.EndSample("Copy Depth");
            
            // Draw Opaque & AlphaTest
            data.buffer.BeginSample("Draw Opaque & AlphaTest");
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(RenderTargetIDs.k_ColorBufferId), 
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                new RenderTargetIdentifier(RenderTargetIDs.k_DepthBufferId),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            
            data.buffer.ClearRenderTarget(false, true, data.camera.backgroundColor.linear);

            DrawingSettings opaqueDrawing = new DrawingSettings(k_UnlitShaderTagId, opaqueSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            opaqueDrawing.SetShaderPassName(1, k_ForwardLitShaderTagId);
            
            DrawingSettings alphaTestDrawing = new DrawingSettings(k_UnlitShaderTagId, alphaTestSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            alphaTestDrawing.SetShaderPassName(1, k_ForwardLitShaderTagId);

            RendererListParams opaqueRendererListParams =
                new RendererListParams(data.cullingResults, opaqueDrawing, opaqueFiltering);
            
            RendererListParams alphaTestRendererListParams =
                new RendererListParams(data.cullingResults, alphaTestDrawing, alphaTestFiltering);
                
            RendererList opaqueRendererList = data.context.CreateRendererList(ref opaqueRendererListParams);
            RendererList alphaTestRendererList = data.context.CreateRendererList(ref alphaTestRendererListParams);
            
            data.buffer.DrawRendererList(opaqueRendererList);
            data.buffer.DrawRendererList(alphaTestRendererList);
            
            data.buffer.EndSample("Draw Opaque & AlphaTest");
            
            // Submit
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }
    }
}