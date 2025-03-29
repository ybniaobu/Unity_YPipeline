using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace YPipeline
{
    public partial class YRenderPipelineAsset
    {
        // ----------------------------------------------------------------------------------------------------
        // 光照配置
        
        
        // ----------------------------------------------------------------------------------------------------
        // 阴影配置
        [FoldoutGroup("Shadows Settings", expanded: true)]
        [TitleGroup("Shadows Settings/Shadow Bias")]
        [Range(0f, 10f)] public float depthBias = 0.25f;
        
        [TitleGroup("Shadows Settings/Shadow Bias")]
        [Range(0f, 10f)] public float slopeScaledDepthBias = 0.5f;
        
        [TitleGroup("Shadows Settings/Shadow Bias")]
        [Range(0f, 10f)] public float normalBias = 0.25f;
        
        [TitleGroup("Shadows Settings/Shadow Bias")]
        [Range(0f, 10f)] public float slopeScaledNormalBias = 0.5f;
        
        [TitleGroup("Shadows Settings/Direct Light Shadows")]
        [MinValue(0)] public float maxShadowDistance = 80.0f;
        
        [TitleGroup("Shadows Settings/Direct Light Shadows")]
        [Range(0f, 1f)] public float distanceFade = 0.1f;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [ValueDropdown("m_TextureSizes")] public int sunLightShadowArraySize = 2048;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [ValueDropdown("m_SampleNumbers")] public int sunLightShadowSampleNumber = 16;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(0f, 1f)] public float sunLightPenumbraWidth = 0.25f;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(1, 4)] public int cascadeCount = 4;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")] [SerializeField]
        [Range(0f, 1f)] [Indent] private float spiltRatio1 = 0.15f, spiltRatio2 = 0.3f, spiltRatio3 = 0.6f;
        
        public Vector3 SpiltRatios => new Vector3(spiltRatio1, spiltRatio2, spiltRatio3);
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(0f, 1f)] public float cascadeEdgeFade = 0.05f;
        
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Punctual Light Shadows")]
        [ValueDropdown("m_TextureSizes")] public int punctualLightShadowArraySize = 512;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Punctual Light Shadows")]
        [ValueDropdown("m_SampleNumbers")] public int punctualLightShadowSampleNumber = 16;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Punctual Light Shadows")]
        [Range(0f, 0.5f)] public float punctualLightPenumbra = 0.05f;
    }
}