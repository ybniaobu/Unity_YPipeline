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
        private PostColorGradingRenderer m_PostColorGradingRenderer;
        
        protected override void Initialize()
        {
            m_BloomRenderer = PostProcessingRenderer.Create<BloomRenderer>();
            m_ColorGradingLutRenderer = PostProcessingRenderer.Create<ColorGradingLutRenderer>();
            m_PostColorGradingRenderer = PostProcessingRenderer.Create<PostColorGradingRenderer>();
        }
        
        protected override void Dispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos()) 
            {
                RendererList gizmosRendererList = data.context.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                data.buffer.DrawRendererList(gizmosRendererList);
            }
            
            // disable post-processing in material preview and reflection probe preview
            if (data.camera.cameraType > CameraType.SceneView)
            {
                // TODO: 改变逻辑
                BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget);
                return;
            }
            
            // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
            if (data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget);
                return;
            }
#endif
            data.buffer.BeginSample("Post Processing");
            
            // all post processing renderers entrance
            PostProcessingRender(asset, ref data);
            
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

        private void PostProcessingRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            RenderTextureFormat format = asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            
            // Bloom
            m_BloomRenderer.Render(asset, ref data);
            
            // Color Grading Lut
            m_ColorGradingLutRenderer.Render(asset, ref data);
            
            // Post Color Grading
            m_PostColorGradingRenderer.Render(asset, ref data);
            
            // Clear RT
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_BloomTextureId);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_ColorGradingLutTextureId);
        }
    }
}