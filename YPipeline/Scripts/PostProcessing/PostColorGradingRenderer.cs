using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class PostColorGradingRenderer : PostProcessingRenderer
    {
        private const string k_ChromaticAberration = "_CHROMATIC_ABERRATION";
        private static readonly int k_SpectralLutID = Shader.PropertyToID("_SpectralLut");
        private static readonly int k_ChromaticAberrationParamsID = Shader.PropertyToID("_ChromaticAberrationParams");
        
        private const string k_Vignette = "_VIGNETTE";
        private static readonly int k_VignetteColorId = Shader.PropertyToID("_VignetteColor");
        private static readonly int k_VignetteParams1Id = Shader.PropertyToID("_VignetteParams1");
        private static readonly int k_VignetteParams2Id = Shader.PropertyToID("_VignetteParams2");
        
        private static readonly int k_PostColorGradingParamsId = Shader.PropertyToID("_PostColorGradingParams");
        
        private const string k_ExtraLut = "_EXTRA_LUT";
        private static readonly int k_ExtraLutId = Shader.PropertyToID("_ExtraLut");
        private static readonly int k_ExtraLutParamsID = Shader.PropertyToID("_ExtraLutParams");
        
        private const string k_FilmGrain = "_FILM_GRAIN";
        private static readonly int k_FilmGrainTexID = Shader.PropertyToID("_FilmGrainTex");
        private static readonly int k_FilmGrainParamsID = Shader.PropertyToID("_FilmGrainParams");
        private static readonly int k_FilmGrainTexParamsID = Shader.PropertyToID("_FilmGrainTexParams");
        
        private ChromaticAberration m_ChromaticAberration;
        private Vignette m_Vignette;
        private LookupTable m_LookupTable;
        private FilmGrain m_FilmGrain;
        
        private System.Random m_Random;
        
        private const string k_PostColorGrading = "Hidden/YPipeline/PostColorGrading";
        private Material m_PostColorGradingMaterial;

        private Material PostColorGradingMaterial
        {
            get
            {
                if (m_PostColorGradingMaterial == null)
                {
                    m_PostColorGradingMaterial = new Material(Shader.Find(k_PostColorGrading));
                    m_PostColorGradingMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_PostColorGradingMaterial;
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
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            m_Random = new System.Random();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Post Color Grading");
            
            // Chromatic Aberration
            CoreUtils.SetKeyword(PostColorGradingMaterial, k_ChromaticAberration, m_ChromaticAberration.IsActive());
            PostColorGradingMaterial.SetTexture(k_SpectralLutID, InternalSpectralLut);
            PostColorGradingMaterial.SetVector(k_ChromaticAberrationParamsID, new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples.value));
            
            // Vignette
            CoreUtils.SetKeyword(PostColorGradingMaterial, k_Vignette, m_Vignette.IsActive());
            float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
            float aspectRatio = data.camera.aspect;
            Vector4 vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
            Vector4 vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? aspectRatio : 1f);
            PostColorGradingMaterial.SetColor(k_VignetteColorId, m_Vignette.color.value);
            PostColorGradingMaterial.SetVector(k_VignetteParams1Id, vignetteParams1);
            PostColorGradingMaterial.SetVector(k_VignetteParams2Id, vignetteParams2);
            
            // Color Grading Baked Lut
            int lutHeight = asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            PostColorGradingMaterial.SetVector(k_PostColorGradingParamsId, new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f));
            
            // Extra Lut
            CoreUtils.SetKeyword(PostColorGradingMaterial, k_ExtraLut, m_LookupTable.IsActive());
            if (m_LookupTable.IsActive())
            {
                PostColorGradingMaterial.SetTexture(k_ExtraLutId, m_LookupTable.texture.value);
                Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                PostColorGradingMaterial.SetVector(k_ExtraLutParamsID, extraLutParams);
            }
            
            // Film Grain
            CoreUtils.SetKeyword(PostColorGradingMaterial,k_FilmGrain, m_FilmGrain.IsActive());
            if (m_LookupTable.IsActive())
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
                
                PostColorGradingMaterial.SetVector(k_FilmGrainParamsID, new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
                PostColorGradingMaterial.SetVector(k_FilmGrainTexParamsID, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                PostColorGradingMaterial.SetTexture(k_FilmGrainTexID, texture);
            }
            
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_BloomTextureId, BuiltinRenderTextureType.CameraTarget, PostColorGradingMaterial, 0);
            
            data.buffer.EndSample("Post Color Grading");
        }
    }
}