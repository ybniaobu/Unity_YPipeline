using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public partial class YRenderPipeline : RenderPipeline
    {
        private YPipelineData m_Data;
        
        private GameCameraRenderer m_GameCameraRenderer;
        private SceneCameraRenderer m_SceneCameraRenderer;
        
        public YRenderPipeline(YRenderPipelineAsset asset)
        {
            m_Data = new YPipelineData();
            m_Data.asset = asset;
            
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
            
            VolumeManager.instance.Initialize(null, asset.globalVolumeProfile);

            m_GameCameraRenderer = CameraRenderer.Create<GameCameraRenderer>(ref m_Data);
            
#if UNITY_EDITOR
            m_SceneCameraRenderer = CameraRenderer.Create<SceneCameraRenderer>(ref m_Data);
            InitializeLightmapper();
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
            
            // TODO: 删除 Begin 方法
            m_GameCameraRenderer.Begin(ref m_Data);
            
            foreach(Camera camera in cameras)
            {
                BeginCameraRendering(context, camera);
                m_Data.camera = camera;
                
                m_GameCameraRenderer.Render(ref m_Data);
                
                EndCameraRendering(context, camera);
            }
            
            EndContextRendering(context, cameras);
        }
        
        protected override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            VolumeManager.instance.Deinitialize();
            
#if UNITY_EDITOR
            UnityEngine.Experimental.GlobalIllumination.Lightmapping.ResetDelegate();
#endif
        }
    }
}