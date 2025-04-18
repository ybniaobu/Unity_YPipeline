using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    /// <summary>
    /// Unity Camera 类的扩展方法
    /// </summary>
    public static class CameraExtensions
    {
        public static YPipelineCamera GetYPipelineCamera(this Camera camera)
        {
            GameObject cameraObject = camera.gameObject;
            bool componentExists = cameraObject.TryGetComponent<YPipelineCamera>(out YPipelineCamera pipelineCamera);
            if(!componentExists) pipelineCamera = cameraObject.AddComponent<YPipelineCamera>();
            return pipelineCamera;
        }
    }
    
    /// <summary>
    /// YPipeline 增加的摄像机额外设置
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class YPipelineCamera : MonoBehaviour, ISerializationCallbackReceiver
    {
        
        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}