using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DownSamplePass : PipelinePass
    {
        private class DownSamplePassData
        {
            public ComputeShader cs;
            public bool temporalDenoiseEnabled;
            
            public Vector2Int threadGroupSizes;
            public Vector4 textureSize;
            
            public TextureHandle halfDepth;
            public TextureHandle halfNormalRoughness;
            public TextureHandle halfMotionVector;
            public TextureHandle halfSceneHistory;
        }
        
        private ScreenSpaceGlobalIllumination m_SSGI;
        private AmbientOcclusion m_SSAO;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_SSGI = stack.GetComponent<ScreenSpaceGlobalIllumination>();
            m_SSAO = stack.GetComponent<AmbientOcclusion>();
        }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddComputePass<DownSamplePassData>("Downsample", out var passData))
            {
                passData.cs = data.runtimeResources.DownSampleCS;
                passData.temporalDenoiseEnabled = m_SSGI.enableTemporalDenoise.value || m_SSAO.enableTemporalFilter.value;
                Vector2Int textureSize = data.BufferSize / 2;
                passData.threadGroupSizes = new Vector2Int(textureSize.x / 8, textureSize.y / 8);
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                
                // Half Depth Texture
                TextureDesc halfDepthDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R32_SFloat,
                    filterMode = FilterMode.Point,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Half Depth Texture"
                };

                data.HalfDepthTexture = data.renderGraph.CreateTexture(halfDepthDesc);
                passData.halfDepth = data.HalfDepthTexture;
                builder.UseTexture(data.HalfDepthTexture, AccessFlags.Write);
                
                // Half Normal Rougness Texture
                TextureDesc halfNormalRoughnessDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Point,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Half Normal Roughness Texture"
                };
                
                data.HalfNormalRoughnessTexture = data.renderGraph.CreateTexture(halfNormalRoughnessDesc);
                passData.halfNormalRoughness = data.HalfNormalRoughnessTexture;
                builder.UseTexture(data.HalfNormalRoughnessTexture, AccessFlags.Write);
                
                // Half Motion Vector Texture
                TextureDesc halfMotionVectorDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Point,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Half Motion Vector Texture"
                };
                
                data.HalfMotionVectorTexture = data.renderGraph.CreateTexture(halfMotionVectorDesc);
                passData.halfMotionVector = data.HalfMotionVectorTexture;
                builder.UseTexture(data.HalfMotionVectorTexture, AccessFlags.Write);
                
                // Input Render Textures
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                builder.UseTexture(data.HalfMotionVectorTexture, AccessFlags.Read);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc((DownSamplePassData data, ComputeGraphContext context) =>
                {
                    LocalKeyword motionVectorKeyword = new LocalKeyword(data.cs, "_OUTPUT_MOTION_VECTOR");
                    LocalKeyword sceneHistoryKeyword = new LocalKeyword(data.cs, "_OUTPUT_SCENE_HISTORY");
                    
                    context.cmd.SetKeyword(data.cs, motionVectorKeyword, data.temporalDenoiseEnabled);
                    int downsampleKernel = data.cs.FindKernel("DownsampleKernel");
                    
                    context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfDepthTextureID, data.halfDepth);
                    context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughness);
                    if (data.temporalDenoiseEnabled) context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfMotionVectorTextureID, data.halfMotionVector);
                    
                    context.cmd.DispatchCompute(data.cs, downsampleKernel, data.threadGroupSizes.x, data.threadGroupSizes.y, 1);
                });
            }
        }
    }
}