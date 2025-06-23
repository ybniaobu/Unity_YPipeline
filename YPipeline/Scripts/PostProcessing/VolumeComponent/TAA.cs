using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Anti-Aliasing/Temporal Anti-Aliasing (TAA)")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class TAA : VolumeComponent, IPostProcessComponent
    {
        
        public bool IsActive() => true;
    }
}