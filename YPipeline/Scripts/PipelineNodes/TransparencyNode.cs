using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class TransparencyNode : PipelineNode
    {
        protected override void Initialize()
        {
            
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(ref YPipelineData data)
        {
            base.OnRelease(ref data);
        }

        protected override void OnRender(ref YPipelineData data)
        {
            base.OnRender(ref data);
            
            // Copy Color
            data.buffer.BeginSample("Copy Color");
            BlitUtility.BlitTexture(data.buffer, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_ColorTextureID);
            data.buffer.EndSample("Copy Color");
            
            TransparencyRenderer(ref data);
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        private void TransparencyRenderer(ref YPipelineData data)
        {
            data.buffer.BeginSample("Transparency");
            FilteringSettings transparencyFiltering = new FilteringSettings(RenderQueueRange.transparent);
            
            SortingSettings transparencySorting = new SortingSettings(data.camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            
            DrawingSettings transparencyDrawing = new DrawingSettings(YPipelineShaderTagIDs.k_TransparencyShaderTagId, transparencySorting)
            {
                enableInstancing = data.asset.enableGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe
            };
            transparencyDrawing.SetShaderPassName(1, YPipelineShaderTagIDs.k_SRPDefaultShaderTagId);
            
            RendererListParams transparencyRendererListParams =
                new RendererListParams(data.cullingResults, transparencyDrawing, transparencyFiltering);
            
            RendererList transparencyRendererList = data.context.CreateRendererList(ref transparencyRendererListParams);
            
            data.buffer.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_ColorBufferID), 
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                new RenderTargetIdentifier(YPipelineShaderIDs.k_DepthBufferID),
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            data.buffer.DrawRendererList(transparencyRendererList);
            data.buffer.EndSample("Transparency");
        }
    }
}