using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum AmbientOcclusionMode
    {
        None, SSAO, HBAO, GTAO
    }
    
    [System.Serializable]
    public sealed class AmbientOcclusionModeParameter : VolumeParameter<AmbientOcclusionMode>
    {
        public AmbientOcclusionModeParameter(AmbientOcclusionMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Global Illumination/Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class AmbientOcclusion : VolumeComponent, IPostProcessComponent
    {
        public AmbientOcclusionModeParameter ambientOcclusionMode = new AmbientOcclusionModeParameter(AmbientOcclusionMode.SSAO, true);
        
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 2, 16);
        
        public ClampedFloatParameter radius = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        
        public bool IsActive() => true;
    }
    
    
}