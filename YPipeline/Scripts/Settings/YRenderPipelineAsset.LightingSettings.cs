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
        [FoldoutGroup("Lighting Settings", expanded: true)]
        public Texture2D environmentBRDFLut;
        
        
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
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [ValueDropdown("m_TextureSizes")] public int sunLightShadowArraySize = 2048;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(1, 4)] public int cascadeCount = 4;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")] [SerializeField]
        [Range(0f, 1f)] [Indent] private float spiltRatio1 = 0.25f, spiltRatio2 = 0.5f, spiltRatio3 = 0.75f;
        
        public Vector3 SpiltRatios => new Vector3(spiltRatio1, spiltRatio2, spiltRatio3);
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [MinValue(0)] public float maxShadowDistance = 60.0f;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(0f, 1f)] public float distanceFade = 0.1f;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(0f, 1f)] public float cascadeEdgeFade = 0.2f;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [ValueDropdown("m_SampleNumbers")] public int shadowSampleNumber = 16;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Sun Light Shadows")]
        [Range(0f, 10f)] public float penumbraWidth = 1.5f;
        

        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Point Light Shadows")]
        public int a = 0;
        
        [TabGroup("Shadows Settings/Direct Light Shadows/Tab", "Spot Light Shadows")]
        public int b = 0;
    }
}