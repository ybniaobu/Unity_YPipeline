using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class TAASubPass : PostProcessingSubPass
    {
        private class TAAPassData
        {
            public Material material;
            public bool isTAAHistoryReset;
            public bool isFirstFrame;
            
            public TextureHandle colorAttachment;
            public TextureHandle motionVectorTexture;
            public TextureHandle taaTarget;
            public TextureHandle taaHistory;
            public TextureHandle motionVectorHistory;
            
            // Shader Variables
            public Vector4 taaParams;
            
            // Shader Keywords Related
            public bool is3X3;
            public bool isYCoCg;
            public bool isVarianceAABB;
            public ColorRectifyMode rectifyMode;
            public CurrentFilter currentFilter;
            public HistoryFilter historyFilter;
        }
        
        private TAA m_TAA;
        
        private const string k_TAA = "Hidden/YPipeline/TAA";
        private Material m_TAAMaterial;
        private Material TAAMaterial
        {
            get
            {
                if (m_TAAMaterial == null)
                {
                    m_TAAMaterial = new Material(Shader.Find(k_TAA));
                    m_TAAMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_TAAMaterial;
            }
        }

        protected override void Initialize() { }

        public override void OnRecord(ref YPipelineData data)
        {
            bool isTAAEnabled = data.asset.antiAliasingMode == AntiAliasingMode.TAA;
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_TAA, isTAAEnabled);
            YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
            
            if (!isTAAEnabled)
            {
                yCamera.perCameraData.ReleaseTAAHistory();
            }
            else
            {
                var stack = VolumeManager.instance.stack;
                m_TAA = stack.GetComponent<TAA>();
                
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TAAPassData>("TAA", out var passData))
                {
                    passData.material = TAAMaterial;
                    passData.isFirstFrame = Time.frameCount == 1;
                    
                    passData.colorAttachment = builder.ReadTexture(data.CameraColorAttachment);
                    passData.motionVectorTexture = builder.ReadTexture(data.MotionVectorTexture);
                    builder.ReadTexture(data.CameraDepthTexture);
                    
                    // Record shader variables & keywords
                    passData.taaParams = new Vector4(m_TAA.historyBlendFactor.value, m_TAA.varianceCriticalValue.value, 
                        m_TAA.fixedContrastThreshold.value, m_TAA.relativeContrastThreshold.value);

                    passData.is3X3 = m_TAA.neighborhood.value == TAANeighborhood._3X3;
                    passData.isYCoCg = m_TAA.colorSpace.value == TAAColorSpace.YCoCg;
                    passData.isVarianceAABB = m_TAA.AABB.value == AABBMode.Variance;
                    passData.rectifyMode = m_TAA.colorRectifyMode.value;
                    passData.currentFilter = m_TAA.currentFilter.value;
                    passData.historyFilter = m_TAA.historyFilter.value;
                    
                    // Import TAA history
                    Vector2Int bufferSize = data.BufferSize;
                
                    RenderTextureDescriptor taaHistoryDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y)
                    {
                        graphicsFormat = SystemInfo.GetGraphicsFormat(data.asset.enableHDRColorBuffer ? DefaultFormat.HDR : DefaultFormat.LDR),
                        volumeDepth = 1,
                        msaaSamples = 1,
                        mipCount = 0,
                        autoGenerateMips = false,
                    };
                    
                    RTHandle taaHistory = yCamera.perCameraData.GetTAAHistory(ref taaHistoryDesc);
                    passData.isTAAHistoryReset = yCamera.perCameraData.IsTAAHistoryReset;
                    yCamera.perCameraData.IsTAAHistoryReset = false;
                    data.TAAHistory = data.renderGraph.ImportTexture(taaHistory);
                    passData.taaHistory = builder.ReadWriteTexture(data.TAAHistory);

                    // Create TAA target
                    TextureDesc taaTargetDesc = new TextureDesc(taaHistoryDesc)
                    {
                        anisoLevel = 0,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        name = "TAA Target"
                    };
                    
                    data.TAATarget = data.renderGraph.CreateTexture(taaTargetDesc);
                    passData.taaTarget = builder.WriteTexture(data.TAATarget);
                    
                    builder.AllowPassCulling(false);
                
                    builder.SetRenderFunc((TAAPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.BeginSample("TAABlendHistory");
                        data.material.SetVector(YPipelineShaderIDs.k_TAAParamsID, data.taaParams);
                        // data.material.SetTexture(YPipelineShaderIDs.k_MotionVectorTextureID, data.isFirstFrame || data.isTAAHistoryReset ? context.defaultResources.blackTexture : data.motionVectorTexture);
                        
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAASample3X3, data.is3X3);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAYCOCG, data.isYCoCg);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAVariance, data.isVarianceAABB);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAACurrentFilter, data.currentFilter == CurrentFilter.Gaussian);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAHistoryFilter, data.historyFilter == HistoryFilter.CatmullRomBicubic);
                        
                        if (data.isFirstFrame || data.isTAAHistoryReset) BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaHistory);
                        data.material.SetTexture(YPipelineShaderIDs.k_TAAHistoryID, data.taaHistory);
                        
                        BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget, data.material, (int) data.rectifyMode);
                        context.cmd.EndSample("TAABlendHistory");
                        
                        context.cmd.BeginSample("TAACopyHistory");
                        bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                        if (copyTextureSupported) context.cmd.CopyTexture(data.taaTarget, data.taaHistory);
                        else BlitUtility.BlitTexture(context.cmd, data.taaTarget, data.taaHistory);
                        context.cmd.EndSample("TAACopyHistory");
                        
                        context.renderContext.ExecuteCommandBuffer(context.cmd);
                        context.cmd.Clear();
                    });
                }
            }
        }
    }
}