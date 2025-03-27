using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class PostColorGradingRenderer : PostProcessingRenderer
    {
        private static readonly int k_PostColorGradingParamsId = Shader.PropertyToID("_PostColorGradingParams");
        
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
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Post Color Grading");
            
            int lutHeight = asset.bakedLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            data.buffer.SetGlobalVector(k_PostColorGradingParamsId, new Vector4(1.0f / lutWidth, 1.0f / lutHeight, lutHeight - 1.0f));
            
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_BloomTextureId, BuiltinRenderTextureType.CameraTarget, PostColorGradingMaterial, 0);
            
            data.buffer.EndSample("Post Color Grading");
        }
    }
}