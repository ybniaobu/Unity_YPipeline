using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class DeferredResourcesPass : PipelinePass
    {
        private class DeferredResourcesPassData
        {
            // Global Constant Buffer Variables
            public Vector2Int bufferSize;
            public Vector4 jitter;
            public Vector4 timeParams;
            public Vector4 cascadeSettings;
            public Vector4 shadowMapSizes;
        }
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        private RTHandle m_BlueNoise64;
            
        protected override void Initialize() { }

        protected override void OnDispose()
        {
            RTHandles.Release(m_CameraColorTarget);
            RTHandles.Release(m_CameraDepthTarget);
            m_CameraColorTarget = null;
            m_CameraDepthTarget = null;
            
            RTHandles.Release(m_EnvBRDFLut);
            RTHandles.Release(m_BlueNoise64);
            m_EnvBRDFLut = null;
            m_BlueNoise64 = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            // ----------------------------------------------------------------------------------------------------
            // GBuffers
            // GBuffer0 -- RGBA8_SRGB: albedo, AO
            // GBuffer1 -- RGBA8_UNORM: normal, material ID
            // GBuffer2 -- RGBA8_UNORM: Reflectance, Roughness, Metallic
            // GBuffer3 -- R11G11B10_FLOAT: Emission
            // GBUffer4 -- R11G11B10_FLOAT: LightMap/APV
            // ----------------------------------------------------------------------------------------------------
        }
    }
}