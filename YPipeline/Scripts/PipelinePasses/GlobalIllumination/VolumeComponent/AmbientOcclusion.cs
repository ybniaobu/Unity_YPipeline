using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Global Illumination/Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class AmbientOcclusion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("XXXXXXXXXXXXXXXXX")]
        public MinFloatParameter intensity = new MinFloatParameter(0.5f, 0.0f, true);
        
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0.0f, 10.0f);
        
        public ClampedIntParameter sampleCount = new ClampedIntParameter(32, 8, 64);
        
        public bool IsActive() => true;
    }
    
    
}