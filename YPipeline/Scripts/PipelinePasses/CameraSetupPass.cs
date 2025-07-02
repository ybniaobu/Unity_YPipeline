using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CameraSetupPass : PipelinePass
    {
        private class CameraSetupPassData
        {
            public Camera camera;
            public YPipelineCamera yCamera;
        }

        private TAA m_TAA;
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<CameraSetupPassData>("Setup Camera Properties", out var passData))
            {
                passData.camera = data.camera;
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                passData.yCamera = yCamera;
                
                var stack = VolumeManager.instance.stack;
                m_TAA = stack.GetComponent<TAA>();

                bool isOrthographic = data.camera.orthographic;
                Matrix4x4 viewMatrix = data.camera.worldToCameraMatrix;
                Matrix4x4 projectionMatrix = data.camera.projectionMatrix;
                Matrix4x4 jitteredProjectionMatrix;

                if (data.asset.antiAliasingMode == AntiAliasingMode.TAA)
                {
                    int taaFrameIndex = Time.frameCount;
                    Vector2 jitter = RandomUtility.k_Halton[taaFrameIndex % 16 + 1] - new Vector2(0.5f, 0.5f);
                    jitter *= 2.0f * m_TAA.jitterScale.value;
                    jitteredProjectionMatrix = CameraUtility.GetJitteredProjectionMatrix(data.BufferSize, projectionMatrix, jitter, isOrthographic);
                }
                else
                {
                    jitteredProjectionMatrix = CameraUtility.GetJitteredProjectionMatrix(data.BufferSize, projectionMatrix, new Vector2(0.0f, 0.0f), isOrthographic);
                }
                
                yCamera.perCameraData.SetPerCameraDataMatrices(viewMatrix, projectionMatrix, jitteredProjectionMatrix);

                builder.SetRenderFunc((CameraSetupPassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetupCameraProperties(data.camera);
                    context.cmd.SetViewProjectionMatrices(data.yCamera.perCameraData.viewMatrix, data.yCamera.perCameraData.jitteredProjectionMatrix);
                });
            }
        }
    }
}