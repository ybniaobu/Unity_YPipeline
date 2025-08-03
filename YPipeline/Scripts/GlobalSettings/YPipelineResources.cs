using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Icon("Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/d_RenderPipelineResources icon.asset")]
    [CreateAssetMenu(menuName = "YPipeline/YPipelineResources")]
    public sealed class YPipelineResources : ScriptableObject
    {
        // [System.Serializable]
        // public struct Shaders
        // {
        //     public Shader copy;
        // }

        [System.Serializable]
        public struct Textures
        {
            public Texture environmentBRDFLut;
            public Texture[] filmGrainTex;
            public Texture blueNoise16;
            public Texture blueNoise64;
            public Texture blueNoise128;
            public Texture blueNoise256;
        }
        
        public Textures textures;

        [System.Serializable]
        public struct ComputeShaders
        {
            public ComputeShader tiledLightCullingCs;
            public ComputeShader ambientOcclusionCs;
            
        }
        
        public ComputeShaders computeShaders;


        [System.Serializable]
        public struct DefaultMaterials
        {
            public Material standardPBR;
        }
        
        public DefaultMaterials defaultMaterials;
    }
}