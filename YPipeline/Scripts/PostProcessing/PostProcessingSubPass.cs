using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class PostProcessingSubPass
    {
        public bool isActivated = true;
        
        public static T Create<T>() where T : PostProcessingSubPass, new()
        {
            T subPass = new T();
            subPass.Initialize();
            return subPass;
        }

        protected abstract void Initialize();

        public abstract void OnDispose();

        public abstract void OnRecord(ref YPipelineData data);
    }
}