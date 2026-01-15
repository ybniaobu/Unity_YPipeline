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
        public SSGIModeParameter mode = new SSGIModeParameter(SSGIMode.None, true);
        
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        // HBIL
        public ClampedFloatParameter hbilIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedFloatParameter convergeDegree = new ClampedFloatParameter(1.5f, 1.0f, 2.0f);
        
        public ClampedIntParameter directionCount = new ClampedIntParameter(3, 1, 6);
        
        public ClampedIntParameter stepCount = new ClampedIntParameter(6, 2, 12);
        
        // Fallback
        public SSGIFallbackModeParameter fallbackMode = new SSGIFallbackModeParameter(SSGIFallbackMode.APV, true);
        
        public ClampedFloatParameter fallbackIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedFloatParameter farFieldAO = new ClampedFloatParameter(0.75f, 0.0f, 2.0f);
        
        // Denoise
        [Tooltip("过滤核半径 Defines the neighborhood area used for weighted averaging. Larger kernel produces stronger blurring effects.")]
        public ClampedIntParameter kernelRadius = new ClampedIntParameter(5, 2, 10);
        
        [Tooltip("标准差 The standard deviation of the Gaussian function, higher value results in blurrier result.")]
        public ClampedFloatParameter sigma = new ClampedFloatParameter(3.0f, 0.0f, 6.0f);
        
        [Tooltip("深度阈值 Rejects pixel averaging when the depth difference is above depth threshold. Lower value achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.05f, 0.0f, 0.2f);
        
        public BoolParameter enableTemporalDenoise = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("方差临界值 Lower value reduces ghosting but produces more noise and flicking, higher value reduces noise but produces more ghosting.")]
        public ClampedFloatParameter criticalValue = new ClampedFloatParameter(1.0f, 0.5f, 1.5f);
        
        public bool IsActive() => mode.value != SSGIMode.None;
    }
}