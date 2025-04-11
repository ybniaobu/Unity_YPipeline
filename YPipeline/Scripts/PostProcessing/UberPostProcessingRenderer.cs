using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class UberPostProcessingRenderer : PostProcessingRenderer
    {
        private const string k_ChromaticAberration = "_CHROMATIC_ABERRATION";
        private static readonly int k_SpectralLutID = Shader.PropertyToID("_SpectralLut");
        private static readonly int k_ChromaticAberrationParamsID = Shader.PropertyToID("_ChromaticAberrationParams");
        
        private const string k_Bloom = "_BLOOM";
        private const string k_BloomBicubicUpsampling = "_BLOOM_BICUBIC_UPSAMPLING";
        private static readonly int k_BloomTexID = Shader.PropertyToID("_BloomTex");
        private static readonly int k_BloomParamsId = Shader.PropertyToID("_BloomParams");
        private static readonly int k_BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        
        private const string k_Vignette = "_VIGNETTE";
        private static readonly int k_VignetteColorId = Shader.PropertyToID("_VignetteColor");
        private static readonly int k_VignetteParams1Id = Shader.PropertyToID("_VignetteParams1");
        private static readonly int k_VignetteParams2Id = Shader.PropertyToID("_VignetteParams2");
        
        private static readonly int k_ColorGradingLutParamsId = Shader.PropertyToID("_ColorGradingLutParams");
        
        private const string k_ExtraLut = "_EXTRA_LUT";
        private static readonly int k_ExtraLutId = Shader.PropertyToID("_ExtraLut");
        private static readonly int k_ExtraLutParamsID = Shader.PropertyToID("_ExtraLutParams");
        
        private const string k_FilmGrain = "_FILM_GRAIN";
        private static readonly int k_FilmGrainTexID = Shader.PropertyToID("_FilmGrainTex");
        private static readonly int k_FilmGrainParamsID = Shader.PropertyToID("_FilmGrainParams");
        private static readonly int k_FilmGrainTexParamsID = Shader.PropertyToID("_FilmGrainTexParams");
        
        private ChromaticAberration m_ChromaticAberration;
        private Bloom m_Bloom;
        private Vignette m_Vignette;
        private LookupTable m_LookupTable;
        private FilmGrain m_FilmGrain;
        
        private System.Random m_Random;
        
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
            var stack = VolumeManager.instance.stack;
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            m_Random = new System.Random();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Uber Post Processing");
            
            // Chromatic Aberration
            CoreUtils.SetKeyword(UberPostProcessingMaterial, k_ChromaticAberration, m_ChromaticAberration.IsActive());
            UberPostProcessingMaterial.SetTexture(k_SpectralLutID, InternalSpectralLut);
            UberPostProcessingMaterial.SetVector(k_ChromaticAberrationParamsID, new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples.value));
            
            // Bloom
            CoreUtils.SetKeyword(UberPostProcessingMaterial, k_Bloom, m_Bloom.IsActive());
            CoreUtils.SetKeyword(UberPostProcessingMaterial, k_BloomBicubicUpsampling, m_Bloom.bicubicUpsampling.value);
            Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.intensity.value, 0.0f) : new Vector4(m_Bloom.finalIntensity.value, 1.0f);
            UberPostProcessingMaterial.SetVector(k_BloomParamsId, bloomParams);
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = threshold * m_Bloom.thresholdKnee.value;
            UberPostProcessingMaterial.SetVector(k_BloomThresholdId, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f)));
            data.buffer.SetGlobalTexture(k_BloomTexID, new RenderTargetIdentifier(RenderTargetIDs.k_BloomTextureId));
            
            // Vignette
            CoreUtils.SetKeyword(UberPostProcessingMaterial, k_Vignette, m_Vignette.IsActive());
            float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
            float aspectRatio = data.camera.aspect;
            Vector4 vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
            Vector4 vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? aspectRatio : 1f);
            UberPostProcessingMaterial.SetColor(k_VignetteColorId, m_Vignette.color.value);
            UberPostProcessingMaterial.SetVector(k_VignetteParams1Id, vignetteParams1);
            UberPostProcessingMaterial.SetVector(k_VignetteParams2Id, vignetteParams2);
            
            // Baked Color Grading Lut
            int lutHeight = asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            UberPostProcessingMaterial.SetVector(k_ColorGradingLutParamsId, new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f));
            
            // Extra Lut
            CoreUtils.SetKeyword(UberPostProcessingMaterial, k_ExtraLut, m_LookupTable.IsActive());
            if (m_LookupTable.IsActive())
            {
                UberPostProcessingMaterial.SetTexture(k_ExtraLutId, m_LookupTable.texture.value);
                Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                UberPostProcessingMaterial.SetVector(k_ExtraLutParamsID, extraLutParams);
            }
            
            // Film Grain
            CoreUtils.SetKeyword(UberPostProcessingMaterial,k_FilmGrain, m_FilmGrain.IsActive());
            if (m_FilmGrain.IsActive())
            {
                Texture texture = null;
                if (m_FilmGrain.type.value != FilmGrainKinds.Custom)
                {
                    texture = asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value];
                }
                else
                {
                    texture = m_FilmGrain.texture.value;
                }
                float uvScaleX = data.camera.pixelWidth / (float) texture.width;
                float uvScaleY = data.camera.pixelHeight / (float) texture.height;
                float offsetX = (float) m_Random.NextDouble();
                float offsetY = (float) m_Random.NextDouble();
                
                UberPostProcessingMaterial.SetVector(k_FilmGrainParamsID, new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
                UberPostProcessingMaterial.SetVector(k_FilmGrainTexParamsID, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                UberPostProcessingMaterial.SetTexture(k_FilmGrainTexID, texture);
            }
            
            // TODO: Final Pass
            BlitUtility.BlitCameraTarget(data.buffer, RenderTargetIDs.k_ColorBufferId, data.camera.pixelRect, UberPostProcessingMaterial, 0);
            
            data.buffer.EndSample("Uber Post Processing");
        }
    }
}