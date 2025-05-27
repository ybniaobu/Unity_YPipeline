using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class YPipelineShaderTagIDs
    {
        public static ShaderTagId k_SRPDefaultShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        
        public static ShaderTagId k_DepthShaderTagId = new ShaderTagId("Depth");
        public static ShaderTagId k_DepthNormalShaderTagId = new ShaderTagId("DepthNormal");
        public static ShaderTagId k_ForwardLitShaderTagId = new ShaderTagId("YPipelineForward");
        public static ShaderTagId k_TransparencyShaderTagId = new ShaderTagId("YPipelineTransparency");
        
        public static ShaderTagId[] k_OpaqueShaderTagIds = new ShaderTagId[]
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("YPipelineForward"),
        };

        public static ShaderTagId[] k_TransparencyShaderTagIds = new ShaderTagId[]
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("YPipelineTransparency"),
        };
        
        public static ShaderTagId[] k_LegacyShaderTagIds = new ShaderTagId[]
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("ForwardAdd"),
            new ShaderTagId("Deferred"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };
    }
    
    public static class YPipelineShaderIDs
    {
        // ----------------------------------------------------------------------------------------------------
        // Render Target Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        // Forward
        public static readonly int k_ColorTextureID = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int k_DepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int k_FinalTextureID = Shader.PropertyToID("_CameraFinalTexture");
        
        // Deferred
        
        
        // ----------------------------------------------------------------------------------------------------
        // Common Resource Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_EnvBRDFLutID = Shader.PropertyToID("_EnvBRDFLut");
        public static readonly int k_BlueNoise64ID = Shader.PropertyToID("_BlueNoise64");
        
        // ----------------------------------------------------------------------------------------------------
        // Shadow Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_SunLightShadowMapID = Shader.PropertyToID("_SunLightShadowMap");
        public static readonly int k_SpotLightShadowMapID = Shader.PropertyToID("_SpotLightShadowMap");
        public static readonly int k_PointLightShadowMapID = Shader.PropertyToID("_PointLightShadowMap");
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        // Result Textures
        public static readonly int k_BloomTextureID = Shader.PropertyToID("_BloomTexture");
        public static readonly int k_ColorGradingLutTextureID = Shader.PropertyToID("_ColorGradingLutTexture");
        
        // Process Textures
        public static readonly int k_BloomLowerTextureID = Shader.PropertyToID("_BloomLowerTexture");
        public static readonly int k_BloomPrefilterTextureID = Shader.PropertyToID("_BloomPrefilterTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Lighting Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        // Light Global Params Per Setting
        public static readonly int k_CascadeSettingsID = Shader.PropertyToID("_CascadeSettings");
        public static readonly int k_ShadowMapSizesID = Shader.PropertyToID("_ShadowMapSizes");
        
        // Sun Light Params Per Frame
        public static readonly int k_SunLightColorID = Shader.PropertyToID("_SunLightColor");
        public static readonly int k_SunLightDirectionID = Shader.PropertyToID("_SunLightDirection");
        public static readonly int k_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        public static readonly int k_SunLightShadowMatricesID = Shader.PropertyToID("_SunLightShadowMatrices");
        public static readonly int k_SunLightShadowBiasID = Shader.PropertyToID("_SunLightShadowBias");
        public static readonly int k_SunLightPCFParamsID = Shader.PropertyToID("_SunLightPCFParams");
        public static readonly int k_SunLightShadowParamsID = Shader.PropertyToID("_SunLightShadowParams");
        public static readonly int k_SunLightDepthParamsID = Shader.PropertyToID("_SunLightDepthParams");
        
        // Spot and Point Light Params Per Frame
        public static readonly int k_PunctualLightCountID = Shader.PropertyToID("_PunctualLightCount");
        
        public static readonly int k_SpotLightColorsID = Shader.PropertyToID("_SpotLightColors");
        public static readonly int k_SpotLightPositionsID = Shader.PropertyToID("_SpotLightPositions");
        public static readonly int k_SpotLightDirectionsID = Shader.PropertyToID("_SpotLightDirections");
        public static readonly int k_SpotLightParamsID = Shader.PropertyToID("_SpotLightParams");
        public static readonly int k_SpotLightShadowMatricesID = Shader.PropertyToID("_SpotLightShadowMatrices");
        public static readonly int k_SpotLightShadowBiasID = Shader.PropertyToID("_SpotLightShadowBias");
        public static readonly int k_SpotLightPCFParamsID = Shader.PropertyToID("_SpotLightPCFParams");
        public static readonly int k_SpotLightShadowParamsID = Shader.PropertyToID("_SpotLightShadowParams");
        public static readonly int k_SpotLightDepthParamsID = Shader.PropertyToID("_SpotLightDepthParams");
        
        public static readonly int k_PointLightColorsID = Shader.PropertyToID("_PointLightColors");
        public static readonly int k_PointLightPositionsID = Shader.PropertyToID("_PointLightPositions");
        public static readonly int k_PointLightParamsID = Shader.PropertyToID("_PointLightParams");
        public static readonly int k_PointLightShadowMatricesID = Shader.PropertyToID("_PointLightShadowMatrices");
        public static readonly int k_PointLightShadowBiasID = Shader.PropertyToID("_PointLightShadowBias");
        public static readonly int k_PointLightPCFParamsID = Shader.PropertyToID("_PointLightPCFParams");
        public static readonly int k_PointLightShadowParamsID = Shader.PropertyToID("_PointLightShadowParams");
        public static readonly int k_PointLightDepthParamsID = Shader.PropertyToID("_PointLightDepthParams");
        
        // Params Per Shadow Caster
        public static readonly int k_ShadowPancakingID = Shader.PropertyToID("_ShadowPancaking");
        
        // ----------------------------------------------------------------------------------------------------
        // Common Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_BufferSizeID = Shader.PropertyToID("_CameraBufferSize");
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_BloomParamsID = Shader.PropertyToID("_BloomParams");
        public static readonly int k_BloomThresholdID = Shader.PropertyToID("_BloomThreshold");
        
        public static readonly int k_ColorGradingLUTParamsID = Shader.PropertyToID("_ColorGradingLUTParams");
        
        public static readonly int k_ColorAdjustmentsParamsID = Shader.PropertyToID("_ColorAdjustmentsParams");
        public static readonly int k_ColorFilterID = Shader.PropertyToID("_ColorFilter");
        public static readonly int k_WhiteBalanceID = Shader.PropertyToID("_WhiteBalance");
        
        public static readonly int k_CurveMasterID  = Shader.PropertyToID("_CurveMaster");
        public static readonly int k_CurveRedID = Shader.PropertyToID("_CurveRed");
        public static readonly int k_CurveGreenID = Shader.PropertyToID("_CurveGreen");
        public static readonly int k_CurveBlueID = Shader.PropertyToID("_CurveBlue");
        public static readonly int k_CurveHueVsHueID = Shader.PropertyToID("_CurveHueVsHue");
        public static readonly int k_CurveHueVsSatID = Shader.PropertyToID("_CurveHueVsSat");
        public static readonly int k_CurveLumVsSatID = Shader.PropertyToID("_CurveLumVsSat");
        public static readonly int k_CurveSatVsSatID = Shader.PropertyToID("_CurveSatVsSat");
        
        public static readonly int k_SMHShadowsID = Shader.PropertyToID("_SMHShadows");
        public static readonly int k_SMHMidtonesID = Shader.PropertyToID("_SMHMidtones");
        public static readonly int k_SMHHighlightsID = Shader.PropertyToID("_SMHHighlights");
        public static readonly int k_SMHRangeID = Shader.PropertyToID("_SMHRange");
        
        public static readonly int k_LGGLiftID = Shader.PropertyToID("_LGGLift");
        public static readonly int k_LGGGammaID = Shader.PropertyToID("_LGGGamma");
        public static readonly int k_LGGGainID = Shader.PropertyToID("_LGGGain");
        
        public static readonly int k_ToneMappingParamsID = Shader.PropertyToID("_ToneMappingParams");
        
        // ----------------------------------------------------------------------------------------------------
        // Uber Post Related Texture or Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_SpectralLutID = Shader.PropertyToID("_SpectralLut");
        public static readonly int k_ChromaticAberrationParamsID = Shader.PropertyToID("_ChromaticAberrationParams");
        
        public static readonly int k_VignetteColorID = Shader.PropertyToID("_VignetteColor");
        public static readonly int k_VignetteParams1ID = Shader.PropertyToID("_VignetteParams1");
        public static readonly int k_VignetteParams2ID = Shader.PropertyToID("_VignetteParams2");
        
        public static readonly int k_ColorGradingLutParamsID = Shader.PropertyToID("_ColorGradingLutParams");
        
        public static readonly int k_ExtraLutID = Shader.PropertyToID("_ExtraLut");
        public static readonly int k_ExtraLutParamsID = Shader.PropertyToID("_ExtraLutParams");
        
        // ----------------------------------------------------------------------------------------------------
        // Final Post Related Texture or Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_FilmGrainTexID = Shader.PropertyToID("_FilmGrainTex");
        public static readonly int k_FilmGrainParamsID = Shader.PropertyToID("_FilmGrainParams");
        public static readonly int k_FilmGrainTexParamsID = Shader.PropertyToID("_FilmGrainTexParams");
    }

    public static class YPipelineKeywords
    {
        // ----------------------------------------------------------------------------------------------------
        // Lighting Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        // public const string k_ShadowMaskDistance = "_SHADOW_MASK_DISTANCE";
        // public const string k_ShadowMaskNormal = "_SHADOW_MASK_NORMAL";
        
        public const string k_ShadowPCF = "_SHADOW_PCF";
        public const string k_ShadowPCSS = "_SHADOW_PCSS";
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        public const string k_BloomBicubicUpsampling = "_BLOOM_BICUBIC_UPSAMPLING";
        
        public const string k_Bloom = "_BLOOM";
        public const string k_ChromaticAberration = "_CHROMATIC_ABERRATION";
        public const string k_Vignette = "_VIGNETTE";
        public const string k_ExtraLut = "_EXTRA_LUT";
        
        public const string k_FXAAQuality = "_FXAA_QUALITY";
        public const string k_FXAAConsole = "_FXAA_CONSOLE";
        public const string k_FilmGrain = "_FILM_GRAIN";
    }
}