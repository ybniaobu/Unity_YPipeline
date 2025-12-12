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
        private ReflectionCameraRenderer m_ReflectionCameraRenderer;
        
#if UNITY_EDITOR
        private PreviewCameraRenderer m_PreviewCameraRenderer;
#endif
        
        public YRenderPipeline(YRenderPipelineAsset asset)
        {
            m_Data = new YPipelineData();
            m_Data.asset = asset;
            m_Data.runtimeResources = GraphicsSettings.GetRenderPipelineSettings<YPipelineRuntimeResources>();
            m_Data.renderGraph = new RenderGraph("YPipeline Render Graph");
            m_Data.lightsData = new YPipelineLightsData();
            
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
            
            RTHandles.Initialize(Screen.width, Screen.height);
            VolumeManager.instance.Initialize(null, asset.globalVolumeProfile);

            m_GameCameraRenderer = CameraRenderer.Create<GameCameraRenderer>(ref m_Data);
            m_ReflectionCameraRenderer = CameraRenderer.Create<ReflectionCameraRenderer>(ref m_Data);
            
#if UNITY_EDITOR
            m_PreviewCameraRenderer = CameraRenderer.Create<PreviewCameraRenderer>(ref m_Data);
            InitializeLightmapper();
            EditorPrefs.SetInt("SceneViewFPS", 60);
#endif
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_Data.debugSettings = new DebugSettings();
#endif
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.YPipelineTotal));
            m_Data.context = context;
            
            foreach(Camera camera in cameras)
            {
                m_Data.camera = camera;
                m_Data.cmd = CommandBufferPool.Get();
                VolumeManager.instance.Update(camera.transform, 1);
                
#if UNITY_EDITOR
                // 待修改
                if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                    ScriptableRenderContext.EmitGeometryForCamera(camera);

                if (camera.cameraType == CameraType.SceneView) 
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(m_Data.camera);
                }
#endif

                switch (camera.cameraType)
                {
                    case CameraType.SceneView: 
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                    case CameraType.Preview:
                        m_PreviewCameraRenderer.Render(ref m_Data);
                        // m_GameCameraRenderer.Render(ref m_Data);
                        break;
                    case CameraType.Reflection:  // TODO：反射探针不能用 depth prepass 渲染，效果不好 ！！！！！！！！！！！！！！
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                    case CameraType.Game:
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                    default:
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                }
                
                m_Data.context.ExecuteCommandBuffer(m_Data.cmd);
                m_Data.context.Submit();
                m_Data.cmd.Clear();
                CommandBufferPool.Release(m_Data.cmd);
                m_Data.cmd = null;
            }
            
            m_Data.renderGraph.EndFrame();
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
#if UNITY_EDITOR
            UnityEngine.Experimental.GlobalIllumination.Lightmapping.ResetDelegate();
            m_PreviewCameraRenderer.Dispose();
            m_PreviewCameraRenderer = null;
#endif
            
            VolumeManager.instance.Deinitialize();
            m_Data.Dispose();
            
            m_GameCameraRenderer.Dispose();
            m_GameCameraRenderer = null;
            m_ReflectionCameraRenderer.Dispose();
            m_ReflectionCameraRenderer = null;
        }
    }
}