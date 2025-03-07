using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum BloomDownscaleMode
    {
        Half,
        Quarter,
        HalfQuarter
    }
    
    [System.Serializable]
    public sealed class DownscaleParameter : VolumeParameter<BloomDownscaleMode>
    {
        public DownscaleParameter(BloomDownscaleMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("YPipeline Post Processing/Bloom")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class Bloom : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Strength of the bloom filter.")]
        public MinFloatParameter intensity = new MinFloatParameter(0.0f, 0.0f);
        
        [Tooltip("决定了像素开始泛光的亮度阈值 Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0.0f);
        
        [Tooltip("缓和亮度阈值参数的效果 Smooths cutoff effect of the configured threshold. Higher value makes more transition.")]
        public ClampedFloatParameter thresholdKnee = new ClampedFloatParameter(0.5f, 0.0f, 1f);
        
        [Tooltip("泛光模糊开始的分辨率(决定了模糊过程的最大分辨率) The starting resolution that this effect begins processing."), AdditionalProperty]
        public DownscaleParameter downscale = new DownscaleParameter(BloomDownscaleMode.Half);
        
        [Tooltip("最大迭代次数或泛光金字塔层数(决定了模糊过程的最小分辨率) The maximum number of iterations/Pyramid Levels.")]
        public ClampedIntParameter maxIterations = new ClampedIntParameter(6, 1, 15);
        
        [Tooltip("是否在向上采样阶段使用 Bicubic 插值以获取更平滑的效果（略微更费性能） Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter bicubicUpsampling = new BoolParameter(false);
        
        public bool IsActive() => intensity.value > 0f;
    }

    public class BloomRenderer : PostProcessingRenderer<Bloom>
    {
        private const int k_MaxBloomPyramidLevels = 15;
        private static readonly int k_BloomLowerTextureID = Shader.PropertyToID("_BloomLowerTexture");
        private static readonly int k_BloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        
        private static readonly int k_BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private static readonly int k_BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        private const string k_BloomBicubicUpsampling = "_BLOOM_BICUBIC_UPSAMPLING";
        
        private int[] m_BloomPyramidUpIds;
        private int[] m_BloomPyramidDownIds;
        
        private const string k_Bloom = "Hidden/YPipeline/Bloom";
        private Material m_BloomMaterial;
        
        private const string k_Copy = "Hidden/YPipeline/Copy";
        private Material m_CopyMaterial;

        public override void Initialize()
        {
            base.Initialize();
            m_BloomPyramidUpIds = new int[k_MaxBloomPyramidLevels];
            m_BloomPyramidDownIds = new int[k_MaxBloomPyramidLevels];

            for (int i = 0; i < k_MaxBloomPyramidLevels; i++)
            {
                m_BloomPyramidUpIds[i] = Shader.PropertyToID("_BloomPyramidUp" + i);
                m_BloomPyramidDownIds[i] = Shader.PropertyToID("_BloomPyramidDown" + i);
            }
            
            m_BloomMaterial = new Material(Shader.Find(k_Bloom));
            m_CopyMaterial = new Material(Shader.Find(k_Copy));
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (!settings.IsActive())
            {
                BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget, m_CopyMaterial, 0);
                return;
            }
            
            data.buffer.BeginSample("Bloom");
            
            // do bloom at half or quarter resolution
            int width = data.camera.pixelWidth >> (int) settings.downscale.value;
            int height = data.camera.pixelHeight >> (int) settings.downscale.value;
            
            // Determine the iteration count
            int minSize = Mathf.Min(width, height);
            int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f));
            iterationCount = Mathf.Clamp(iterationCount, 1, settings.maxIterations.value);
            
            // Shader property and keyword setup
            data.buffer.SetGlobalFloat(k_BloomIntensityId, settings.intensity.value);
            float threshold = Mathf.GammaToLinearSpace(settings.threshold.value);
            float knee = threshold * settings.thresholdKnee.value + 1e-6f;
            data.buffer.SetGlobalVector(k_BloomThresholdId, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / knee));
            
            CoreUtils.SetKeyword(m_BloomMaterial, k_BloomBicubicUpsampling, settings.bicubicUpsampling.value);
            
            // Prefilter
            data.buffer.GetTemporaryRT(k_BloomPrefilterId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, k_BloomPrefilterId, m_BloomMaterial, 3);
            width >>= 1;
            height >>= 1;
            
            // Downsample - gaussian pyramid
            int sourceId = k_BloomPrefilterId;
            for (int i = 0; i < iterationCount; i++)
            {
                data.buffer.GetTemporaryRT(m_BloomPyramidUpIds[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                data.buffer.GetTemporaryRT(m_BloomPyramidDownIds[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                BlitUtility.BlitTexture(data.buffer, sourceId, m_BloomPyramidUpIds[i], m_BloomMaterial, 0);
                BlitUtility.BlitTexture(data.buffer, m_BloomPyramidUpIds[i], m_BloomPyramidDownIds[i], m_BloomMaterial, 1);
                sourceId = m_BloomPyramidDownIds[i];
                width >>= 1;
                height >>= 1;
            }
            
            // Upsample - bilinear or bicubic
            int lastDst = m_BloomPyramidDownIds[iterationCount - 1];
            for (int i = iterationCount - 2; i >= 0; i--)
            {
                data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
                BlitUtility.BlitTexture(data.buffer, m_BloomPyramidDownIds[i], m_BloomPyramidUpIds[i], m_BloomMaterial, 2);
                lastDst = m_BloomPyramidUpIds[i];
            }
            
            data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
            BlitUtility.BlitTexture(data.buffer, RenderTargetIDs.k_FrameBufferId, BuiltinRenderTextureType.CameraTarget, m_BloomMaterial, 2);
            //BlitUtility.BlitTexture(data.buffer, sourceId, RenderTargetIDs.k_FrameBufferId, m_CopyMaterial, 0);
            
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