using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class FinalPostProcessingRenderer : PostProcessingRenderer
    {
        private class FinalPostProcessingData
        {
            //TODO: 完善 Texture Handles
            //public TextureHandle finalTexture;
            //public TextureHandle cameraTarget;
            
            public bool isFXAAEnabled;
            public bool isFXAAQualityEnabled;
            
            public TextureHandle filmGrainTexture;
            public Vector4 filmGrainParams;
            public Vector4 filmGrainTexParams;
            
            public Rect cameraPixelRect;
        }
        
        private FilmGrain m_FilmGrain;
        private RTHandle m_FilmGrainTexture;
        private System.Random m_Random;
        
        private const string k_FinalPostProcessing = "Hidden/YPipeline/FinalPostProcessing";
        private Material m_FinalPostProcessingMaterial;
        
        public Material FinalPostProcessingMaterial
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
            data.cmd.BeginSample("Final Post Processing");
            
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            
            // FXAA
            CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAQuality, data.asset.antiAliasingMode == AntiAliasingMode.FXAA && data.asset.fxaaMode == FXAAMode.Quality);
            CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAConsole, data.asset.antiAliasingMode == AntiAliasingMode.FXAA && data.asset.fxaaMode == FXAAMode.Console);
            
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
            BlitUtility.BlitCameraTarget(data.cmd, YPipelineShaderIDs.k_FinalTextureID, data.camera.pixelRect, FinalPostProcessingMaterial, 0);
            
            data.cmd.EndSample("Final Post Processing");
        }
        
        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<FinalPostProcessingData>("Final Post Processing", out var nodeData))
            {
                nodeData.isFXAAEnabled = data.asset.antiAliasingMode == AntiAliasingMode.FXAA;
                nodeData.isFXAAQualityEnabled = data.asset.fxaaMode == FXAAMode.Quality;
                nodeData.cameraPixelRect = data.camera.pixelRect;
                
                if (m_FilmGrain.IsActive())
                {
                    if (m_FilmGrainTexture == null || m_FilmGrain.type.value != FilmGrainKinds.Custom && m_FilmGrainTexture.externalTexture != data.asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value])
                    {
                        m_FilmGrainTexture?.Release();
                        m_FilmGrainTexture = RTHandles.Alloc(data.asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value]);
                    }
                    else if (m_FilmGrainTexture == null || m_FilmGrain.type.value == FilmGrainKinds.Custom && m_FilmGrainTexture.externalTexture != m_FilmGrain.texture.value)
                    {
                        m_FilmGrainTexture?.Release();
                        m_FilmGrainTexture = RTHandles.Alloc(m_FilmGrain.texture.value);
                    }
                    
                    float uvScaleX = data.camera.pixelWidth / (float) m_FilmGrainTexture.externalTexture.width;
                    float uvScaleY = data.camera.pixelHeight / (float) m_FilmGrainTexture.externalTexture.height;
                    float offsetX = (float) m_Random.NextDouble();
                    float offsetY = (float) m_Random.NextDouble();
                    
                    TextureHandle filmGrainTexture = data.renderGraph.ImportTexture(m_FilmGrainTexture);
                    nodeData.filmGrainTexture = filmGrainTexture;
                    builder.ReadTexture(filmGrainTexture);
                    
                    nodeData.filmGrainParams = new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value);
                    nodeData.filmGrainTexParams = new Vector4(uvScaleX, uvScaleY, offsetX, offsetY);
                }
                
                builder.SetRenderFunc((FinalPostProcessingData data, RenderGraphContext context) =>
                {
                    CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAQuality, data.isFXAAEnabled && data.isFXAAQualityEnabled);
                    CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FXAAConsole, data.isFXAAEnabled && !data.isFXAAQualityEnabled);
                    
                    CoreUtils.SetKeyword(FinalPostProcessingMaterial, YPipelineKeywords.k_FilmGrain, m_FilmGrain.IsActive());

                    if (m_FilmGrain.IsActive())
                    {
                        FinalPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_FilmGrainParamsID, data.filmGrainParams);
                        FinalPostProcessingMaterial.SetVector(YPipelineShaderIDs.k_FilmGrainTexParamsID, data.filmGrainTexParams);
                        FinalPostProcessingMaterial.SetTexture(YPipelineShaderIDs.k_FilmGrainTexID, data.filmGrainTexture);
                    }
                    
                    BlitUtility.BlitCameraTarget(context.cmd, YPipelineShaderIDs.k_FinalTextureID, data.cameraPixelRect, FinalPostProcessingMaterial, 0);
                });
            }
        }
    }
}