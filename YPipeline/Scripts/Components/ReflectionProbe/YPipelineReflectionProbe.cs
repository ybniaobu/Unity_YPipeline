using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class ReflectionProbeExtensions
    {
        public static YPipelineReflectionProbe GetYPipelineReflectionProbe(this ReflectionProbe probe)
        {
            GameObject probeObject = probe.gameObject;
            bool componentExists = probeObject.TryGetComponent<YPipelineReflectionProbe>(out YPipelineReflectionProbe pipelineProbe);
            if(!componentExists) pipelineProbe = probeObject.AddComponent<YPipelineReflectionProbe>();
            return pipelineProbe;
        }
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReflectionProbe))]
    public class YPipelineReflectionProbe : MonoBehaviour, IAdditionalData
    {
        public Texture texture;
        public bool showOctahedralCubemap;
    }
}