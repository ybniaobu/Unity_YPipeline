using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    public enum AmbientOcclusionMode
    {
        None, SSAO, [InspectorName("HBAO(Not Recommended)")] HBAO, GTAO
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
        [Tooltip("屏幕空间环境光遮蔽算法 Choose a screen space ambient occlusion algorithm.")]
        public AmbientOcclusionModeParameter ambientOcclusionMode = new AmbientOcclusionModeParameter(AmbientOcclusionMode.GTAO, true);
        
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        [Tooltip("遮蔽强度 Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        // SSAO
        [Tooltip("采样次数 Increase the amount of samples will produce higher quality results at a cost of lower performance.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 4, 32);
        
        [Tooltip("采样半径 Sampling radius. Bigger the radius, wider ambient occlusion will be achieved.")]
        public ClampedFloatParameter ssaoRadius = new ClampedFloatParameter(2.0f, 0.0f, 5.0f);
        
        // HBAO
        [Tooltip("采样半径 Sampling radius. Bigger the radius, wider ambient occlusion will be achieved.")]
        public ClampedFloatParameter hbaoRadius = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);
        
        // Spatial Filter
        public BoolParameter enableSpatialFilter = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("过滤核半径 Defines the neighborhood area used for weighted averaging. Larger kernel produces stronger blurring effects.")]
        public ClampedIntParameter kernelRadius = new ClampedIntParameter(4, 2, 8);
        
        [Tooltip("空域标准差 The smoothing parameter for spatial kernel, higher value results in blurrier result.")]
        public ClampedFloatParameter spatialSigma = new ClampedFloatParameter(0.6f, 0.0f, 5.0f);
        
        [Tooltip("值域标准差 The smoothing parameter for range kernel, lower value achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter depthSigma = new ClampedFloatParameter(0.25f, 0.0f, 0.5f);
        
        // Temporal Filter
        public BoolParameter enableTemporalFilter = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("Lower value reduces ghosting but produces more noise and flicking, higher value reduces noise but produces more ghosting.")]
        public ClampedFloatParameter criticalValue = new ClampedFloatParameter(1.0f, 0.5f, 1.5f);
        
        public bool IsActive() => ambientOcclusionMode.value != AmbientOcclusionMode.None;
    }
}