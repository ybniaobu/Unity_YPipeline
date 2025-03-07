using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class PostProcessingRenderer
    {
        public static T1 Create<T1, T2>() 
            where T1 : PostProcessingRenderer<T2>, new() 
            where T2 : VolumeComponent
        {
            T1 renderer = new T1();
            renderer.Initialize();
            return renderer;
        }
        
        protected static Material CreateMaterial(Shader shader, Material material)
        {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }
    
    public abstract class PostProcessingRenderer<T> : PostProcessingRenderer where T : VolumeComponent
    {
        public T settings;

        public virtual void Initialize()
        {
            settings = VolumeManager.instance.stack.GetComponent<T>();
        }

        public abstract void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data);
    }
}