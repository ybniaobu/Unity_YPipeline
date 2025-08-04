using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class AmbientOcclusionPass : PipelinePass
    {
        private class AmbientOcclusionPassData
        {
            public ComputeShader cs;
            
            public TextureHandle ambientOcclusionTexture;
            
            public Vector2Int bufferSize;
            public Vector4 ambientOcclusionParams;
        }

        private AmbientOcclusion m_AO;
        
        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_AO = stack.GetComponent<AmbientOcclusion>();
            data.isAmbientOcclusionTextureCreated = m_AO.IsActive();
            
            if (data.isAmbientOcclusionTextureCreated)
            {
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<AmbientOcclusionPassData>("Ambient Occlusion", out var passData))
                {
                    passData.cs = data.asset.pipelineResources.computeShaders.ambientOcclusionCs;
                    builder.ReadTexture(data.ThinGBuffer);
                    builder.ReadTexture(data.CameraDepthTexture);

                    passData.ambientOcclusionParams = new Vector4(m_AO.sampleCount.value, m_AO.radius.value);

                    // Create Ambient Occlusion Texture
                    Vector2Int bufferSize = data.BufferSize;
                    passData.bufferSize = bufferSize;

                    TextureDesc ambientOcclusionTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = GraphicsFormat.R8_UNorm,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = true,
                        clearColor = Color.clear,
                        enableRandomWrite = true,
                        name = "Screen Ambient Occlusion Texture"
                    };

                    data.AmbientOcclusionTexture = data.renderGraph.CreateTexture(ambientOcclusionTextureDesc);
                    passData.ambientOcclusionTexture = builder.WriteTexture(data.AmbientOcclusionTexture);

                    builder.AllowPassCulling(false);
                    builder.AllowRendererListCulling(false);

                    builder.SetRenderFunc((AmbientOcclusionPassData data, RenderGraphContext context) =>
                    {
                        int kernel = data.cs.FindKernel("SSAOKernel");
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_AmbientOcclusionTextureID, data.ambientOcclusionTexture);
                        context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_AmbientOcclusionParamsID, data.ambientOcclusionParams);
                        context.cmd.DispatchCompute(data.cs, kernel, data.bufferSize.x / 32, data.bufferSize.y / 8, 1);

                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
        }
    }
}