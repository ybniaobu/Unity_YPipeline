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

            public Vector2 jitter;

            public void SetNonBuiltInCameraMatrixShaderVariables(CommandBuffer cmd)
            {
                bool isProjectionMatrixFlipped = SystemInfo.graphicsUVStartsAtTop;

                Matrix4x4 viewMatrix = yCamera.perCameraData.viewMatrix;
                Matrix4x4 inverseViewMatrix = viewMatrix.inverse;
                Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.jitteredProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 inverseProjectionMatrix = gpuProjectionMatrix.inverse;
                Matrix4x4 gpuNonJitterProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.projectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 nonJitterInverseProjectionMatrix = gpuNonJitterProjectionMatrix.inverse;
                
                Matrix4x4 inverseViewProjectionMatrix = inverseViewMatrix * inverseProjectionMatrix;
                Matrix4x4 nonJitterViewProjectionMatrix = gpuNonJitterProjectionMatrix * viewMatrix;
                Matrix4x4 nonJitterInverseViewProjectionMatrix = inverseViewMatrix * nonJitterInverseProjectionMatrix;
                
                Matrix4x4 previousViewMatrix = yCamera.perCameraData.previousViewMatrix;
                Matrix4x4 previousInverseViewMatrix = previousViewMatrix.inverse;
                Matrix4x4 previousGPUProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.previousJitteredProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 previousInverseProjectionMatrix = previousGPUProjectionMatrix.inverse;
                Matrix4x4 previousGPUNonJitterProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.previousProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 previousNonJitterInverseProjectionMatrix = previousGPUNonJitterProjectionMatrix.inverse;
                
                Matrix4x4 previousViewProjectionMatrix = previousGPUProjectionMatrix * previousViewMatrix;
                Matrix4x4 previousInverseViewProjectionMatrix = previousInverseViewMatrix * previousInverseProjectionMatrix;
                Matrix4x4 previousNonJitterViewProjectionMatrix = previousGPUNonJitterProjectionMatrix * previousViewMatrix;
                Matrix4x4 previousNonJitterInverseViewProjectionMatrix = previousInverseViewMatrix * previousNonJitterInverseProjectionMatrix;
                
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_InverseProjectionMatrixID, inverseProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_InverseViewProjectionMatrixID, inverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredViewProjectionMatrixID, nonJitterViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredInverseViewProjectionMatrixID, nonJitterInverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_PreviousViewProjectionMatrixID, previousViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_PreviousInverseViewProjectionMatrixID, previousInverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredPreviousViewProjectionMatrixID, previousNonJitterViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredPreviousInverseViewProjectionMatrixID, previousNonJitterInverseViewProjectionMatrix);
            }
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
                    int frameIndex = Time.frameCount;
                    Vector2 jitter = RandomUtility.k_Halton[frameIndex % 16 + 1] - new Vector2(0.5f, 0.5f);
                    jitter *= 2.0f * m_TAA.jitterScale.value;
                    jitteredProjectionMatrix = CameraUtility.GetJitteredProjectionMatrix(data.BufferSize, projectionMatrix, jitter, isOrthographic);
                    passData.jitter = jitter;
                }
                else
                {
                    jitteredProjectionMatrix = projectionMatrix;
                    passData.jitter = new Vector2(0, 0);
                }
                
                yCamera.perCameraData.SetPerCameraDataMatrices(viewMatrix, projectionMatrix, jitteredProjectionMatrix);

                builder.SetRenderFunc((CameraSetupPassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetupCameraProperties(data.camera);
                    context.cmd.SetViewProjectionMatrices(data.yCamera.perCameraData.viewMatrix, data.yCamera.perCameraData.jitteredProjectionMatrix);
                    data.SetNonBuiltInCameraMatrixShaderVariables(context.cmd);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_JitterID, data.jitter * 0.5f);
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
    }
}