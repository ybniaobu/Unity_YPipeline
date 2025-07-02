using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Anti-Aliasing/Temporal Anti-Aliasing (TAA)")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class TAA : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("抖动范围参数，默认 1.0 即抖动一个像素，值越大越模糊 Smaller value results in less flicker but more alias")]
        public ClampedFloatParameter jitterScale = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("历史帧混合系数 Determines how much the history is blended with the current frame")]
        public ClampedFloatParameter historyBlendFactor = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        
        public bool IsActive() => true;
    }
}