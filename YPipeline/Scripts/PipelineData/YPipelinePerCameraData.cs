using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class YPipelinePerCameraData
    {
        private bool m_IsPerCameraDataReset;
        
        // ----------------------------------------------------------------------------------------------------
        // Matrices
        // ----------------------------------------------------------------------------------------------------
        
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projectionMatrix;
        public Matrix4x4 jitteredProjectionMatrix;
        public Matrix4x4 previousViewMatrix;
        public Matrix4x4 previousProjectionMatrix;
        public Matrix4x4 previousJitteredProjectionMatrix;

        public void SetPerCameraDataMatrices(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 jitteredProjectionMatrix)
        {
            if (Time.frameCount == 1 || m_IsPerCameraDataReset)
            {
                this.previousViewMatrix = viewMatrix;
                this.previousProjectionMatrix = projectionMatrix;
                this.previousJitteredProjectionMatrix = jitteredProjectionMatrix;
                m_IsPerCameraDataReset = false;
            }
            else
            {
                this.previousViewMatrix = this.viewMatrix;
                this.previousProjectionMatrix = this.projectionMatrix;
                this.previousJitteredProjectionMatrix = this.jitteredProjectionMatrix;
            }
            
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
                IsTAAHistoryReset = true;
                m_TAAHistory?.Release();
                m_TAAHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_TAAHistory;
        }
        
        public bool IsTAAHistoryReset { get; set; }
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // ----------------------------------------------------------------------------------------------------
        public YPipelinePerCameraData()
        {
            m_IsPerCameraDataReset = true;
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