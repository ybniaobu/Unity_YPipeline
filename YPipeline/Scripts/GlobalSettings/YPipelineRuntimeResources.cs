using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineRuntimeResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;
        
        // ----------------------------------------------------------------------------------------------------
        // Textures
        // ----------------------------------------------------------------------------------------------------

        #region Textures
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/EnvBRDFLut.exr")]
        private Texture2D m_EnvironmentBRDFLut;
        public Texture2D EnvironmentBRDFLut
        {
            get => m_EnvironmentBRDFLut;
            set => this.SetValueAndNotify(ref m_EnvironmentBRDFLut, value, nameof(m_EnvironmentBRDFLut));
        }
        
        [SerializeField] [ResourcePaths(new[]
        {
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainThin01.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainThin02.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium01.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium02.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium03.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium04.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium05.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainMedium06.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainLarge01.png",
            "YPipeline/PipelineResources/Textures/FilmGrain/FilmGrainLarge02.png"
        })]
        private Texture2D[] m_FilmGrainTex;
        public Texture2D[] FilmGrainTex
        {
            get => m_FilmGrainTex;
            set => this.SetValueAndNotify(ref m_FilmGrainTex, value, nameof(m_FilmGrainTex));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/BlueNoise/BlueNoise16.png")]
        private Texture2D m_BlueNoise16;
        public Texture2D BlueNoise16
        {
            get => m_BlueNoise16;
            set => this.SetValueAndNotify(ref m_BlueNoise16, value, nameof(m_BlueNoise16));
        }
        
        
        [SerializeField]
        private Texture2D m_BlueNoise32;
        public Texture2D BlueNoise32
        {
            get => m_BlueNoise32;
            set => this.SetValueAndNotify(ref m_BlueNoise32, value, nameof(m_BlueNoise32));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/BlueNoise/BlueNoise64.png")]
        private Texture2D m_BlueNoise64;
        public Texture2D BlueNoise64
        {
            get => m_BlueNoise64;
            set => this.SetValueAndNotify(ref m_BlueNoise64, value, nameof(m_BlueNoise64));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/STBN/STBN128_scalar3.png")]
        private Texture2D m_STBN128Scale3;
        public Texture2D STBN128Scale3
        {
            get => m_STBN128Scale3;
            set => this.SetValueAndNotify(ref m_STBN128Scale3, value, nameof(m_STBN128Scale3));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/STBN/STBN128_unitvec3.png")]
        private Texture2D m_STBN128UnitVec3;
        public Texture2D STBN128UnitVec3
        {
            get => m_STBN128UnitVec3;
            set => this.SetValueAndNotify(ref m_STBN128UnitVec3, value, nameof(m_STBN128UnitVec3));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/STBN/STBN128_unitvec3_cosine.png")]
        private Texture2D m_STBN128CosineUnitVec3;
        public Texture2D STBN128CosineUnitVec3
        {
            get => m_STBN128CosineUnitVec3;
            set => this.SetValueAndNotify(ref m_STBN128CosineUnitVec3, value, nameof(m_STBN128CosineUnitVec3));
        }
        
        [SerializeField] [ResourcePath("YPipeline/PipelineResources/Textures/STBN/STBN128_vec3.png")]
        private Texture2D m_STBN128Vec3;
        public Texture2D STBN128Vec3
        {
            get => m_STBN128Vec3;
            set => this.SetValueAndNotify(ref m_STBN128Vec3, value, nameof(m_STBN128Vec3));
        }
        
        #endregion
        
        // ----------------------------------------------------------------------------------------------------
        // Shaders 
        // ----------------------------------------------------------------------------------------------------

        #region Shaders

        [SerializeField] [ResourcePath("YPipeline/Shaders/MaterialModels/StandardForward/StandardPBR.shader")]
        private Shader m_DefaultPBRShader;
        public Shader DefaultPBRShader
        {
            get => m_DefaultPBRShader;
            set => this.SetValueAndNotify(ref m_DefaultPBRShader, value, nameof(m_DefaultPBRShader));
        }

        #endregion
        
        // ----------------------------------------------------------------------------------------------------
        // Compute Shaders
        // ----------------------------------------------------------------------------------------------------
        
        #region Compute Shaders
        
        [SerializeField] [ResourcePath("YPipeline/Shaders/PipelineShader/LightCulling/TiledLightCulling.compute")]
        private ComputeShader m_TiledLightCullingCS;
        public ComputeShader TiledLightCullingCS
        {
            get => m_TiledLightCullingCS;
            set => this.SetValueAndNotify(ref m_TiledLightCullingCS, value, nameof(m_TiledLightCullingCS));
        }
        
        [SerializeField] [ResourcePath("YPipeline/Shaders/PipelineShader/GlobalIllumination/AmbientOcclusion.compute")]
        private ComputeShader m_AmbientOcclusionCS;
        public ComputeShader AmbientOcclusionCS
        {
            get => m_AmbientOcclusionCS;
            set => this.SetValueAndNotify(ref m_AmbientOcclusionCS, value, nameof(m_AmbientOcclusionCS));
        }
        
        #endregion
    }
}