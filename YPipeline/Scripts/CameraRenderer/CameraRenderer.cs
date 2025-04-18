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
        /// 用于只需要设置一次的全局贴图或者变量，不在 render() 里每帧调用，只调用一次
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
    }
}