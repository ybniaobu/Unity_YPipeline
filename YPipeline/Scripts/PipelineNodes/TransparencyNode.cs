using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class TransparencyNode : PipelineNode
    {
        private static ShaderTagId m_UnlitShaderTagId;
        private static ShaderTagId m_TransparencyShaderTagId;
        
        protected override void Initialize()
        {
            m_UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"); 
            m_TransparencyShaderTagId = new ShaderTagId("YPipelineForward");
            //m_TransparencyShaderTagId = new ShaderTagId("YPipelineTransparency");
        }
        
        protected override void Dispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_ColorTextureId);
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            
            // Copy Color
            data.buffer.BeginSample("Copy Color");
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_ColorTextureId, data.camera.pixelWidth, data.camera.pixelHeight, 0, FilterMode.Bilinear, 
                asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_ColorBufferId, RenderTargetIDs.k_ColorTextureId);
            
            data.buffer.EndSample("Copy Color");
            
            
            TransparencyRenderer(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        private void TransparencyRenderer(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            data.buffer.BeginSample("Transparency");
            FilteringSettings transparencyFiltering = new FilteringSettings(RenderQueueRange.transparent);
            
            SortingSettings transparencySorting = new SortingSettings(data.camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            
            DrawingSettings transparencyDrawing = new DrawingSettings(m_TransparencyShaderTagId, transparencySorting)
            {
                enableInstancing = asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            transparencyDrawing.SetShaderPassName(1, m_UnlitShaderTagId);
            
            RendererListParams transparencyRendererListParams =
                new RendererListParams(data.cullingResults, transparencyDrawing, transparencyFiltering);
            
            RendererList transparencyRendererList = data.context.CreateRendererList(ref transparencyRendererListParams);
            
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(RenderTargetIDs.k_ColorBufferId), 
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                new RenderTargetIdentifier(RenderTargetIDs.k_DepthBufferId),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            data.buffer.DrawRendererList(transparencyRendererList);
            data.buffer.EndSample("Transparency");
        }
    }
}