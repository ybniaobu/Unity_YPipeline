using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ForwardNode : PipelineNode
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
            
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            data.context.SetupCameraProperties(data.cameraData.camera);
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
            CameraClearFlags flags = data.cameraData.camera.clearFlags;
            data.buffer.ClearRenderTarget(flags < CameraClearFlags.Nothing, 
                flags < CameraClearFlags.Depth, data.cameraData.camera.backgroundColor.linear);
            ForwardRenderer(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }

        private void ForwardRenderer(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            FilteringSettings opaqueFiltering = 
                new FilteringSettings(new RenderQueueRange(2000, 2449));
            
            FilteringSettings alphaTestFiltering =
                new FilteringSettings(new RenderQueueRange(2450, 2499));

            SortingSettings opaqueSorting = new SortingSettings(data.cameraData.camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            
            SortingSettings alphaTestSorting = new SortingSettings(data.cameraData.camera)
            {
                criteria = SortingCriteria.OptimizeStateChanges
            };

            DrawingSettings opaqueDrawing = new DrawingSettings(m_UnlitShaderTagId, opaqueSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            opaqueDrawing.SetShaderPassName(1, m_ForwardLitShaderTagId);
            
            DrawingSettings alphaTestDrawing = new DrawingSettings(m_UnlitShaderTagId, alphaTestSorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
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