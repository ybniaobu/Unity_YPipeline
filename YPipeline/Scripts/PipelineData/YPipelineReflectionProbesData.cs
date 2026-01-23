using System;
using UnityEngine;

namespace YPipeline
{
    public class YPipelineReflectionProbesData : IDisposable
    {
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        public const int k_MaxReflectionProbeCount = 16;
        
        // ----------------------------------------------------------------------------------------------------
        // Data
        // ----------------------------------------------------------------------------------------------------
        
        public Vector4[] boxCenter = new Vector4[k_MaxReflectionProbeCount]; // xyz: box center, w: importance
        public Vector4[] boxExtent = new Vector4[k_MaxReflectionProbeCount]; // xyz: box extent, w: blend distance
        public Vector4[] SH = new Vector4[k_MaxReflectionProbeCount * 7]; // For Reflection Probe Normalization
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------
        
        private bool m_Disposed = false;
        
        ~YPipelineReflectionProbesData()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    boxCenter = null;
                    boxExtent = null;
                }
            }
            m_Disposed = true;
        }
    }
}