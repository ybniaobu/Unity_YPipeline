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
    [ExecuteAlways]
    public class YPipelineReflectionProbe : MonoBehaviour, IAdditionalData
    {
        public Texture octahedralMap;
        public bool showOctahedralMap;
        public bool isOctahedralMapBaked;
        public Vector4[] SH = new Vector4[7];
        public bool isSHBaked;
    }
}