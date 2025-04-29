using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class CameraRenderer
    {
        public List<PipelineNode> cameraPipelineNodes = new List<PipelineNode>();
        private bool m_IsBegan = false;
        
        public static T Create<T>(ref YPipelineData data) where T : CameraRenderer, new()
        {
            T node = new T();
            node.Initialize(ref data);
            return node;
        }

        protected virtual void Initialize(ref YPipelineData data)
        {
            
        }

        public virtual void Dispose()
        {
            
        }

        public virtual void Render(ref YPipelineData data)
        {
            
        }
        
        /// <summary>
        /// 待删除
        /// </summary>
        public void Begin(ref YPipelineData data)
        {
            if (!m_IsBegan)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                data.buffer = cmd;
                PipelineNode.Begin(cameraPipelineNodes, ref data);
                m_IsBegan = true;
                data.buffer = null;
                CommandBufferPool.Release(cmd);
            }
        }
        
        protected void PresetRenderPaths(RenderPath renderPath)
        {
            cameraPipelineNodes.Clear();
            switch (renderPath)
            {
                case RenderPath.Forward: 
                    cameraPipelineNodes.Add(PipelineNode.Create<ForwardLightingNode>());
                    cameraPipelineNodes.Add(PipelineNode.Create<ForwardGeometryNode>());
                    cameraPipelineNodes.Add(PipelineNode.Create<SkyboxNode>());
                    cameraPipelineNodes.Add(PipelineNode.Create<TransparencyNode>());
                    cameraPipelineNodes.Add(PipelineNode.Create<PostProcessingNode>());
                    break;
                case RenderPath.Deferred:
                    
                    break;
                case RenderPath.Custom:
                    
                    break;
            }
        }
        
        protected bool Setup(ref YPipelineData data)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.CameraSetup));
            
#if UNITY_EDITOR
            if (data.camera.cameraType == CameraType.SceneView) 
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(data.camera);
            }
#endif
            
            if (!data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
            {
                return false;
            }
            
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            data.cullingResults = data.context.Cull(ref cullingParameters);
            data.buffer = CommandBufferPool.Get();
            data.buffer.name =data.camera.name;
            return true;
        }

        protected void PrepareBuffers(ref YPipelineData data)
        {
            Vector2Int bufferSize = data.bufferSize;
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
            
            data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_ColorBufferID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_DepthBufferID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                RenderTextureFormat.Depth);
            data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_ColorTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, 
                data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_DepthTextureID, bufferSize.x, bufferSize.y, 32, FilterMode.Point, 
                RenderTextureFormat.Depth);
            data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_FinalTextureID, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear,
                data.asset.enableHDRFrameBufferFormat ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        }

        protected void ReleaseBuffers(ref YPipelineData data)
        {
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_ColorBufferID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_DepthBufferID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_ColorTextureID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_DepthTextureID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_FinalTextureID);
        }
    }
}