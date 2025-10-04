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
            base.Render(ref data);
        }
    }
}