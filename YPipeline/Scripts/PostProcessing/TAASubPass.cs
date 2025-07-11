﻿using UnityEngine;
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
            public bool isFirstFrame;
            
            public TextureHandle colorAttachment;
            public TextureHandle motionVectorTexture;
            public TextureHandle taaTarget;
            public TextureHandle taaHistory;
            
            // Shader Variables
            public Vector4 taaParams; 
            
            // Shader Keywords Related
            public bool is3X3;
            public bool isYCoCg;
            public ColorRectifyMode rectifyMode;
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
            if (data.asset.antiAliasingMode == AntiAliasingMode.TAA)
            {
                var stack = VolumeManager.instance.stack;
                m_TAA = stack.GetComponent<TAA>();
                
                using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<TAAPassData>("TAA", out var passData))
                {
                    YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                    passData.colorAttachment = builder.UseColorBuffer(data.CameraColorAttachment, 0);
                    passData.motionVectorTexture = builder.ReadTexture(data.CameraMotionVectorTexture);
                    passData.material = TAAMaterial;
                    passData.isFirstFrame = yCamera.perCameraData.isFirstFrame;
                    
                    // Record shader variables & keywords
                    passData.taaParams = new Vector4(m_TAA.historyBlendFactor.value, 0f);

                    passData.is3X3 = m_TAA.neighborhood.value == TAANeighborhood._3X3;
                    passData.isYCoCg = m_TAA.colorSpace.value == TAAColorSpace.YCoCg;
                    passData.rectifyMode = m_TAA.colorRectifyMode.value;
                    
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
                        
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAASample3X3, data.is3X3);
                        CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAYCOCG, data.isYCoCg);

                        int pass;
                        switch (passData.rectifyMode)
                        {
                            case ColorRectifyMode.AABBClamp: pass = 0; break;
                            case ColorRectifyMode.AABBClipToCenter: pass = 1; break;
                            case ColorRectifyMode.AABBClipToFiltered: pass = 2; break;
                            case ColorRectifyMode.VarianceClip: pass = 3; break;
                            default: pass = 3; break;
                        }
                        
                        data.material.SetTexture(YPipelineShaderIDs.k_TAAHistoryID, data.taaHistory);
                        
                        if (data.isFirstFrame) BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget);
                        else BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget, data.material, pass);
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