using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class BloomRenderer : PostProcessingRenderer
    {
        private class BloomData
        {
            public Material material;

            public TextureHandle colorAttachment;
            public TextureHandle bloomTexture;
            public TextureHandle bloomPrefilteredTexture;
            public TextureHandle[] bloomPyramidUpTextures = new TextureHandle[k_MaxBloomPyramidLevels];
            public TextureHandle[] bloomPyramidDownTextures = new TextureHandle[k_MaxBloomPyramidLevels];
            
            public bool isBloomEnabled;
            public BloomMode bloomMode;
            public int iterationCount;
            public Vector4 bloomParams;
            public Vector4 bloomThreshold;
            public bool isBloomBicubicUpsampling;
        }
        
        private const int k_MaxBloomPyramidLevels = 12;
        
        private Bloom m_Bloom;
        
        private const string k_Bloom = "Hidden/YPipeline/Bloom";
        private Material m_BloomMaterial;

        private Material BloomMaterial
        {
            get
            {
                if (m_BloomMaterial == null)
                {
                    m_BloomMaterial = new Material(Shader.Find(k_Bloom));
                    m_BloomMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_BloomMaterial;
            }
        }

        protected override void Initialize()
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_Bloom = stack.GetComponent<Bloom>();

            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<BloomData>("Bloom", out var nodeData, ProfilingSampler.Get(YPipelineProfileIDs.Bloom)))
            {
                nodeData.material = BloomMaterial;
                nodeData.isBloomEnabled = m_Bloom.IsActive();
                
                if (m_Bloom.IsActive())
                {
                    // do bloom at half or quarter resolution
                    int width;
                    int height;
                    if (m_Bloom.ignoreRenderScale.value)
                    {
                        width = data.camera.pixelWidth >> ((int)m_Bloom.bloomDownscale.value + 1);
                        height = data.camera.pixelHeight >> ((int)m_Bloom.bloomDownscale.value + 1);
                    }
                    else
                    {
                        width = data.BufferSize.x >> ((int)m_Bloom.bloomDownscale.value + 1);
                        height = data.BufferSize.y >> ((int)m_Bloom.bloomDownscale.value + 1);
                    }

                    // Determine the iteration count
                    int minSize = Mathf.Min(width, height);
                    int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f) - 1);
                    iterationCount = Mathf.Clamp(iterationCount, 1, m_Bloom.maxIterations.value);
                    nodeData.iterationCount = iterationCount;

                    // Texture Recording
                    nodeData.colorAttachment = data.CameraColorAttachment;
                    builder.ReadTexture(nodeData.colorAttachment);
                    
                    DefaultFormat format = data.asset.enableHDRFrameBufferFormat ? DefaultFormat.HDR : DefaultFormat.LDR;
                    TextureDesc bloomTextureDesc = new TextureDesc(width, height)
                    {
                        colorFormat = SystemInfo.GetGraphicsFormat(format),
                        filterMode = FilterMode.Bilinear,
                        name = "Bloom Texture"
                    };
                    data.BloomTexture = data.renderGraph.CreateTexture(bloomTextureDesc);
                    nodeData.bloomTexture = data.BloomTexture;
                    builder.ReadWriteTexture(nodeData.bloomTexture);

                    TextureDesc bloomPrefilteredTextureDesc = new TextureDesc(width, height)
                    {
                        colorFormat = SystemInfo.GetGraphicsFormat(format),
                        filterMode = FilterMode.Bilinear,
                        name = "Bloom Prefiltered Texture"
                    };
                    nodeData.bloomPrefilteredTexture = data.renderGraph.CreateTexture(bloomPrefilteredTextureDesc);
                    builder.ReadWriteTexture(nodeData.bloomPrefilteredTexture);
                    
                    width >>= 1;
                    height >>= 1;
                    for (int i = 0; i < iterationCount; i++)
                    {
                        TextureDesc bloomPyramidDesc = new TextureDesc(width, height)
                        {
                            colorFormat = SystemInfo.GetGraphicsFormat(format),
                            filterMode = FilterMode.Bilinear,
                        };
                        bloomPyramidDesc.name = "Bloom Pyramid Up" + i;
                        nodeData.bloomPyramidUpTextures[i] = data.renderGraph.CreateTexture(bloomPyramidDesc);
                        builder.ReadWriteTexture(nodeData.bloomPyramidUpTextures[i]);
                        bloomPyramidDesc.name = "Bloom Pyramid Down" + i;
                        nodeData.bloomPyramidDownTextures[i] = data.renderGraph.CreateTexture(bloomPyramidDesc);
                        builder.ReadWriteTexture(nodeData.bloomPyramidDownTextures[i]);
                        width >>= 1;
                        height >>= 1;
                    }

                    // Data Recording
                    Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.additiveStrength.value, 0.0f) : new Vector4(m_Bloom.scatter.value, 0.0f);
                    nodeData.bloomParams = bloomParams;
                    float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                    float knee = threshold * m_Bloom.thresholdKnee.value;
                    nodeData.bloomThreshold = new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f));
                    nodeData.isBloomBicubicUpsampling = m_Bloom.bicubicUpsampling.value;
                    nodeData.bloomMode = m_Bloom.mode.value;
                }

                builder.SetRenderFunc((BloomData data, RenderGraphContext context) =>
                {
                    if (data.isBloomEnabled)
                    {
                        // Shader property and keyword setup
                        data.material.SetVector(YPipelineShaderIDs.k_BloomParamsID, data.bloomParams);
                        data.material.SetVector(YPipelineShaderIDs.k_BloomThresholdID, data.bloomThreshold);
                        CoreUtils.SetKeyword(BloomMaterial, YPipelineKeywords.k_BloomBicubicUpsampling, data.isBloomBicubicUpsampling);
                        
                        // Prefilter
                        BlitUtility.BlitTexture(context.cmd, nodeData.colorAttachment, nodeData.bloomPrefilteredTexture, data.material, 0);
                        
                        // Downsample - gaussian pyramid
                        TextureHandle source = nodeData.bloomPrefilteredTexture;
                        for (int i = 0; i < data.iterationCount; i++)
                        {
                            BlitUtility.BlitTexture(context.cmd, source, data.bloomPyramidUpTextures[i], BloomMaterial, 1);
                            BlitUtility.BlitTexture(context.cmd, data.bloomPyramidUpTextures[i], data.bloomPyramidDownTextures[i], BloomMaterial, 2);
                            source = data.bloomPyramidDownTextures[i];
                        }
                        
                        // Upsample - bilinear or bicubic
                        int upsamplePass = data.bloomMode == BloomMode.Additive ? 3 : 4;
                        TextureHandle lastDst = data.bloomPyramidDownTextures[data.iterationCount - 1];
                        for (int i = data.iterationCount - 2; i >= 0; i--)
                        {
                            context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BloomLowerTextureID, lastDst);
                            BlitUtility.BlitTexture(context.cmd, data.bloomPyramidDownTextures[i], data.bloomPyramidUpTextures[i], data.material, upsamplePass);
                            lastDst = data.bloomPyramidUpTextures[i];
                        }
                        
                        // 有问题，待修改
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BloomLowerTextureID, data.bloomPrefilteredTexture);
                        BlitUtility.BlitTexture(context.cmd, data.bloomPyramidDownTextures[0], data.bloomTexture, data.material, upsamplePass);
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BloomTextureID, data.bloomTexture);
                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    }
                });

            }
        }
    }
}