using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class PostColorGradingRenderer : PostProcessingRenderer
    {
        private static readonly int k_VignetteColorId = Shader.PropertyToID("_VignetteColor");
        private static readonly int k_VignetteParams1Id = Shader.PropertyToID("_VignetteParams1");
        private static readonly int k_VignetteParams2Id = Shader.PropertyToID("_VignetteParams2");
        
        private static readonly int k_PostColorGradingParamsId = Shader.PropertyToID("_PostColorGradingParams");
        
        private static readonly int k_ExtraLutId = Shader.PropertyToID("_ExtraLut");
        private static readonly int k_ExtraLutParamsID = Shader.PropertyToID("_ExtraLutParams");
        
        private Vignette m_Vignette;
        private LookupTable m_LookupTable;
        
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
        
        protected override void Initialize()
        {
            base.Initialize();
            var stack = VolumeManager.instance.stack;
            m_Vignette = stack.GetComponent<Vignette>();
            m_LookupTable = stack.GetComponent<LookupTable>();
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Post Color Grading");
            
            // Vignette
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
            if (m_LookupTable.IsActive())
            {
                PostColorGradingMaterial.SetTexture(k_ExtraLutId, m_LookupTable.texture.value);
                Vector4 extraLutParams = new Vector4(1.0f / m_LookupTable.texture.value.width, 1.0f / m_LookupTable.texture.value.height, m_LookupTable.texture.value.height - 1.0f, m_LookupTable.contribution.value);
                PostColorGradingMaterial.SetVector(k_ExtraLutParamsID, extraLutParams);
            }
            else
            {
                PostColorGradingMaterial.SetVector(k_ExtraLutParamsID, Vector4.zero);
            }
            
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_BloomTextureId, BuiltinRenderTextureType.CameraTarget, PostColorGradingMaterial, 0);
            
            data.buffer.EndSample("Post Color Grading");
        }
    }
}