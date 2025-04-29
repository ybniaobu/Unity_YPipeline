using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class UberPostProcessingRenderer : PostProcessingRenderer
    {
        private ChromaticAberration m_ChromaticAberration;
        private Bloom m_Bloom;
        private Vignette m_Vignette;
        private LookupTable m_LookupTable;
        
        private const string k_UberPostProcessing = "Hidden/YPipeline/UberPostProcessing";
        private Material m_UberPostProcessingMaterial;

        private Material UberPostProcessingMaterial
        {
            get
            {
                if (m_UberPostProcessingMaterial == null)
                {
                    m_UberPostProcessingMaterial = new Material(Shader.Find(k_UberPostProcessing));
                    m_UberPostProcessingMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_UberPostProcessingMaterial;
            }
        }

        private Texture2D m_InternalSpectralLut;

        private Texture2D InternalSpectralLut
        {
            get
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
                    {
                        name = "Chromatic Aberration Spectral LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new[]
                    {
                        new Color(1f, 0f, 0f, 1f),
                        new Color(0f, 1f, 0f, 1f),
                        new Color(0f, 0f, 1f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                return m_InternalSpectralLut;
            }
        }
        
        protected override void Initialize()
        {
            base.Initialize();
        }

        public override void Render(ref YPipelineData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Uber Post Processing");
            
            var stack = VolumeManager.instance.stack;
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();
            
            // Chromatic Aberration
            CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_ChromaticAberration, m_ChromaticAberration.IsActive());
            UberPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_SpectralLutID, InternalSpectralLut);
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ChromaticAberrationParamsID, new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples.value));
            
            // Bloom
            CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_Bloom, m_Bloom.IsActive());
            CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_BloomBicubicUpsampling, m_Bloom.bicubicUpsampling.value);
            Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.intensity.value, 0.0f) : new Vector4(m_Bloom.finalIntensity.value, 1.0f);
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_BloomParamsID, bloomParams);
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = threshold * m_Bloom.thresholdKnee.value;
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_BloomThresholdID, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f)));
            data.buffer.SetGlobalTexture(YPipelineShaderIDs.k_BloomTexID, new RenderTargetIdentifier(YPipelineShaderIDs.k_BloomTextureID));
            
            // Vignette
            CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_Vignette, m_Vignette.IsActive());
            float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
            float aspectRatio = data.camera.aspect;
            Vector4 vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
            Vector4 vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? aspectRatio : 1f);
            UberPostProcessingMaterial.SetColor(YPipelineShaderIDs.k_VignetteColorID, m_Vignette.color.value);
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_VignetteParams1ID, vignetteParams1);
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_VignetteParams2ID, vignetteParams2);
            
            // Baked Color Grading Lut
            int lutHeight = data.asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ColorGradingLutParamsID, new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f));
            
            // Extra Lut
            CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_ExtraLut, m_LookupTable.IsActive());
            if (m_LookupTable.IsActive())
            {
                UberPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_ExtraLutID, m_LookupTable.texture.value);
                Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ExtraLutParamsID, extraLutParams);
            }
            
            // Blit
            BlitUtility.BlitTexture(data.buffer, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_FinalTextureID, UberPostProcessingMaterial, 0);
            
            data.buffer.EndSample("Uber Post Processing");
        }
    }
}