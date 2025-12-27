using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum ScreenSpaceDiffuseGIMode
    {
        None, HBGI, SSGI
    }
    
    [System.Serializable]
    public sealed class ScreenSpaceDiffuseGIModeParameter : VolumeParameter<ScreenSpaceDiffuseGIMode>
    {
        public ScreenSpaceDiffuseGIModeParameter(ScreenSpaceDiffuseGIMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public class ScreenSpaceDiffuseGlobalIllumination : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("屏幕空间漫反射全局光照算法 Choose a screen space diffuse global illumination algorithm.")]
        public ScreenSpaceDiffuseGIModeParameter mode = new ScreenSpaceDiffuseGIModeParameter(ScreenSpaceDiffuseGIMode.HBGI, true);
        
        // HBIL
        public ClampedFloatParameter hbilIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 4, 32);
        
        public ClampedFloatParameter hbilRadius = new ClampedFloatParameter(2.0f, 0.0f, 5.0f);
        
        
        
        public bool IsActive() => mode.value != ScreenSpaceDiffuseGIMode.None;
    }
}