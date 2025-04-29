using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class FinalPostProcessingRenderer : PostProcessingRenderer
    {
        private FilmGrain m_FilmGrain;
        
        private System.Random m_Random;
        
        private const string k_FinalPostProcessing = "Hidden/YPipeline/FinalPostProcessing";
        private Material m_FinalPostProcessingMaterial;
        
        private Material FinalPostProcessingMaterial
        {
            get
            {
                if (m_FinalPostProcessingMaterial == null)
                {
                    m_FinalPostProcessingMaterial = new Material(Shader.Find(k_FinalPostProcessing));
                    m_FinalPostProcessingMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_FinalPostProcessingMaterial;
            }
        }
        
        
        protected override void Initialize()
        {
            base.Initialize();
            m_Random = new System.Random();
        }

        public override void Render(ref YPipelineData data)
        {
            isActivated = true;
            data.buffer.BeginSample("Final Post Processing");
            
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            
            // FXAA
            CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAQuality, data.asset.antiAliasing == AntiAliasing.FXAA && data.asset.fxaaMode == FXAAMode.Quality);
            CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAConsole, data.asset.antiAliasing == AntiAliasing.FXAA && data.asset.fxaaMode == FXAAMode.Console);
            
            // Film Grain
            CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FilmGrain, m_FilmGrain.IsActive());
            if (m_FilmGrain.IsActive())
            {
                Texture texture = null;
                if (m_FilmGrain.type.value != FilmGrainKinds.Custom)
                {
                    texture = data.asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value];
                }
                else
                {
                    texture = m_FilmGrain.texture.value;
                }
                float uvScaleX = data.camera.pixelWidth / (float) texture.width;
                float uvScaleY = data.camera.pixelHeight / (float) texture.height;
                float offsetX = (float) m_Random.NextDouble();
                float offsetY = (float) m_Random.NextDouble();
                
                FinalPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_FilmGrainParamsID, new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
                FinalPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_FilmGrainTexParamsID, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                FinalPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_FilmGrainTexID, texture);
            }
            
            // Blit
            BlitUtility.BlitCameraTarget(data.buffer, YPipelineShaderIDs.k_FinalTextureID, data.camera.pixelRect, FinalPostProcessingMaterial, 0);
            
            data.buffer.EndSample("Final Post Processing");
        }
    }
}