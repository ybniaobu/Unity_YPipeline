﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    // --------------------------------------------------------------------------------
    // Profiling 相关
    public enum YPipelineProfileId
    {
        YPipelineTotal, BeginFrameRendering, EndFrameRendering, BeginCameraRendering, EndCameraRendering, 
        LightingNode, Setup
    }
    public partial class YRenderPipeline : RenderPipeline
    {
        private YRenderPipelineAsset m_Asset;
        private PipelinePerFrameData m_Data;
        
        private bool m_IsPipelineNodesBegan;
        
        public YRenderPipeline(YRenderPipelineAsset asset)
        {
            m_Asset = asset;
            m_Data = new PipelinePerFrameData();
            asset.PresetRenderPaths();
            
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;

            m_IsPipelineNodesBegan = false;
            
#if UNITY_EDITOR
            InitializeLightmapper();
#endif
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // Older version of the Render function that can generate garbage, needed for backwards compatibility
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileId.YPipelineTotal));
            
            Begin(ref context);
            
            BeginContextRendering(context, cameras);
            m_Data.context = context;
            
            foreach(Camera camera in cameras)
            {
                BeginCameraRendering(context, camera);
                if (!Setup(camera)) return;
                PipelineNode.Render(m_Asset, ref m_Data);
                
                // Drawing Gizmos
#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos()) 
                {
                    RendererList gizmosRendererList = context.CreateGizmoRendererList(camera, GizmoSubset.PreImageEffects);
                    m_Data.buffer.DrawRendererList(gizmosRendererList);
                    gizmosRendererList = context.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects);
                    m_Data.buffer.DrawRendererList(gizmosRendererList);
                }
#endif
                
                // Submit all scheduled commands
                m_Data.context.ExecuteCommandBuffer(m_Data.buffer);
                m_Data.buffer.Clear();
                m_Data.context.Submit();
                
                PipelineNode.ReleaseResources(m_Asset, ref m_Data);
                CommandBufferPool.Release(m_Data.buffer);
                EndCameraRendering(context, camera);
            }
            
            EndContextRendering(context, cameras);
        }

        private bool Setup(Camera camera)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileId.Setup));
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                return false;
            }
            cullingParameters.shadowDistance = Mathf.Min(m_Asset.maxShadowDistance, camera.farClipPlane);
            m_Data.cullingResults = m_Data.context.Cull(ref cullingParameters);
            m_Data.cameraData = new YPipelineCameraData(camera); //TODO: GC 问题
            m_Data.buffer = CommandBufferPool.Get();
#if UNITY_EDITOR
            m_Data.buffer.name = camera.name;
                
            // Drawing UI
            if (camera.cameraType == CameraType.SceneView) 
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
            return true;
        }

        private void Begin(ref ScriptableRenderContext context)
        {
            if (!m_IsPipelineNodesBegan)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                PipelineNode.Begin(m_Asset, ref context, cmd);
                m_IsPipelineNodesBegan = true;
                CommandBufferPool.Release(cmd);
            }
        }
        
        protected override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
#if UNITY_EDITOR
            UnityEngine.Experimental.GlobalIllumination.Lightmapping.ResetDelegate();
#endif
            PipelineNode.DisposeNodes(m_Asset);
        }
    }
}