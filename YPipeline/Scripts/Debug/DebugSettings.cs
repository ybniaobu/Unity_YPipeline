using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class DebugSettings
    {
        public LightingDebugSettings lightingDebugSettings;
        
        public DebugSettings()
        {
            lightingDebugSettings = new LightingDebugSettings();
            lightingDebugSettings.Initialize();
        }
        
        public void Dispose()
        {
            lightingDebugSettings.OnDispose();
        }
    }
#endif
}