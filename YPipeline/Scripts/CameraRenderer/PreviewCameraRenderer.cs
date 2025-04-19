namespace YPipeline
{
    public class PreviewCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            base.Initialize(ref data);
        }

        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
            
            if (!Setup(ref data)) return;
        }
    }
}