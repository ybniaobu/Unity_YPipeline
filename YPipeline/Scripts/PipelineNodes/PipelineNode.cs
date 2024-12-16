using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class PipelineNode : ScriptableObject
    {
        /// <summary>
        /// 创建 PipelineNode 实例并初始化
        /// </summary>
        /// <typeparam name="T">PipelineNode 子类</typeparam>
        /// <returns>PipelineNode 子类实例</returns>
        public static T Create<T>() where T : PipelineNode
        {
            T node = ScriptableObject.CreateInstance<T>();
            node.Initialize();
            return node;
        }
        
        protected abstract void Initialize();
        protected abstract void Dispose();
        
        /// <summary>
        /// 用于提前设置需要跨 PipelineNode 的数据，以便各个 PipelineNode 可以使用
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="data"></param>
        protected virtual void OnSetup(YRenderPipelineAsset asset, ref PipelinePerFrameData data) { }
        protected virtual void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data) { }

        public static void SetupPipelineNodes(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (asset.currentPipelineNodes.Count != 0)
            {
                for (int i = 0; i < asset.currentPipelineNodes.Count; i++)
                {
                    asset.currentPipelineNodes[i].OnSetup(asset, ref data);
                }
            }
        }

        public static void RenderPipelineNodes(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (asset.currentPipelineNodes.Count != 0)
            {
                for (int i = 0; i < asset.currentPipelineNodes.Count; i++)
                {
                    asset.currentPipelineNodes[i].OnRender(asset, ref data);
                }
            }
        }
    }
}