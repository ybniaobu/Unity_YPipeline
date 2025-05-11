using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class PipelineNode
    {
        /// <summary>
        /// 创建 PipelineNode 实例并初始化
        /// </summary>
        /// <typeparam name="T">PipelineNode 子类</typeparam>
        /// <returns>PipelineNode 子类实例</returns>
        public static T Create<T>() where T : PipelineNode, new()
        {
            T node = new T();
            node.Initialize();
            return node;
        }
        
        protected abstract void Initialize();
        
        protected virtual void OnDispose() { }
        
        /// <summary>
        /// Recording pipeline node to the render graph. 
        /// </summary>
        /// <param name="data">YPipelineData</param>
        public virtual void OnRecord(ref YPipelineData data) { }

        public static void Record(List<PipelineNode> cameraPipelineNodes, ref YPipelineData data)
        {
            int nodeCount = cameraPipelineNodes.Count;
            
            if (nodeCount != 0)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    cameraPipelineNodes[i].OnRecord(ref data);
                }
            }
        }

        public static void Dispose(List<PipelineNode> cameraPipelineNodes)
        {
            int nodeCount = cameraPipelineNodes.Count;
            
            if (nodeCount != 0)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    cameraPipelineNodes[i].OnDispose();
                }
            }
        }
    }
}