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
            public bool isFirstFrame;
            
            public TextureHandle colorAttachment;
            public TextureHandle motionVectorTexture;
            public TextureHandle taaTarget;
            public TextureHandle taaHistory;
            
            public Vector4 taaParams; 
        }
        
        private bool isFirstFrame;
        
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

        protected override void Initialize()
        {
            isFirstFrame = true;
        }

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
                    passData.motionVectorTexture = builder.ReadTexture(data.MotionVectorTexture);
                    passData.material = TAAMaterial;
                    passData.taaParams = new Vector4(m_TAA.historyBlendFactor.value, 0f);
                    passData.isFirstFrame = isFirstFrame;
                    if (isFirstFrame) isFirstFrame = false;
                    
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
                        
                        data.material.SetTexture(YPipelineShaderIDs.k_TAAHistoryID, data.taaHistory);
                        
                        if (data.isFirstFrame) BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget);
                        else BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget, data.material, 0);
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