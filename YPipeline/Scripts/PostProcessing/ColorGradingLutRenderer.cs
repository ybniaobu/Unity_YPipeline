using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ColorGradingLutRenderer : PostProcessingRenderer
    {
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
        }

        public override void Render(ref YPipelineData data)
        {
            isActivated = true;
            data.cmd.BeginSample("Color Grading Lut");
            
            var stack = VolumeManager.instance.stack;
            m_GlobalColorCorrections = stack.GetComponent<GlobalColorCorrections>();
            m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_LiftGammaGain = stack.GetComponent<LiftGammaGain>();
            m_ToneMapping = stack.GetComponent<ToneMapping>();
            
            int lutHeight = data.asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_ColorGradingLutTextureID, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_ColorGradingLUTParamsID, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));
            
            // Global Color Corrections
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_WhiteBalanceID, ColorUtils.ColorBalanceToLMSCoeffs(m_GlobalColorCorrections.temperature.value, m_GlobalColorCorrections.tint.value));
            
            float hue = m_GlobalColorCorrections.hue.value - 0.5f;
            float exposure = Mathf.Pow(2.0f, m_GlobalColorCorrections.exposure.value);
            float contrast = m_GlobalColorCorrections.contrast.value;
            float saturation = m_GlobalColorCorrections.saturation.value;
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_ColorAdjustmentsParamsID, new Vector4(hue, exposure, contrast, saturation));
            ColorGradingLutMaterial.SetColor(YPipelineShaderIDs.k_ColorFilterID, m_GlobalColorCorrections.colorFilter.value.linear);
            
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveMasterID, m_GlobalColorCorrections.master.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveRedID, m_GlobalColorCorrections.red.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveGreenID, m_GlobalColorCorrections.green.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveBlueID, m_GlobalColorCorrections.blue.value.GetTexture());
            
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveHueVsHueID, m_GlobalColorCorrections.hueVsHue.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveHueVsSatID, m_GlobalColorCorrections.hueVsSat.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveLumVsSatID, m_GlobalColorCorrections.lumVsSat.value.GetTexture());
            ColorGradingLutMaterial.SetTexture(YPipelineShaderIDs.k_CurveSatVsSatID, m_GlobalColorCorrections.satVsSat.value.GetTexture());
            
            // Shadows Midtones Highlights
            var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(m_ShadowsMidtonesHighlights.shadows.value, 
                m_ShadowsMidtonesHighlights.midtones.value, m_ShadowsMidtonesHighlights.highlights.value);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_SMHShadowsID, shadows);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_SMHMidtonesID, midtones);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_SMHHighlightsID, highlights);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_SMHRangeID, new Vector4(m_ShadowsMidtonesHighlights.shadowsStart.value, m_ShadowsMidtonesHighlights.shadowsEnd.value, 
                m_ShadowsMidtonesHighlights.highlightsStart.value, m_ShadowsMidtonesHighlights.highlightsEnd.value));
            
            // Lift Gamma Gain
            var (lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(m_LiftGammaGain.lift.value, m_LiftGammaGain.gamma.value, m_LiftGammaGain.gain.value);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_LGGLiftID, lift);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_LGGGammaID, gamma);
            ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_LGGGainID, gain);
            
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
                    ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_ToneMappingParamsID, new Vector4(m_ToneMapping.minWhite.value, 0.0f));
                    break;
                case TonemappingMode.Uncharted2Filmic:
                    toneMappingPass = 4;
                    ColorGradingLutMaterial.SetVector(YPipelineShaderIDs.k_ToneMappingParamsID, new Vector4(m_ToneMapping.exposureBias.value, 0.0f));
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
            BlitUtility.DrawTexture(data.cmd, YPipelineShaderIDs.k_ColorGradingLutTextureID, ColorGradingLutMaterial, toneMappingPass);
            
            data.cmd.EndSample("Color Grading Lut");
        }
    }
}