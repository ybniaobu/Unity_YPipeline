using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class SceneCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            base.Initialize(ref data);
            PresetRenderPaths(data.asset.renderPath);
        }
        
        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            // UI Geometry
            ScriptableRenderContext.EmitWorldGeometryForSceneView(data.camera);
            
            if (!Setup(ref data)) return;
            
            // 好像在这个版本中，要自己调用 Update，否则无法获取到 VolumeComponent 序列化后的数据。可能以后的版本要删除这段代码。
            VolumeManager.instance.Update(data.camera.transform, 1);
            
            // TODO: 添加绘制错误材质的功能
            
            PrepareBuffers(ref data);
            PipelineNode.Render(cameraPipelineNodes, ref data);
            PipelineNode.Release(cameraPipelineNodes, ref data);
            ReleaseBuffers(ref data);
            CommandBufferPool.Release(data.cmd);
        }
    }
}