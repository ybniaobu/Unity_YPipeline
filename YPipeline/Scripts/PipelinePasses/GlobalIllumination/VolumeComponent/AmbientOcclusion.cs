using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Global Illumination/Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class AmbientOcclusion : VolumeComponent, IPostProcessComponent
    {
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 2, 32);
        
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0.0f, 5.0f);
        
        public bool IsActive() => true;
    }
    
    
}