using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum SSGIMode
    {
        None, HBIL, SSGI
    }

    public enum SSGIFallbackMode
    {
        APV = 0, AmbientProbe = 1
    }
    
    [System.Serializable]
    public sealed class SSGIModeParameter : VolumeParameter<SSGIMode>
    {
        public SSGIModeParameter(SSGIMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class SSGIFallbackModeParameter : VolumeParameter<SSGIFallbackMode>
    {
        public SSGIFallbackModeParameter(SSGIFallbackMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public class ScreenSpaceGlobalIllumination : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("屏幕空间漫反射全局光照算法 Choose a screen space diffuse global illumination algorithm.")]
        public SSGIModeParameter mode = new SSGIModeParameter(SSGIMode.HBIL, true);
        
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        // HBIL
        public ClampedFloatParameter hbilIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedFloatParameter convergeDegree = new ClampedFloatParameter(1.5f, 1.0f, 2.0f);
        
        public ClampedIntParameter directionCount = new ClampedIntParameter(2, 1, 6);
        
        public ClampedIntParameter stepCount = new ClampedIntParameter(4, 2, 12);
        
        // Fallback
        public SSGIFallbackModeParameter fallbackMode = new SSGIFallbackModeParameter(SSGIFallbackMode.APV, true);
        
        public ClampedFloatParameter fallbackIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedFloatParameter farFieldAO = new ClampedFloatParameter(0.75f, 0.0f, 2.0f);
        
        public bool IsActive() => mode.value != SSGIMode.None;
    }
}