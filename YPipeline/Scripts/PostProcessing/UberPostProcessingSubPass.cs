﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class UberPostProcessingSubPass : PostProcessingSubPass
    {
        private class UberPostPassData
        {
            public Material material;
            
            public TextureHandle colorAttachment;
            public TextureHandle bloomTexture;
            public TextureHandle colorGradingLut;
            public TextureHandle finalTexture;
            
            public bool isChromaticAberrationEnabled;
            public TextureHandle spectralLut;
            public Vector4 chromaticAberrationParams;
            
            public bool isBloomEnabled;
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
            
        }
        
        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();

            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<UberPostPassData>("Uber Post Processing", out var passData))
            {
                passData.material = UberPostProcessingMaterial;
                passData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                
                builder.AllowPassCulling(false);
                
                Vector2Int bufferSize = data.BufferSize;
                TextureDesc finalTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRColorBuffer ? DefaultFormat.HDR : DefaultFormat.LDR),
                    filterMode = FilterMode.Bilinear,
                    name = "Final Texture",
                };
                
                data.CameraFinalTexture = data.renderGraph.CreateTexture(finalTextureDesc);
                passData.finalTexture = builder.WriteTexture(data.CameraFinalTexture);
                
                // Chromatic Aberration
                passData.isChromaticAberrationEnabled = m_ChromaticAberration.IsActive();
                if (m_ChromaticAberration.IsActive())
                {
                    if (m_SpectralLut == null || m_SpectralLut.externalTexture != InternalSpectralLut)
                    {
                        m_SpectralLut?.Release();
                        m_SpectralLut = RTHandles.Alloc(InternalSpectralLut);
                    }
                    
                    passData.spectralLut = data.renderGraph.ImportTexture(m_SpectralLut);
                    builder.ReadTexture(passData.spectralLut);
                    
                    passData.chromaticAberrationParams = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples.value);
                }
                else
                {
                    m_SpectralLut?.Release();
                }
                
                // Bloom
                passData.isBloomEnabled = m_Bloom.IsActive();
                if (m_Bloom.IsActive())
                {
                    passData.bloomTexture = builder.ReadTexture(data.BloomTexture);
                    passData.isBloomBicubicUpsampling = m_Bloom.bicubicUpsampling.value;
                    
                    Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.intensity.value, 0.0f) : new Vector4(m_Bloom.finalIntensity.value, 1.0f);
                    float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                    float knee = threshold * m_Bloom.thresholdKnee.value;
                    passData.bloomParams = bloomParams;
                    passData.bloomThreshold = new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f));
                }
                
                // Vignette
                passData.isVignetteEnabled = m_Vignette.IsActive();
                if (m_Vignette.IsActive())
                {
                    float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                    float aspectRatio = data.camera.aspect;
                    Vector4 vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
                    Vector4 vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? aspectRatio : 1f);
                    passData.vignetteColor = m_Vignette.color.value;
                    passData.vignetteParams1 = vignetteParams1;
                    passData.vignetteParams2 = vignetteParams2;
                }
                
                // Baked Color Grading Lut
                int lutHeight = data.asset.bakedLUTResolution;
                int lutWidth = lutHeight * lutHeight;
                passData.colorGradingLutParams = new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f);
                passData.colorGradingLut = builder.ReadTexture(data.ColorGradingLutTexture);
                
                // Extra Lut
                passData.isExtraLutEnabled = m_LookupTable.IsActive();
                if (m_LookupTable.IsActive())
                {
                    if (m_ExtraLut == null || m_ExtraLut.externalTexture != m_LookupTable.texture.value)
                    {
                        m_ExtraLut?.Release();
                        m_ExtraLut = RTHandles.Alloc(m_LookupTable.texture.value);
                    }
                    
                    TextureHandle extraLut = data.renderGraph.ImportTexture(m_ExtraLut);
                    passData.extraLut = extraLut;
                    builder.ReadTexture(extraLut);
                    
                    Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                    passData.extraLutParams = extraLutParams;
                }
                else
                {
                    m_ExtraLut?.Release();
                }
                
                builder.SetRenderFunc((UberPostPassData data, RenderGraphContext context) =>
                {
                    // Chromatic Aberration
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_ChromaticAberration, data.isChromaticAberrationEnabled);
                    if (data.isChromaticAberrationEnabled)
                    {
                        data.material.SetTexture(YPipelineShaderIDs.k_SpectralLutID, data.spectralLut);
                        data.material.SetVector(YPipelineShaderIDs.k_ChromaticAberrationParamsID, data.chromaticAberrationParams);
                    }
                    
                    // Bloom
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_Bloom, data.isBloomEnabled);
                    if (data.isBloomEnabled)
                    {
                        data.material.SetTexture(YPipelineShaderIDs.k_BloomTextureID, data.bloomTexture);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_BloomBicubicUpsampling, data.isBloomBicubicUpsampling);
                        data.material.SetVector(YPipelineShaderIDs.k_BloomParamsID, data.bloomParams);
                        data.material.SetVector(YPipelineShaderIDs.k_BloomThresholdID, data.bloomThreshold);
                    }
                    
                    // Vignette
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_Vignette, data.isVignetteEnabled);
                    if (data.isVignetteEnabled)
                    {
                        data.material.SetColor(YPipelineShaderIDs.k_VignetteColorID, data.vignetteColor);
                        data.material.SetVector(YPipelineShaderIDs.k_VignetteParams1ID, data.vignetteParams1);
                        data.material.SetVector(YPipelineShaderIDs.k_VignetteParams2ID, data.vignetteParams2);
                    }
                    
                    // Baked Color Grading Lut
                    data.material.SetTexture(YPipelineShaderIDs.k_ColorGradingLutTextureID, data.colorGradingLut);
                    data.material.SetVector(YPipelineShaderIDs.k_ColorGradingLutParamsID, data.colorGradingLutParams);
                    
                    // Extra Lut
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_ExtraLut, data.isExtraLutEnabled);
                    if (data.isExtraLutEnabled)
                    {
                        data.material.SetTexture(YPipelineShaderIDs.k_ExtraLutID, data.extraLut);
                        data.material.SetVector(YPipelineShaderIDs.k_ExtraLutParamsID, data.extraLutParams);
                    }
                    
                    // Blit
                    BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.finalTexture, data.material, 0);
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}