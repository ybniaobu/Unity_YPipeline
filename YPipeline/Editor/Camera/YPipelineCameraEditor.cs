using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using YPipeline;

[CanEditMultipleObjects]
[CustomEditor(typeof(Camera))]
[SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
public class YPipelineCameraEditor : Editor
{
    public Camera camera => target as Camera;
    private YPipelineCamera m_YPipelineCamera;
    
    public void OnEnable()
    {
        m_YPipelineCamera = camera.GetYPipelineCamera();
    }
}
