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
        
        private static readonly int k_CurveMaster  = Shader.PropertyToID("_CurveMaster");
        private static readonly int k_CurveRed = Shader.PropertyToID("_CurveRed");
        private static readonly int k_CurveGreen = Shader.PropertyToID("_CurveGreen");
        private static readonly int k_CurveBlue = Shader.PropertyToID("_CurveBlue");
        private static readonly int k_CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");
        private static readonly int k_CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");
        private static readonly int k_CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");
        private static readonly int k_CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");
        
        private static readonly int k_SMHShadowsID = Shader.PropertyToID("_SMHShadows");
        private static readonly int k_SMHMidtonesID = Shader.PropertyToID("_SMHMidtones");
        private static readonly int k_SMHHighlightsID = Shader.PropertyToID("_SMHHighlights");
        private static readonly int k_SMHRangeID = Shader.PropertyToID("_SMHRange");
        
        private static readonly int k_LGGLiftID = Shader.PropertyToID("_LGGLift");
        private static readonly int k_LGGGammaID = Shader.PropertyToID("_LGGGamma");
        private static readonly int k_LGGGainID = Shader.PropertyToID("_LGGGain");
        
        private static readonly int k_ToneMappingParamsId = Shader.PropertyToID("_ToneMappingParams");
        
        private GlobalColorCorrections m_GlobalColorCorrections;
        private ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;
        private LiftGammaGain m_LiftGammaGain;
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
            m_LiftGammaGain = stack.GetComponent<LiftGammaGain>();
            m_ToneMapping = stack.GetComponent<ToneMapping>();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Color Grading Lut");
            
            int lutHeight = asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            data.buffer.GetTemporaryRT(RenderTargetIDs.k_ColorGradingLutTextureId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            ColorGradingLutMaterial.SetVector(k_ColorGradingLUTParamsId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));
            
            // Global Color Corrections
            ColorGradingLutMaterial.SetVector(k_WhiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(m_GlobalColorCorrections.temperature.value, m_GlobalColorCorrections.tint.value));
            
            float hue = m_GlobalColorCorrections.hue.value - 0.5f;
            float exposure = Mathf.Pow(2.0f, m_GlobalColorCorrections.exposure.value);
            float contrast = m_GlobalColorCorrections.contrast.value;
            float saturation = m_GlobalColorCorrections.saturation.value;
            ColorGradingLutMaterial.SetVector(k_ColorAdjustmentsParamsId, new Vector4(hue, exposure, contrast, saturation));
            ColorGradingLutMaterial.SetColor(k_ColorFilterId, m_GlobalColorCorrections.colorFilter.value.linear);
            
            ColorGradingLutMaterial.SetTexture(k_CurveMaster, m_GlobalColorCorrections.master.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveRed, m_GlobalColorCorrections.red.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveGreen, m_GlobalColorCorrections.green.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveBlue, m_GlobalColorCorrections.blue.value.GetTexture());
            
            ColorGradingLutMaterial.SetTexture(k_CurveHueVsHue, m_GlobalColorCorrections.hueVsHue.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveHueVsSat, m_GlobalColorCorrections.hueVsSat.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveLumVsSat, m_GlobalColorCorrections.lumVsSat.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(k_CurveSatVsSat, m_GlobalColorCorrections.satVsSat.value.GetTexture());
            
            // Shadows Midtones Highlights
            var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(m_ShadowsMidtonesHighlights.shadows.value, 
                m_ShadowsMidtonesHighlights.midtones.value, m_ShadowsMidtonesHighlights.highlights.value);
            ColorGradingLutMaterial.SetVector(k_SMHShadowsID, shadows);
            ColorGradingLutMaterial.SetVector(k_SMHMidtonesID, midtones);
            ColorGradingLutMaterial.SetVector(k_SMHHighlightsID, highlights);
            ColorGradingLutMaterial.SetVector(k_SMHRangeID, new Vector4(m_ShadowsMidtonesHighlights.shadowsStart.value, m_ShadowsMidtonesHighlights.shadowsEnd.value, 
                m_ShadowsMidtonesHighlights.highlightsStart.value, m_ShadowsMidtonesHighlights.highlightsEnd.value));
            
            // Lift Gamma Gain
            var (lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(m_LiftGammaGain.lift.value, m_LiftGammaGain.gamma.value, m_LiftGammaGain.gain.value);
            ColorGradingLutMaterial.SetVector(k_LGGLiftID, lift);
            ColorGradingLutMaterial.SetVector(k_LGGGammaID, gamma);
            ColorGradingLutMaterial.SetVector(k_LGGGainID, gain);
            
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
                    ColorGradingLutMaterial.SetVector(k_ToneMappingParamsId, new Vector4(m_ToneMapping.minWhite.value, 0.0f));
                    break;
                case TonemappingMode.Uncharted2Filmic:
                    toneMappingPass = 4;
                    ColorGradingLutMaterial.SetVector(k_ToneMappingParamsId, new Vector4(m_ToneMapping.exposureBias.value, 0.0f));
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