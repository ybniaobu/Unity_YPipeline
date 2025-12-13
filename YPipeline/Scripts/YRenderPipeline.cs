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
        // Store locally the value on the instance due as the Render Pipeline Asset data might change before the disposal of the asset, making some APV Resources leak.
        private bool m_APVIsEnabled = false;
        
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
            
            // APV
            m_APVIsEnabled = asset != null && asset.supportProbeVolume;
            SupportedRenderingFeatures.active.overridesLightProbeSystem = m_APVIsEnabled;
            SupportedRenderingFeatures.active.skyOcclusion = m_APVIsEnabled;
            if (m_APVIsEnabled)
            {
                ProbeVolumeSystemParameters apvParams = new ProbeVolumeSystemParameters()
                {
                    memoryBudget = asset.probeVolumeMemoryBudget,
                    blendingMemoryBudget = asset.probeVolumeBlendingMemoryBudget,
                    shBands = asset.probeVolumeSHBands,
                    supportGPUStreaming = asset.supportProbeVolumeGPUStreaming,
                    supportDiskStreaming = asset.supportProbeVolumeDiskStreaming,
                    supportScenarios = asset.supportProbeVolumeScenarios,
                    supportScenarioBlending = asset.supportProbeVolumeScenarioBlending,
                };
                ProbeReferenceVolume.instance.Initialize(apvParams);
            }
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
            if (m_APVIsEnabled) ProbeReferenceVolume.instance.Cleanup();
            
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