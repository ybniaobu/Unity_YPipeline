using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using YPipeline;

[CanEditMultipleObjects]
[CustomEditor(typeof(Light))]
[SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
public class YPipelineLightEditor : LightEditor
{
    public override void OnInspectorGUI() 
    {
        base.OnInspectorGUI();
        DrawInnerAndOuterSpotAngle();
    }

    private void DrawInnerAndOuterSpotAngle()
    {
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();
        }
    }
}
