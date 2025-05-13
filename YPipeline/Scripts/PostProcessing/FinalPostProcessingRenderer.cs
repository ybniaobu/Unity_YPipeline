using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class FinalPostProcessingRenderer : PostProcessingRenderer
    {
        private class FinalPostProcessingData
        {
            public Material material;
            
            public TextureHandle finalTexture;
            public TextureHandle cameraColorTarget;
                
            public bool isFXAAEnabled;
            public bool isFXAAQualityEnabled;
            
            public bool isFilmGrainEnabled;
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
            m_Random = new System.Random();
        }
        
        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<FinalPostProcessingData>("Final Post Processing", out var nodeData))
            {
                nodeData.material = FinalPostProcessingMaterial;
                nodeData.finalTexture = builder.ReadTexture(data.CameraFinalTexture);
                nodeData.cameraColorTarget = builder.WriteTexture(data.CameraColorTarget);
                
                nodeData.isFXAAEnabled = data.asset.antiAliasingMode == AntiAliasingMode.FXAA;
                nodeData.isFXAAQualityEnabled = data.asset.fxaaMode == FXAAMode.Quality;
                nodeData.isFilmGrainEnabled = m_FilmGrain.IsActive();
                nodeData.cameraPixelRect = data.camera.pixelRect;
                
                builder.AllowPassCulling(false);
                
                if (m_FilmGrain.IsActive())
                {
                    if (m_FilmGrain.type.value != FilmGrainKinds.Custom)
                    {
                        if (m_FilmGrainTexture == null || m_FilmGrainTexture.externalTexture != data.asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value])
                        {
                            m_FilmGrainTexture?.Release();
                            m_FilmGrainTexture = RTHandles.Alloc(data.asset.pipelineResources.textures.filmGrainTex[(int)m_FilmGrain.type.value]);
                        }
                    }
                    else
                    {
                        if (m_FilmGrainTexture == null || m_FilmGrainTexture.externalTexture != m_FilmGrain.texture.value)
                        {
                            m_FilmGrainTexture?.Release();
                            m_FilmGrainTexture = RTHandles.Alloc(m_FilmGrain.texture.value);
                        }
                    }
                    
                    nodeData.filmGrainTexture = data.renderGraph.ImportTexture(m_FilmGrainTexture);
                    builder.ReadTexture(nodeData.filmGrainTexture);
                    
                    float uvScaleX = data.camera.pixelWidth / (float) m_FilmGrainTexture.externalTexture.width;
                    float uvScaleY = data.camera.pixelHeight / (float) m_FilmGrainTexture.externalTexture.height;
                    float offsetX = (float) m_Random.NextDouble();
                    float offsetY = (float) m_Random.NextDouble();
                    
                    nodeData.filmGrainParams = new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value);
                    nodeData.filmGrainTexParams = new Vector4(uvScaleX, uvScaleY, offsetX, offsetY);
                }
                else
                {
                    m_FilmGrainTexture?.Release();
                }
                
                builder.SetRenderFunc((FinalPostProcessingData data, RenderGraphContext context) =>
                {
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FXAAQuality, data.isFXAAEnabled && data.isFXAAQualityEnabled);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FXAAConsole, data.isFXAAEnabled && !data.isFXAAQualityEnabled);
                    
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FilmGrain, data.isFilmGrainEnabled);
                    if (data.isFilmGrainEnabled)
                    {
                        data.material.SetVector(YPipelineShaderIDs.k_FilmGrainParamsID, data.filmGrainParams);
                        data.material.SetVector(YPipelineShaderIDs.k_FilmGrainTexParamsID, data.filmGrainTexParams);
                        data.material.SetTexture(YPipelineShaderIDs.k_FilmGrainTexID, data.filmGrainTexture);
                    }
                    
                    BlitUtility.BlitTexture(context.cmd, data.finalTexture, data.cameraColorTarget, data.cameraPixelRect, data.material, 0);
                });
            }
        }
    }
}