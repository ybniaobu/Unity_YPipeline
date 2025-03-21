﻿using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum BloomMode
    {
        Additive,
        Scattering
    }
    
    public enum BloomDownscaleMode
    {
        Half,
        Quarter,
        HalfQuarter
    }
    
    [System.Serializable]
    public sealed class BloomModeParameter : VolumeParameter<BloomMode>
    {
        public BloomModeParameter(BloomMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class BloomDownscaleParameter : VolumeParameter<BloomDownscaleMode>
    {
        public BloomDownscaleParameter(BloomDownscaleMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("YPipeline Post Processing/Bloom")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class Bloom : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("泛光模式 Choose classical additive or energy-conserving scattering bloom.")]
        public BloomModeParameter mode = new BloomModeParameter(BloomMode.Scattering, true);
        
        [Tooltip("泛光强度 Strength of bloom during the final blit.")]
        public MinFloatParameter intensity = new MinFloatParameter(0.5f, 0.0f, true);
        
        [Tooltip("泛光强度 Strength of bloom during the final blit")]
        public ClampedFloatParameter finalIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f, true);
        
        [Tooltip("泛光上采样加强系数 Boosts the low-res source intensity during the upsampling stage.")]
        public ClampedFloatParameter additiveStrength = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);
        
        [Tooltip("泛光上采样散开插值系数 Interpolates between the high-res and low-res sources during upsampling stage. 1 means that only the low-res is used")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("决定了像素开始泛光的亮度阈值 Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(1.0f, 0.0f);
        
        [Tooltip("缓和亮度阈值参数的效果 Smooths cutoff effect of the configured threshold. Higher value makes more transition.")]
        public ClampedFloatParameter thresholdKnee = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("泛光模糊开始的分辨率(决定了模糊过程的最大分辨率) The starting resolution that this effect begins processing.")]
        public BloomDownscaleParameter bloomDownscale = new BloomDownscaleParameter(BloomDownscaleMode.Half);
        
        [Tooltip("最大迭代次数或泛光金字塔层数(决定了模糊过程的最小分辨率) The maximum number of iterations/Pyramid Levels.")]
        public ClampedIntParameter maxIterations = new ClampedIntParameter(6, 1, 15);
        
        [Tooltip("是否在向上采样阶段使用 Bicubic 插值以获取更平滑的效果（略微更费性能） Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter bicubicUpsampling = new BoolParameter(false);
        
        public bool IsActive()
        {
            if (mode.value == BloomMode.Additive) return intensity.value > 0f;
            else return finalIntensity.value > 0f;
        }
    }

    public class BloomRenderer : PostProcessingRenderer<Bloom>
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
        
        // private const string k_Copy = "Hidden/YPipeline/Copy";
        // private Material m_CopyMaterial;
        //
        // private Material CopyMaterial
        // {
        //     get
        //     {
        //         if (m_CopyMaterial == null)
        //         {
        //             m_CopyMaterial = new Material(Shader.Find(k_Copy));
        //             m_CopyMaterial.hideFlags = HideFlags.HideAndDontSave;
        //         }
        //         return m_CopyMaterial;
        //     }
        // }

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
        }

        public override void Render(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (!settings.IsActive())
            {
                return;
            }
            
            data.buffer.BeginSample("Bloom");
            
            // do bloom at half or quarter resolution
            int width = data.camera.pixelWidth >> (int) settings.bloomDownscale.value;
            int height = data.camera.pixelHeight >> (int) settings.bloomDownscale.value;
            
            // Determine the iteration count
            int minSize = Mathf.Min(width, height);
            int iterationCount = Mathf.FloorToInt(Mathf.Log(minSize, 2.0f) - 1);
            iterationCount = Mathf.Clamp(iterationCount, 1, settings.maxIterations.value);
            
            // Shader property and keyword setup
            Vector4 bloomParams = settings.mode.value == BloomMode.Additive ? new Vector4(settings.additiveStrength.value, 0.0f) : new Vector4(settings.scatter.value, 0.0f);
            data.buffer.SetGlobalVector(k_BloomParamsId, bloomParams);
            float threshold = Mathf.GammaToLinearSpace(settings.threshold.value);
            float knee = threshold * settings.thresholdKnee.value;
            data.buffer.SetGlobalVector(k_BloomThresholdId, new Vector4(threshold, knee - threshold, 2.0f * knee, 0.25f / (knee + 1e-6f)));
            
            CoreUtils.SetKeyword(BloomMaterial, k_BloomBicubicUpsampling, settings.bicubicUpsampling.value);
            
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
            int upsamplePass = settings.mode.value == BloomMode.Additive ? 3 : 4;
            int lastDst = m_BloomPyramidDownIds[iterationCount - 1];
            for (int i = iterationCount - 2; i >= 0; i--)
            {
                data.buffer.SetGlobalTexture(k_BloomLowerTextureID, new RenderTargetIdentifier(lastDst));
                BlitUtility.BlitTexture(data.buffer, m_BloomPyramidDownIds[i], m_BloomPyramidUpIds[i], BloomMaterial, upsamplePass);
                lastDst = m_BloomPyramidUpIds[i];
            }
            
            // Final Blit
            bloomParams = settings.mode.value == BloomMode.Additive ? new Vector4(settings.intensity.value, 0.0f) : new Vector4(settings.finalIntensity.value, 0.0f);
            int finalPass = settings.mode.value == BloomMode.Additive ? 3 : 5;
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