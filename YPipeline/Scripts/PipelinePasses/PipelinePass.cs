using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public abstract class PipelinePass
    {
        /// <summary>
        /// 创建 PipelinePass 实例并初始化
        /// </summary>
        /// <typeparam name="T">PipelinePass 子类</typeparam>
        /// <returns>PipelinePass 子类实例</returns>
        public static T Create<T>() where T : PipelinePass, new()
        {
            T node = new T();
            node.Initialize();
            return node;
        }
        
        protected abstract void Initialize();
        
        protected virtual void OnDispose() { }
        
        /// <summary>
        /// Recording pipeline pass to the render graph. 
        /// </summary>
        /// <param name="data">YPipelineData</param>
        public virtual void OnRecord(ref YPipelineData data) { }

        public static void Record(List<PipelinePass> cameraPipelinePasses, ref YPipelineData data)
        {
            int passCount = cameraPipelinePasses.Count;
            
            if (passCount != 0)
            {
                for (int i = 0; i < passCount; i++)
                {
                    cameraPipelinePasses[i].OnRecord(ref data);
                }
            }
        }

        public static void Dispose(List<PipelinePass> cameraPipelinePasses)
        {
            int passCount = cameraPipelinePasses.Count;
            
            if (passCount != 0)
            {
                for (int i = 0; i < passCount; i++)
                {
                    cameraPipelinePasses[i].OnDispose();
                }
            }
        }
    }
}