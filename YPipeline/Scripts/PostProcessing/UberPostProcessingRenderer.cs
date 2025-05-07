using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class UberPostProcessingRenderer : PostProcessingRenderer
    {
        private class UberPostProcessingData
        {
            public TextureHandle colorBuffer;
            public TextureHandle finalTexture;
            
            public bool isChromaticAberrationEnabled;
            public TextureHandle spectralLut;
            public Vector4 chromaticAberrationParams;
            
            public bool isBloomEnabled;
            public TextureHandle bloomTexture;
            public bool isBloomBicubicUpsampling;
            public Vector4 bloomParams;
            public Vector4 bloomThreshold;
            
            public bool isVignetteEnabled;
            public Color vignetteColor;
            public Vector4 vignetteParams1;
            public Vector4 vignetteParams2;

            public Vector4 colorGradingLutParams;
            
            public bool isExtraLutEnabled;
            public TextureHandle extraLut;
            public Vector4 extraLutParams;
        }
        
        private ChromaticAberration m_ChromaticAberration;
        private Bloom m_Bloom;
        private Vignette m_Vignette;
        private LookupTable m_LookupTable;

        private RTHandle m_SpectralLut;
        private RTHandle m_ExtraLut;
        
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
                        name = "ChromaticAberrationSpectralLUT",
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
            data.cmd.BeginSample("Uber Post Processing");
            
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
            BlitUtility.BlitTexture(data.cmd, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_FinalTextureID, UberPostProcessingMaterial, 0);
            
            data.cmd.EndSample("Uber Post Processing");
        }
        
        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();

            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<UberPostProcessingData>("Uber Post Processing", out var nodeData))
            {
                // Chromatic Aberration
                nodeData.isChromaticAberrationEnabled = m_ChromaticAberration.IsActive();
                if (m_ChromaticAberration.IsActive())
                {
                    if (m_SpectralLut == null || m_SpectralLut.externalTexture != InternalSpectralLut)
                    {
                        m_SpectralLut?.Release();
                        m_SpectralLut = RTHandles.Alloc(InternalSpectralLut);
                    }
                    
                    TextureHandle spectralLut = data.renderGraph.ImportTexture(m_SpectralLut);
                    nodeData.spectralLut = spectralLut;
                    builder.ReadTexture(spectralLut);
                    
                    nodeData.chromaticAberrationParams = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples.value);
                }
                else
                {
                    m_SpectralLut?.Release();
                }
                
                // Bloom
                nodeData.isBloomEnabled = m_Bloom.IsActive();
                if (m_Bloom.IsActive())
                {
                    nodeData.isBloomBicubicUpsampling = m_Bloom.bicubicUpsampling.value;
                    
                    Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.intensity.value, 0.0f) : new Vector4(m_Bloom.finalIntensity.value, 1.0f);
                    float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                    float knee = threshold * m_Bloom.thresholdKnee.value;
                    nodeData.bloomParams = bloomParams;
                    nodeData.bloomThreshold = new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f));
                }
                
                // Vignette
                nodeData.isVignetteEnabled = m_Vignette.IsActive();
                if (m_Vignette.IsActive())
                {
                    float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                    float aspectRatio = data.camera.aspect;
                    Vector4 vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
                    Vector4 vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? aspectRatio : 1f);
                    nodeData.vignetteColor = m_Vignette.color.value;
                    nodeData.vignetteParams1 = vignetteParams1;
                    nodeData.vignetteParams2 = vignetteParams2;
                }
                
                // Baked Color Grading Lut
                int lutHeight = data.asset.bakedLUTResolution;
                int lutWidth = lutHeight * lutHeight;
                nodeData.colorGradingLutParams = new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f);
                
                // Extra Lut
                nodeData.isExtraLutEnabled = m_LookupTable.IsActive();
                if (m_LookupTable.IsActive())
                {
                    if (m_ExtraLut == null || m_ExtraLut.externalTexture != m_LookupTable.texture.value)
                    {
                        m_ExtraLut?.Release();
                        m_ExtraLut = RTHandles.Alloc(m_LookupTable.texture.value);
                    }
                    
                    TextureHandle extraLut = data.renderGraph.ImportTexture(m_ExtraLut);
                    nodeData.extraLut = extraLut;
                    builder.ReadTexture(extraLut);
                    
                    Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                    nodeData.extraLutParams = extraLutParams;
                }
                else
                {
                    m_ExtraLut?.Release();
                }
                
                builder.SetRenderFunc((UberPostProcessingData data, RenderGraphContext context) =>
                {
                    // Chromatic Aberration
                    CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_ChromaticAberration, data.isChromaticAberrationEnabled);
                    if (data.isChromaticAberrationEnabled)
                    {
                        UberPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_SpectralLutID, data.spectralLut);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ChromaticAberrationParamsID, data.chromaticAberrationParams);
                    }
                    
                    // Bloom
                    CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_Bloom, data.isBloomEnabled);
                    if (data.isBloomEnabled)
                    {
                        CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_BloomBicubicUpsampling, data.isBloomBicubicUpsampling);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_BloomParamsID, data.bloomParams);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_BloomThresholdID, data.bloomThreshold);
                    }
                    
                    // Vignette
                    CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_Vignette, data.isVignetteEnabled);
                    if (data.isVignetteEnabled)
                    {
                        UberPostProcessingMaterial.SetColor(YPipelineShaderIDs.k_VignetteColorID, data.vignetteColor);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_VignetteParams1ID, data.vignetteParams1);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_VignetteParams2ID, data.vignetteParams2);
                    }
                    
                    // Baked Color Grading Lut
                    UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ColorGradingLutParamsID, data.colorGradingLutParams);
                    
                    // Extra Lut
                    CoreUtils.SetKeyword(UberPostProcessingMaterial, YPipelineKeywords.k_ExtraLut, data.isExtraLutEnabled);
                    if (data.isExtraLutEnabled)
                    {
                        UberPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_ExtraLutID, data.extraLut);
                        UberPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_ExtraLutParamsID, data.extraLutParams);
                    }
                    
                    // Blit
                    // TODO: 修改
                    BlitUtility.BlitTexture(context.cmd, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_FinalTextureID, UberPostProcessingMaterial, 0);
                });
            }
        }
    }
}