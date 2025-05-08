using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class BloomRenderer : PostProcessingRenderer
    {
        private const int k_MaxBloomPyramidLevels = 15;
        
        private int[] m_BloomPyramidUpIds;
        private int[] m_BloomPyramidDownIds;
        
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
            m_BloomPyramidUpIds = new int[k_MaxBloomPyramidLevels];
            m_BloomPyramidDownIds = new int[k_MaxBloomPyramidLevels];

            for (int i = 0; i < k_MaxBloomPyramidLevels; i++)
            {
                m_BloomPyramidUpIds[i] = Shader.PropertyToID("_BloomPyramidUp" + i);
                m_BloomPyramidDownIds[i] = Shader.PropertyToID("_BloomPyramidDown" + i);
            }
        }

        public override void Render(ref YPipelineData data)
        {
            isActivated = true;
            data.cmd.BeginSample("Bloom");
            
            var stack = VolumeManager.instance.stack;
            m_Bloom = stack.GetComponent<Bloom>();
            
            // do bloom at half or quarter resolution
            int width;
            int height;
            if (m_Bloom.ignoreRenderScale.value)
            {
                width = data.camera.pixelWidth >> (int) m_Bloom.bloomDownscale.value;
                height = data.camera.pixelHeight >> (int) m_Bloom.bloomDownscale.value;
            }
            else
            {
                width = data.bufferSize.x >> (int) m_Bloom.bloomDownscale.value;
                height = data.bufferSize.y >> (int) m_Bloom.bloomDownscale.value;
            }
            
            // Temporary RT
            RenderTextureFormat format = data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_BloomTextureID, width >> 1, height >> 1, 0, FilterMode.Bilinear, format);
            
            // Determine the iteration count
            int minSize = Mathf.Min(width, height);
            int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f) - 1);
            iterationCount = Mathf.Clamp(iterationCount, 1, m_Bloom.maxIterations.value);
            
            // Shader property and keyword setup
            Vector4 bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.additiveStrength.value, 0.0f) : new Vector4(m_Bloom.scatter.value, 0.0f);
            BloomMaterial.SetVector(YPipelineShaderIDs.k_BloomParamsID, bloomParams);
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = threshold * m_Bloom.thresholdKnee.value;
            BloomMaterial.SetVector(YPipelineShaderIDs.k_BloomThresholdID, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f)));
            
            CoreUtils.SetKeyword(BloomMaterial, YPipelineKeywords.k_BloomBicubicUpsampling, m_Bloom.bicubicUpsampling.value);
            
            // Prefilter
            data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_BloomPrefilterTextureID, width, height, 0, FilterMode.Bilinear, format);
            BlitUtility.BlitTexture(data.cmd, YPipelineShaderIDs.k_ColorBufferID, YPipelineShaderIDs.k_BloomPrefilterTextureID, BloomMaterial, 0);
            width >>= 1;
            height >>= 1;
            
            // Downsample - gaussian pyramid
            int sourceId = YPipelineShaderIDs.k_BloomPrefilterTextureID;
            for (int i = 0; i < iterationCount; i++)
            {
                data.cmd.GetTemporaryRT(m_BloomPyramidUpIds[i], width, height, 0, FilterMode.Bilinear, format);
                data.cmd.GetTemporaryRT(m_BloomPyramidDownIds[i], width, height, 0, FilterMode.Bilinear, format);
                BlitUtility.BlitTexture(data.cmd, sourceId, m_BloomPyramidUpIds[i], BloomMaterial, 1);
                BlitUtility.BlitTexture(data.cmd, m_BloomPyramidUpIds[i], m_BloomPyramidDownIds[i], BloomMaterial, 2);
                sourceId = m_BloomPyramidDownIds[i];
                width >>= 1;
                height >>= 1;
            }
            
            // Upsample - bilinear or bicubic
            int upsamplePass = m_Bloom.mode.value == BloomMode.Additive ? 3 : 4;
            int lastDst = m_BloomPyramidDownIds[iterationCount - 1];
            for (int i = iterationCount - 2; i >= 0; i--)
            {
                data.cmd.SetGlobalTexture(YPipelineShaderIDs.k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
                if (i == 0) BlitUtility.BlitTexture(data.cmd, m_BloomPyramidDownIds[i], YPipelineShaderIDs.k_BloomTextureID, BloomMaterial, upsamplePass);
                else BlitUtility.BlitTexture(data.cmd, m_BloomPyramidDownIds[i], m_BloomPyramidUpIds[i], BloomMaterial, upsamplePass);
                lastDst = m_BloomPyramidUpIds[i];
            }
            
            // Final Blit
            // bloomParams = m_Bloom.mode.value == BloomMode.Additive ? new Vector4(m_Bloom.intensity.value, 0.0f) : new Vector4(m_Bloom.finalIntensity.value, 0.0f);
            // int finalPass = m_Bloom.mode.value == BloomMode.Additive ? 3 : 5;
            // data.buffer.SetGlobalVector(k_BloomParamsId, bloomParams);
            // data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
            // BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, RenderTargetIDs.k_BloomTextureId, BloomMaterial, finalPass);
            
            // Release RT
            data.cmd.ReleaseTemporaryRT(YPipelineShaderIDs.k_BloomPrefilterTextureID);
            for (int i = 0; i < iterationCount; i++)
            {
                data.cmd.ReleaseTemporaryRT(m_BloomPyramidUpIds[i]);
                data.cmd.ReleaseTemporaryRT(m_BloomPyramidDownIds[i]);
            }
                
            data.cmd.EndSample("Bloom");
        }

        public override void OnRecord(ref YPipelineData data)
        {
            
        }
    }
}