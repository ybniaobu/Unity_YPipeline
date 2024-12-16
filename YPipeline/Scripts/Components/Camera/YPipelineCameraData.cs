using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    /// <summary>
    /// 每帧渲染所需的 Camera 所有数据
    /// </summary>
    public class YPipelineCameraData
    {
        public Camera camera;

        public YPipelineCameraData(Camera camera)
        {
            this.camera = camera;
        }
    }
}