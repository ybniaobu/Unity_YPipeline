using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PostProcessingNode : PipelineNode
    {
        private BloomRenderer m_BloomRenderer;
        private ColorGradingLutRenderer m_ColorGradingLutRenderer;
        private UberPostProcessingRenderer m_UberPostProcessingRenderer;
        private FinalPostProcessingRenderer m_FinalPostProcessingRenderer;
        
        protected override void Initialize()
        {
            m_BloomRenderer = PostProcessingRenderer.Create<BloomRenderer>();
            m_ColorGradingLutRenderer = PostProcessingRenderer.Create<ColorGradingLutRenderer>();
            m_UberPostProcessingRenderer = PostProcessingRenderer.Create<UberPostProcessingRenderer>();
            m_FinalPostProcessingRenderer = PostProcessingRenderer.Create<FinalPostProcessingRenderer>();
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(ref YPipelineData data)
        {
            base.OnRelease(ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(ref YPipelineData data)
        {
            base.OnRender(ref data);
            
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                BlitUtility.CopyDepth(data.buffer, YPipelineShaderIDs.k_DepthBufferID, BuiltinRenderTextureType.CameraTarget);
                RendererList gizmosRendererList = data.context.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                data.buffer.DrawRendererList(gizmosRendererList);
            }
            
            // disable post-processing in material preview and reflection probe preview
            if (data.camera.cameraType > CameraType.SceneView)
            {
                // TODO: 改变逻辑
                BlitUtility.BlitTexture(data.buffer, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
                return;
            }
            
            // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
            if (data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                BlitUtility.BlitTexture(data.buffer, YPipelineShaderIDs.k_ColorBufferID, BuiltinRenderTextureType.CameraTarget);
                return;
            }
#endif
            data.buffer.BeginSample("Post Processing");
            
            // all post processing renderers entrance
            PostProcessingRender(data.asset, ref data);
            
            // TODO：Final Blit Node
            // data.buffer.Blit(RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget);
            
            data.buffer.EndSample("Post Processing");
            
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos()) 
            {
                RendererList gizmosRendererList = data.context.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                data.buffer.DrawRendererList(gizmosRendererList);
            }
#endif
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        private void PostProcessingRender(YRenderPipelineAsset asset, ref YPipelineData data)
        {
            // Bloom
            m_BloomRenderer.Render(ref data);
            
            // Color Grading Lut
            m_ColorGradingLutRenderer.Render(ref data);
            
            // Post Color Grading
            m_UberPostProcessingRenderer.Render(ref data);
            
            // Final Post Processing
            m_FinalPostProcessingRenderer.Render(ref data);
            
            // Clear RT
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_BloomTextureID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_ColorGradingLutTextureID);
        }
    }
}