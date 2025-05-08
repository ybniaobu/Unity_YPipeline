using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class PostProcessingRenderer
    {
        public bool isActivated = true;
        
        public static T Create<T>() where T : PostProcessingRenderer, new()
        {
            T renderer = new T();
            renderer.Initialize();
            return renderer;
        }

        protected abstract void Initialize();

        public abstract void Render(ref YPipelineData data);

        public abstract void OnRecord(ref YPipelineData data);
    }
}