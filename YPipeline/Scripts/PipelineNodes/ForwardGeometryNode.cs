using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ForwardGeometryNode : PipelineNode
    {
        private static ShaderTagId m_UnlitShaderTagId;
        private static ShaderTagId m_ForwardLitShaderTagId;

        protected override void Initialize()
        {
            m_UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"); 
            m_ForwardLitShaderTagId = new ShaderTagId("YPipelineForward");
        }

        protected override void Dispose()
        {
            DestroyImmediate(this);
        }

        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.buffer.ReleaseTemporaryRT(ForwardRenderTarget.frameBufferId);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            data.context.SetupCameraProperties(data.camera);
            
            data.buffer.GetTemporaryRT(ForwardRenderTarget.frameBufferId, data.camera.pixelWidth, data.camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(ForwardRenderTarget.frameBufferId), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // CameraClearFlags flags = data.camera.clearFlags;
            // data.buffer.ClearRenderTarget(flags < CameraClearFlags.Nothing, flags < CameraClearFlags.Depth, data.camera.backgroundColor.linear);
            data.buffer.ClearRenderTarget(true, true, Color.clear);
            
            RenderOpaqueAndAlphaTest(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        private void RenderOpaqueAndAlphaTest(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
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

            DrawingSettings opaqueDrawing = new DrawingSettings(m_UnlitShaderTagId, opaqueSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            opaqueDrawing.SetShaderPassName(1, m_ForwardLitShaderTagId);
            
            DrawingSettings alphaTestDrawing = new DrawingSettings(m_UnlitShaderTagId, alphaTestSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            //alphaTestDrawing.SetShaderPassName(1, m_ForwardLitShaderTagId);

            RendererListParams opaqueRendererListParams =
                new RendererListParams(data.cullingResults, opaqueDrawing, opaqueFiltering);
            
            RendererListParams alphaTestRendererListParams =
                new RendererListParams(data.cullingResults, alphaTestDrawing, alphaTestFiltering);
                
            RendererList opaqueRendererList = data.context.CreateRendererList(ref opaqueRendererListParams);
            RendererList alphaTestRendererList = data.context.CreateRendererList(ref alphaTestRendererListParams);
            
            data.buffer.DrawRendererList(opaqueRendererList);
            data.buffer.DrawRendererList(alphaTestRendererList);
        }
    }
}