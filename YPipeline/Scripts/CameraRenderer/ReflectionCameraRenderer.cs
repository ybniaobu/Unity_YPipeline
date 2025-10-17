using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class ReflectionCameraRenderer : CameraRenderer
    {
        
        protected override void Initialize(ref YPipelineData data)
        {
            // SetRenderPaths(data.asset.renderPath);
        }
        
        public override void Render(ref YPipelineData data)
        {
            // TODO：反射探针不能用 depth prepass 渲染，效果不好 ！！！！！！！！！！！！！！
            
            base.Render(ref data);
        }
    }
}