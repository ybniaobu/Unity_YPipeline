using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace YPipeline
{
    public static class LightExtensions
    {
        public static YPipelineLight GetYPipelineLight(this Light light)
        {
            GameObject lightObject = light.gameObject;
            bool componentExists = lightObject.TryGetComponent<YPipelineLight>(out YPipelineLight pipelineLight);
            if(!componentExists) pipelineLight = lightObject.AddComponent<YPipelineLight>();
            return pipelineLight;
        }
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class YPipelineLight : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Space(5f)]
        [Header("Shadow Bias Settings")]
        [Range(0f, 10f)] public float depthBias = 0.25f;
        [Range(0f, 10f)] public float slopeScaledDepthBias = 0.5f;
        [Range(0f, 10f)] public float normalBias = 0.25f;
        [Range(0f, 10f)] public float slopeScaledNormalBias = 0.5f;
        
        [Space(5f)]
        [Header("PCSS Settings")]
        [Range(0f, 2f)] public float lightSize = 0.1f;
        [Range(0f, 10f)] public float penumbraScale = 1.0f;

        [Range(1, 64)] public int blockerSearchSampleNumber = 8;
        [Range(1, 64)] public int filterSampleNumber = 8;
        
        
        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}