﻿using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    /// <summary>
    /// 每帧渲染所需的灯光所有数据
    /// </summary>
    public class YPipelineLightData
    {
        public Light light;

        public YPipelineLightData(Light light)
        {
            this.light = light;
        }
    }
}