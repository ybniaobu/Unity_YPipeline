using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ColorGradingLutRenderer : PostProcessingRenderer
    {
        private static readonly int k_ColorGradingLUTParamsId = Shader.PropertyToID("_ColorGradingLUTParams");
        
        private static readonly int k_ColorAdjustmentsParamsId = Shader.PropertyToID("_ColorAdjustmentsParams");
        private static readonly int k_ColorFilterId = Shader.PropertyToID("_ColorFilter");
        private static readonly int k_WhiteBalanceId = Shader.PropertyToID("_WhiteBalance");
        private static readonly int k_SMHShadowsID = Shader.PropertyToID("_SMHShadows");
        private static readonly int k_SMHMidtonesID = Shader.PropertyToID("_SMHMidtones");
        private static readonly int k_SMHHighlightsID = Shader.PropertyToID("_SMHHighlights");
        private static readonly int k_SMHRangeID = Shader.PropertyToID("_SMHRange");
        
        private static readonly int k_ToneMappingParamsId = Shader.PropertyToID("_ToneMappingParams");
        
        private GlobalColorCorrections m_GlobalColorCorrections;
        private ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;
        private ToneMapping m_ToneMapping;
        
        private const string k_ColorGradingLut = "Hidden/YPipeline/ColorGradingLut";
        private Material m_ColorGradingLutMaterial;

        private Material ColorGradingLutMaterial
        {
            get
            {
                if (m_ColorGradingLutMaterial == null)
                {
                    m_ColorGradingLutMaterial = new Material(Shader.Find(k_ColorGradingLut));
                    m_ColorGradingLutMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_ColorGradingLutMaterial;
            }
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            var stack = VolumeManager.instance.stack;
            m_GlobalColorCorrections = stack.GetComponent<GlobalColorCorrections>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_ToneMapping = stack.GetComponent<ToneMapping>();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Color Grading Lut");
            
            int lutHeight = asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_ColorGradingLutTextureId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            data.buffer.SetGlobalVector(k_ColorGradingLUTParamsId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));
            
            // Global Color Corrections
            data.buffer.SetGlobalVector(k_WhiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(m_GlobalColorCorrections.temperature.value, m_GlobalColorCorrections.tint.value));
            
            float hue = m_GlobalColorCorrections.hue.value - 0.5f;
            float exposure = Mathf.Pow(2.0f, m_GlobalColorCorrections.exposure.value);
            float contrast = m_GlobalColorCorrections.contrast.value;
            float saturation = m_GlobalColorCorrections.saturation.value;
            data.buffer.SetGlobalVector(k_ColorAdjustmentsParamsId, new Vector4(hue, exposure, contrast, saturation));
            data.buffer.SetGlobalColor(k_ColorFilterId, m_GlobalColorCorrections.colorFilter.value);
            
            // Shadows Midtones Highlights
            var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(m_ShadowsMidtonesHighlights.shadows.value, 
                m_ShadowsMidtonesHighlights.midtones.value, m_ShadowsMidtonesHighlights.highlights.value);
            data.buffer.SetGlobalVector(k_SMHShadowsID, shadows);
            data.buffer.SetGlobalVector(k_SMHMidtonesID, midtones);
            data.buffer.SetGlobalVector(k_SMHHighlightsID, highlights);
            data.buffer.SetGlobalVector(k_SMHRangeID, new Vector4(m_ShadowsMidtonesHighlights.shadowsStart.value, m_ShadowsMidtonesHighlights.shadowsEnd.value, 
                m_ShadowsMidtonesHighlights.highlightsStart.value, m_ShadowsMidtonesHighlights.highlightsEnd.value));
            
            // Tone Mapping
            TonemappingMode mode = m_ToneMapping.mode.value;
            int toneMappingPass;

            switch (mode)
            {
                case TonemappingMode.Reinhard:
                    ReinhardMode reinhardMode = m_ToneMapping.reinhardMode.value;
                    if (reinhardMode == ReinhardMode.Simple) toneMappingPass = 1;
                    else if (reinhardMode == ReinhardMode.Extended) toneMappingPass = 2;
                    else toneMappingPass = 3;
                    data.buffer.SetGlobalVector(k_ToneMappingParamsId, new Vector4(m_ToneMapping.minWhite.value, 0.0f));
                    break;
                case TonemappingMode.Uncharted2Filmic:
                    toneMappingPass = 4;
                    data.buffer.SetGlobalVector(k_ToneMappingParamsId, new Vector4(m_ToneMapping.exposureBias.value, 0.0f));
                    break;
                case TonemappingMode.KhronosPBRNeutral:
                    toneMappingPass = 5;
                    break;
                case TonemappingMode.ACES:
                    ACESMode acesMode = m_ToneMapping.aCESMode.value;
                    if (acesMode == ACESMode.Full) toneMappingPass = 6;
                    else if (acesMode == ACESMode.StephenHillFit) toneMappingPass = 7;
                    else toneMappingPass = 8;
                    break;
                case TonemappingMode.AGXApproximation:
                    AgXMode agxMode = m_ToneMapping.aGXMode.value;
                    if (agxMode == AgXMode.Default) toneMappingPass = 9;
                    else if (agxMode == AgXMode.Golden) toneMappingPass = 10;
                    else toneMappingPass = 11;
                    break;
                default:
                    toneMappingPass = 0;
                    break;
            }
            
            // Blit
            BlitUtility.DrawTexture(data.buffer, RenderTargetIDs.k_ColorGradingLutTextureId, ColorGradingLutMaterial, toneMappingPass);
            
            data.buffer.EndSample("Color Grading Lut");
        }
    }
}