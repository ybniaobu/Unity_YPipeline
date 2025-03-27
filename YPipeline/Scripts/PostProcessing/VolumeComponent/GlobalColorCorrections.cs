using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("YPipeline Post Processing/Global Color Corrections")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class GlobalColorCorrections : VolumeComponent, IPostProcessComponent
    {
        [Header("White Balance")]
        [Tooltip("Sets the white balance to a custom color temperature.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);
        
        [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);
        
        [Header("Color Adjustments")]
        [Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);
        
        [Tooltip("Shift the hue of all colors.")]
        public ClampedFloatParameter hue = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("Adjusts the overall exposure of the scene in EV100. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter exposure = new ClampedFloatParameter(0.0f, -10.0f, 10.0f);
        
        [Tooltip("Expands or shrinks the overall range of tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("Pushes the intensity of all colors.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        // TODO: [Header("Color Curve")]
        
        public bool IsActive() => true;
    }
}