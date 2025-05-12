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
            public Texture[] blueNoise64;
        }
        
        public Textures textures;
    }
}