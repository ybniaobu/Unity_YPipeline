using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public partial class YRenderPipeline : RenderPipeline
    {
        private YPipelineData m_Data;
        private GameCameraRenderer m_GameCameraRenderer;
        
#if UNITY_EDITOR
        private SceneCameraRenderer m_SceneCameraRenderer;
#endif
        
        public YRenderPipeline(YRenderPipelineAsset asset)
        {
            m_Data = new YPipelineData();
            m_Data.asset = asset;
            m_Data.renderGraph = new RenderGraph("YPipeline Render Graph");
            m_Data.lightsData = new YPipelineLightsData();
            
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.enableSRPBatcher;
            // GraphicsSettings.lightsUseLinearIntensity = true;
            
            VolumeManager.instance.Initialize(null, asset.globalVolumeProfile);

            m_GameCameraRenderer = CameraRenderer.Create<GameCameraRenderer>(ref m_Data);
            
#if UNITY_EDITOR
            m_SceneCameraRenderer = CameraRenderer.Create<SceneCameraRenderer>(ref m_Data);
            InitializeLightmapper();
#endif
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_Data.debugSettings = new DebugSettings();
#endif
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // Older version of the Render function that can generate garbage, needed for backwards compatibility
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.YPipelineTotal));
            BeginContextRendering(context, cameras);
            m_Data.context = context;
            
            foreach(Camera camera in cameras)
            {
                BeginCameraRendering(context, camera);
                m_Data.camera = camera;
                m_Data.cmd = CommandBufferPool.Get();
                
#if UNITY_EDITOR
                // 待修改
                if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                    ScriptableRenderContext.EmitGeometryForCamera(camera);

                if (m_Data.camera.cameraType == CameraType.SceneView) 
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(m_Data.camera);
                }
#endif
                VolumeManager.instance.Update(camera.transform, 1);
                
                m_GameCameraRenderer.Render(ref m_Data);
                EndCameraRendering(context, camera);
                
                m_Data.context.ExecuteCommandBuffer(m_Data.cmd);
                m_Data.cmd.Clear();
                m_Data.context.Submit();
                CommandBufferPool.Release(m_Data.cmd);
            }
            
            m_Data.renderGraph.EndFrame();
            EndContextRendering(context, cameras);
        }
        
        protected override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            VolumeManager.instance.Deinitialize();
            m_Data.renderGraph.Cleanup();
            m_Data.renderGraph = null;
            
            
#if UNITY_EDITOR
            UnityEngine.Experimental.GlobalIllumination.Lightmapping.ResetDelegate();
#endif
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_Data.debugSettings.Dispose();
#endif
        }
    }
}