using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class YPipelinePerCameraData
    {
        public int currentCameraID;
        
        // ----------------------------------------------------------------------------------------------------
        // Matrices
        // ----------------------------------------------------------------------------------------------------
        
        // public Matrix4x4 viewMatrix;
        // public Matrix4x4 projectionMatrix;
        public Matrix4x4 jitteredProjectionMatrix;
        
        // ----------------------------------------------------------------------------------------------------
        // TAA History
        // ----------------------------------------------------------------------------------------------------

        private RTHandle m_TAAHistory;

        public RTHandle GetTAAHistory(ref RenderTextureDescriptor desc, string name = "TAAHistory")
        {
            if (m_TAAHistory == null || m_TAAHistory.rt.width != desc.width || m_TAAHistory.rt.height != desc.height
                || m_TAAHistory.rt.graphicsFormat != desc.graphicsFormat)
            {
                m_TAAHistory?.Release();
                m_TAAHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_TAAHistory;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        
        public void Dispose()
        {
            m_TAAHistory?.Release();
            m_TAAHistory = null;
        }
    }
}