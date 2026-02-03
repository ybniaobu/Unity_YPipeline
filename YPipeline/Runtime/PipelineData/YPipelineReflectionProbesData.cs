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

        public Vector2Int atlasSize;
        public int probeCount;
        
        public Vector4[] boxCenter = new Vector4[k_MaxReflectionProbeCount]; // xyz: box center, w: importance
        public Vector4[] boxExtent = new Vector4[k_MaxReflectionProbeCount]; // xyz: box extent, w: box projection
        public Vector4[] SH = new Vector4[k_MaxReflectionProbeCount * 7]; // For reflection probe normalization
        public Vector4[] probeParams  = new Vector4[k_MaxReflectionProbeCount]; // xy: uv in atlas, z: height, w: intensity
        public Texture[] octahedralAtlas = new Texture[k_MaxReflectionProbeCount];
        
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