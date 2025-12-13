using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CullingPass : PipelinePass
    {
        // private class CullingPassData
        // {
        //     
        // }
        
        protected override void Initialize() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // Setup Culling Parameters
            data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            cullingParameters.maximumVisibleLights = YPipelineLightsData.k_MaxPunctualLightCount + 1;
            // TODO：实现了 Reflection Probe 的 tile/cluster culling 就需要取消掉 
            // cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            
            // Emit scene view UI
#if UNITY_EDITOR
            if (data.camera.cameraType == CameraType.Reflection || data.camera.cameraType == CameraType.Preview)
                ScriptableRenderContext.EmitGeometryForCamera(data.camera);

            if (data.camera.cameraType == CameraType.SceneView) 
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(data.camera);
            }
#endif
            // APV
            
            
            // Cull
            data.cullingResults = data.context.Cull(ref cullingParameters);
            
            data.context.ExecuteCommandBuffer(data.cmd);
            data.context.Submit();
            data.cmd.Clear();
        }
    }
}