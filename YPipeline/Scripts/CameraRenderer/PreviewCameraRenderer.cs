namespace YPipeline
{
    public class PreviewCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            
        }

        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            // TODO：反射探针不能用 depth prepass 渲染，效果不好
        }
    }
}