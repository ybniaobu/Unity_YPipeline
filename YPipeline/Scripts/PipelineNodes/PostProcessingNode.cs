﻿using UnityEngine;
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
        private ColorGradingRenderer m_ColorGradingRenderer;
        private ToneMappingRenderer m_ToneMappingRenderer;
        
        private const string k_Copy = "Hidden/YPipeline/Copy";
        private Material m_CopyMaterial;
        
        private Material CopyMaterial
        {
            get
            {
                if (m_CopyMaterial == null)
                {
                    m_CopyMaterial = new Material(Shader.Find(k_Copy));
                    m_CopyMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_CopyMaterial;
            }
        }
        
        protected override void Initialize()
        {
            m_BloomRenderer = PostProcessingRenderer.Create<BloomRenderer, Bloom>();
            m_ToneMappingRenderer = PostProcessingRenderer.Create<ToneMappingRenderer, ToneMapping>();
            m_ColorGradingRenderer = PostProcessingRenderer.Create<ColorGradingRenderer, ColorGrading>();
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
                data.buffer.Blit(RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget);
                return;
            }
            
            // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
            if (data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                data.buffer.Blit(RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget);
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
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_BloomTextureId, data.camera.pixelWidth, data.camera.pixelHeight, 0, FilterMode.Bilinear, format);
            m_BloomRenderer.Render(asset, ref data);
            if (!m_BloomRenderer.isActivated) BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, RenderTargetIDs.k_BloomTextureId, CopyMaterial,0);
            
            // Color Grading
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_ColorGradingTextureId, data.camera.pixelWidth, data.camera.pixelHeight, 0, FilterMode.Bilinear, format);
            m_ColorGradingRenderer.Render(asset, ref data);
            if (!m_ColorGradingRenderer.isActivated) BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_BloomTextureId, RenderTargetIDs.k_ColorGradingTextureId, CopyMaterial,0);;
            
            // Tonemapping
            m_ToneMappingRenderer.Render(asset, ref data);
            if (!m_ToneMappingRenderer.isActivated) BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_ColorGradingTextureId, BuiltinRenderTextureType.CameraTarget, CopyMaterial,0);
            
            // Clear RT
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_BloomTextureId);
            data.buffer.ReleaseTemporaryRT(RenderTargetIDs.k_ColorGradingTextureId);
        }
    }
}