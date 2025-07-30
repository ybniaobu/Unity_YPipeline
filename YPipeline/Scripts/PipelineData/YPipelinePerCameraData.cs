using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class YPipelinePerCameraData : IDisposable
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
        
        private RTHandle m_MotionVectorHistory;

        public RTHandle GetMotionVectorHistory(ref RenderTextureDescriptor desc, string name = "Motion Vector History")
        {
            if (m_MotionVectorHistory == null || m_MotionVectorHistory.rt.width != desc.width || m_MotionVectorHistory.rt.height != desc.height)
            {
                m_MotionVectorHistory?.Release();
                m_MotionVectorHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_MotionVectorHistory;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------
        
        bool m_Disposed = false;
        
        public YPipelinePerCameraData()
        {
            m_IsPerCameraDataReset = true;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~YPipelinePerCameraData()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    //Dispose managed resources
                }
                //Dispose unmanaged resources
                m_TAAHistory?.Release();
                m_TAAHistory = null;
                m_MotionVectorHistory?.Release();
                m_MotionVectorHistory = null;
            }
            
            m_IsPerCameraDataReset = true;
            m_Disposed = true;
        }
    }
}