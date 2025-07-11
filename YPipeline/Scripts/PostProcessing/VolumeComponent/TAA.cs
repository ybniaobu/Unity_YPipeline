﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    public enum TAANeighborhood
    {
        [InspectorName("3X3 (9 taps)")] _3X3, [InspectorName("Cross (5 taps)")] Cross
    }
    
    public enum TAAColorSpace
    {
        [InspectorName("YCoCg")] YCoCg, RGB
    }

    public enum ColorRectifyMode
    {
        AABBClamp, AABBClipToCenter, AABBClipToFiltered, VarianceClip
    }
    
    public enum HistoryFilter
    {
        Linear, Bicubic, CatmullRom
    }
    
    
    
    [System.Serializable]
    public sealed class TAANeighborhoodParameter : VolumeParameter<TAANeighborhood>
    {
        public TAANeighborhoodParameter(TAANeighborhood value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class TAAColorSpaceParameter : VolumeParameter<TAAColorSpace>
    {
        public TAAColorSpaceParameter(TAAColorSpace value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class ColorRectifyModeParameter : VolumeParameter<ColorRectifyMode>
    {
        public ColorRectifyModeParameter(ColorRectifyMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class HistoryFilterParameter : VolumeParameter<HistoryFilter>
    {
        public HistoryFilterParameter(HistoryFilter value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Anti-Aliasing/Temporal Anti-Aliasing (TAA)")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class TAA : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("抖动范围参数，默认 1.0 即抖动一个像素，值越大越模糊 Smaller value results in less flicker but more alias")]
        public ClampedFloatParameter jitterScale = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("历史帧混合系数 Determines how much the history is blended with the current frame")]
        public ClampedFloatParameter historyBlendFactor = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        
        [Tooltip("采样模式 Using a 3X3 or crossed(5 taps) neighborhood samples")]
        public TAANeighborhoodParameter neighborhood = new TAANeighborhoodParameter(TAANeighborhood._3X3);
        
        [Tooltip("XXXXXXXXXXXXXXXX")]
        public TAAColorSpaceParameter colorSpace = new TAAColorSpaceParameter(TAAColorSpace.YCoCg);
        
        [Tooltip("修正历史颜色的方式 Rectify invalid history by clamp or clip to the range of neighborhood samples")]
        public ColorRectifyModeParameter colorRectifyMode = new ColorRectifyModeParameter(ColorRectifyMode.VarianceClip);
        
        [Tooltip("过滤历史以减少模糊 Filtering history to reduce reprojection blur")]
        public HistoryFilterParameter historyFilter = new HistoryFilterParameter(HistoryFilter.CatmullRom);
        
        public bool IsActive() => true;
    }
}