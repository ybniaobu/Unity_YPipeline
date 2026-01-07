using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ScreenSpaceGlobalIlluminationPass : PipelinePass
    {
        private class SSGIPassData
        {
            public ComputeShader cs;
            public Vector2Int threadGroupSizes;
            
            public Vector4 textureSize;
            public Vector4 ssgiParams;
            
            public TextureHandle sceneHistory; // TAAHistory or SceneHistory
            public TextureHandle irradianceTexture;
        }

        private ScreenSpaceGlobalIllumination m_SSGI;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_SSGI = stack.GetComponent<ScreenSpaceGlobalIllumination>();
        }

        protected override void OnDispose()
        {
            m_SSGI = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            data.isSSDGIEnabled = m_SSGI.IsActive();
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ScreenSpaceIrradiance, data.isSSDGIEnabled);
            if (!data.isSSDGIEnabled || Time.frameCount == 0) return;

            using (var builder = data.renderGraph.AddUnsafePass<SSGIPassData>("Diffuse Global Illumination", out var passData))
            {
                passData.cs = data.runtimeResources.HBILCS;
                
                // Pass Data
                Vector2Int bufferSize = data.BufferSize;
                Vector2Int textureSize = bufferSize;
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                passData.threadGroupSizes = new Vector2Int(threadGroupSizeX, threadGroupSizeY);

                passData.ssgiParams = new Vector4(m_SSGI.hbilIntensity.value, m_SSGI.hbilRadius.value, m_SSGI.hbilDirectionCount.value, m_SSGI.hbilStepCount.value);
                
                
                // Irradiance Texture
                TextureDesc irradianceTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.black,
                    enableRandomWrite = true,
                    name = "Irradiance Texture"
                };
                
                data.IrradianceTexture = data.renderGraph.CreateTexture(irradianceTextureDesc);
                passData.irradianceTexture = data.IrradianceTexture;
                builder.UseTexture(data.IrradianceTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.IrradianceTexture, YPipelineShaderIDs.k_IrradianceTextureID);

                passData.sceneHistory = data.TAAHistory;
                builder.UseTexture(data.TAAHistory, AccessFlags.Read);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((SSGIPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_SSGIParamsID, data.ssgiParams);
                    
                    int hbgiKernel = data.cs.FindKernel("HBILAlternateKernel");
                    context.cmd.SetComputeTextureParam(data.cs, hbgiKernel, "_InputTexture", data.sceneHistory);
                    context.cmd.SetComputeTextureParam(data.cs, hbgiKernel, "_OutputTexture", data.irradianceTexture);
                    context.cmd.DispatchCompute(data.cs, hbgiKernel, data.threadGroupSizes.x, data.threadGroupSizes.y, 1);
                });
            }
            
        }
    }
}