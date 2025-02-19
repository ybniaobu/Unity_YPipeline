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
            m_TransparencyShaderTagId = new ShaderTagId("YPipelineTransparency");
        }
        
        protected override void Dispose()
        {
            DestroyImmediate(this);
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            TransparencyRenderer(asset, ref data);
        }

        private void TransparencyRenderer(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            FilteringSettings transparencyFiltering = new FilteringSettings(RenderQueueRange.transparent);
            
            SortingSettings transparencySorting = new SortingSettings(data.cameraData.camera)
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
            
            data.buffer.DrawRendererList(transparencyRendererList);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }
    }
}