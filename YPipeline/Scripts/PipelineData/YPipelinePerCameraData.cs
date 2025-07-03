using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class YPipelinePerCameraData
    {
        // ----------------------------------------------------------------------------------------------------
        // Matrices
        // ----------------------------------------------------------------------------------------------------
        
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projectionMatrix;
        public Matrix4x4 jitteredProjectionMatrix;

        public void SetPerCameraDataMatrices(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 jitteredProjectionMatrix)
        {
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
            this.jitteredProjectionMatrix = jitteredProjectionMatrix;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // TAA History
        // ----------------------------------------------------------------------------------------------------

        private RTHandle m_TAAHistory;

        public RTHandle GetTAAHistory(ref RenderTextureDescriptor desc, string name = "TAA History")
        {
            if (m_TAAHistory == null || m_TAAHistory.rt.width != desc.width || m_TAAHistory.rt.height != desc.height)
            {
                m_TAAHistory?.Release();
                m_TAAHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_TAAHistory;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        public YPipelinePerCameraData()
        {
            m_TAAHistory?.Release();
            m_TAAHistory = null;
        }
        
        public void Dispose()
        {
            m_TAAHistory?.Release();
            m_TAAHistory = null;
        }
    }
}