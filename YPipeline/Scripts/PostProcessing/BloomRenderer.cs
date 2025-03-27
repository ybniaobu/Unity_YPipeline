using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class BloomRenderer : PostProcessingRenderer
    {
        private const int k_MaxBloomPyramidLevels = 15;
        private static readonly int k_BloomLowerTextureID = Shader.PropertyToID("_BloomLowerTexture");
        private static readonly int k_BloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        
        private static readonly int k_BloomParamsId = Shader.PropertyToID("_BloomParams");
        private static readonly int k_BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        private const string k_BloomBicubicUpsampling = "_BLOOM_BICUBIC_UPSAMPLING";
        
        private int[] m_BloomPyramidUpIds;
        private int[] m_BloomPyramidDownIds;
        
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
            base.Initialize();
            
            m_BloomPyramidUpIds = new int[k_MaxBloomPyramidLevels];
            m_BloomPyramidDownIds = new int[k_MaxBloomPyramidLevels];

            for (int i = 0; i < k_MaxBloomPyramidLevels; i++)
            {
                m_BloomPyramidUpIds[i] = Shader.PropertyToID("_BloomPyramidUp" + i);
                m_BloomPyramidDownIds[i] = Shader.PropertyToID("_BloomPyramidDown" + i);
            }
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            var bloom = VolumeManager.instance.stack.GetComponent<Bloom>();
            
            if (!bloom.IsActive())
            {
                isActivated = false;
                return;
            }
            
            isActivated = true;
            data.buffer.BeginSample("Bloom");
            
            // do bloom at half or quarter resolution
            int width = data.camera.pixelWidth >> (int) bloom.bloomDownscale.value;
            int height = data.camera.pixelHeight >> (int) bloom.bloomDownscale.value;
            
            // Determine the iteration count
            int minSize = Mathf.Min(width, height);
            int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f) - 1);
            iterationCount = Mathf.Clamp(iterationCount, 1, bloom.maxIterations.value);
            
            // Shader property and keyword setup
            Vector4 bloomParams = bloom.mode.value == BloomMode.Additive ? new Vector4(bloom.additiveStrength.value, 0.0f) : new Vector4(bloom.scatter.value, 0.0f);
            data.buffer.SetGlobalVector(k_BloomParamsId, bloomParams);
            float threshold = Mathf.GammaToLinearSpace(bloom.threshold.value);
            float knee = threshold * bloom.thresholdKnee.value;
            data.buffer.SetGlobalVector(k_BloomThresholdId, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f)));
            
            CoreUtils.SetKeyword(BloomMaterial, k_BloomBicubicUpsampling, bloom.bicubicUpsampling.value);
            
            // HDR
            RenderTextureFormat format = asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            
            // Prefilter
            data.buffer.GetTemporaryRT(k_BloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, k_BloomPrefilterId, BloomMaterial, 0);
            width >>= 1;
            height >>= 1;
            
            // Downsample - gaussian pyramid
            int sourceId = k_BloomPrefilterId;
            for (int i = 0; i < iterationCount; i++)
            {
                data.buffer.GetTemporaryRT(m_BloomPyramidUpIds[i], width, height, 0, FilterMode.Bilinear, format);
                data.buffer.GetTemporaryRT(m_BloomPyramidDownIds[i], width, height, 0, FilterMode.Bilinear, format);
                BlitUtility.BlitTexture(data.buffer, sourceId, m_BloomPyramidUpIds[i], BloomMaterial, 1);
                BlitUtility.BlitTexture(data.buffer, m_BloomPyramidUpIds[i], m_BloomPyramidDownIds[i], BloomMaterial, 2);
                sourceId = m_BloomPyramidDownIds[i];
                width >>= 1;
                height >>= 1;
            }
            
            // Upsample - bilinear or bicubic
            int upsamplePass = bloom.mode.value == BloomMode.Additive ? 3 : 4;
            int lastDst = m_BloomPyramidDownIds[iterationCount - 1];
            for (int i = iterationCount - 2; i >= 0; i--)
            {
                data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
                BlitUtility.BlitTexture(data.buffer, m_BloomPyramidDownIds[i], m_BloomPyramidUpIds[i], BloomMaterial, upsamplePass);
                lastDst = m_BloomPyramidUpIds[i];
            }
            
            // Final Blit
            bloomParams = bloom.mode.value == BloomMode.Additive ? new Vector4(bloom.intensity.value, 0.0f) : new Vector4(bloom.finalIntensity.value, 0.0f);
            int finalPass = bloom.mode.value == BloomMode.Additive ? 3 : 5;
            data.buffer.SetGlobalVector(k_BloomParamsId, bloomParams);
            data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, RenderTargetIDs.k_BloomTextureId, BloomMaterial, finalPass);
            //BlitUtility.BlitTexture(data.buffer, sourceId, RenderTargetIDs.k_FrameBufferId, CopyMaterial, 0);
            
            // Release RT
            data.buffer.ReleaseTemporaryRT(k_BloomPrefilterId);
            for (int i = 0; i < iterationCount; i++)
            {
                data.buffer.ReleaseTemporaryRT(m_BloomPyramidUpIds[i]);
                data.buffer.ReleaseTemporaryRT(m_BloomPyramidDownIds[i]);
            }
                
            data.buffer.EndSample("Bloom");
        }
    }
}