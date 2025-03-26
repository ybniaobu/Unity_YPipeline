using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("YPipeline Post Processing/Color Grading")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class ColorGrading : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(true, true);
        
        [Header("White Balance")]
        [Tooltip("Sets the white balance to a custom color temperature.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);
        
        [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);
        
        [Header("Global Color Adjustments")]
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
        
        //[Header("Shadows Midtones Highlights")]
        
        public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        
        [Tooltip("Start point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);
        
        [Tooltip("End point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsEnd = new MinFloatParameter(0.5f, 0f);
        
        [Tooltip("Start point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsStart = new MinFloatParameter(0.5f, 0f);
        
        [Tooltip("End point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);
        
        public bool IsActive() => enable.value;
    }

    public class ColorGradingRenderer : PostProcessingRenderer<ColorGrading>
    {
        private static readonly int k_ColorAdjustmentsParamsId = Shader.PropertyToID("_ColorAdjustmentsParams");
        private static readonly int k_ColorFilterId = Shader.PropertyToID("_ColorFilter");
        private static readonly int k_WhiteBalanceId = Shader.PropertyToID("_WhiteBalance");
        private static readonly int k_SMHShadowsID = Shader.PropertyToID("_SMHShadows");
        private static readonly int k_SMHMidtonesID = Shader.PropertyToID("_SMHMidtones");
        private static readonly int k_SMHHighlightsID = Shader.PropertyToID("_SMHHighlights");
        private static readonly int k_SMHRangeID = Shader.PropertyToID("_SMHRange");
        
        private const string k_ColorGrading = "Hidden/YPipeline/ColorGrading";
        private Material m_ColorGradingMaterial;

        private Material ColorGradingMaterial
        {
            get
            {
                if (m_ColorGradingMaterial == null)
                {
                    m_ColorGradingMaterial = new Material(Shader.Find(k_ColorGrading));
                    m_ColorGradingMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_ColorGradingMaterial;
            }
        }
        
        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (!settings.IsActive())
            {
                isActivated = false;
                return;
            }
            
            isActivated = true;
            data.buffer.BeginSample("Color Grading");
            
            // Shader property and keyword setup
            data.buffer.SetGlobalVector(k_WhiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(settings.temperature.value, settings.tint.value));
            
            float hue = settings.hue.value - 0.5f;
            float exposure = Mathf.Pow(2.0f, settings.exposure.value);
            float contrast = settings.contrast.value;
            float saturation = settings.saturation.value;
            data.buffer.SetGlobalVector(k_ColorAdjustmentsParamsId, new Vector4(hue, exposure, contrast, saturation));
            data.buffer.SetGlobalColor(k_ColorFilterId, settings.colorFilter.value);
            
            var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(settings.shadows.value, settings.midtones.value, settings.highlights.value);
            data.buffer.SetGlobalVector(k_SMHShadowsID, shadows);
            data.buffer.SetGlobalVector(k_SMHMidtonesID, midtones);
            data.buffer.SetGlobalVector(k_SMHHighlightsID, highlights);
            data.buffer.SetGlobalVector(k_SMHRangeID, new Vector4(settings.shadowsStart.value, settings.shadowsEnd.value, settings.highlightsStart.value, settings.highlightsEnd.value));
            
            // Blit
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_BloomTextureId,  RenderTargetIDs.k_ColorGradingTextureId, ColorGradingMaterial, 0);
            
            data.buffer.EndSample("Color Grading");
        }
    }
}