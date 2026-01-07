using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum SSGIMode
    {
        None, HBIL, SSGI
    }
    
    [System.Serializable]
    public sealed class SSGIModeParameter : VolumeParameter<SSGIMode>
    {
        public SSGIModeParameter(SSGIMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public class ScreenSpaceGlobalIllumination : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("屏幕空间漫反射全局光照算法 Choose a screen space diffuse global illumination algorithm.")]
        public SSGIModeParameter mode = new SSGIModeParameter(SSGIMode.HBIL, true);
        
        // HBIL
        public ClampedFloatParameter hbilIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedFloatParameter hbilRadius = new ClampedFloatParameter(2.0f, 0.0f, 20.0f);
        
        public ClampedIntParameter hbilDirectionCount = new ClampedIntParameter(4, 2, 8);
        
        public ClampedIntParameter hbilStepCount = new ClampedIntParameter(4, 2, 8);
        
        
        public bool IsActive() => mode.value != SSGIMode.None;
    }
}