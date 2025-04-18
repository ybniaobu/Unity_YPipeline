using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class YPipelineShaderTagIDs
    {
        public static ShaderTagId k_SRPDefaultShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        
        public static ShaderTagId k_ForwardLitShaderTagId = new ShaderTagId("YPipelineForward");
        public static ShaderTagId k_DepthShaderTagId = new ShaderTagId("Depth");
        public static ShaderTagId k_TransparencyShaderTagId = new ShaderTagId("YPipelineTransparency");
    }
    
    public static class YPipelineShaderIDs
    {
        // ----------------------------------------------------------------------------------------------------
        // Render Target Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_ColorBufferId = Shader.PropertyToID("_CameraColorBuffer");
        public static readonly int k_DepthBufferId = Shader.PropertyToID("_CameraDepthBuffer");
        public static readonly int k_ColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int k_DepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Common Resource Textures IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_EnvBRDFLutID = Shader.PropertyToID("_EnvBRDFLut");
        
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
        public static readonly int k_BloomTextureId = Shader.PropertyToID("_BloomTexture");
        public static readonly int k_ColorGradingLutTextureId = Shader.PropertyToID("_ColorGradingLutTexture");
        
        // Process Textures
        public static readonly int k_BloomLowerTextureID = Shader.PropertyToID("_BloomLowerTexture");
        public static readonly int k_BloomPrefilterTextureId = Shader.PropertyToID("_BloomPrefilterTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Lighting Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        // Light Global Params Per Setting
        public static readonly int k_CascadeSettingsID = Shader.PropertyToID("_CascadeSettings");
        public static readonly int k_ShadowBiasID = Shader.PropertyToID("_ShadowBias");
        public static readonly int k_SunLightShadowSettingsID = Shader.PropertyToID("_SunLightShadowSettings");
        public static readonly int k_PunctualLightShadowSettingsID = Shader.PropertyToID("_PunctualLightShadowSettings");
        
        // Sun Light Params Per Frame
        public static readonly int k_SunLightColorId = Shader.PropertyToID("_SunLightColor");
        public static readonly int k_SunLightDirectionId = Shader.PropertyToID("_SunLightDirection");
        public static readonly int k_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        public static readonly int k_SunLightShadowMatricesID = Shader.PropertyToID("_SunLightShadowMatrices");
        public static readonly int k_SunLightShadowBiasID = Shader.PropertyToID("_SunLightShadowBias");
        public static readonly int k_SunLightShadowParamsID = Shader.PropertyToID("_SunLightShadowParams");
        public static readonly int k_SunLightDepthParamsID = Shader.PropertyToID("_SunLightDepthParams");
        
        // Spot and Point Light Params Per Frame
        public static readonly int k_PunctualLightCountId = Shader.PropertyToID("_PunctualLightCount");
        
        public static readonly int k_SpotLightColorsId = Shader.PropertyToID("_SpotLightColors");
        public static readonly int k_SpotLightPositionsId = Shader.PropertyToID("_SpotLightPositions");
        public static readonly int k_SpotLightDirectionsId = Shader.PropertyToID("_SpotLightDirections");
        public static readonly int k_SpotLightParamsId = Shader.PropertyToID("_SpotLightParams");
        public static readonly int k_SpotLightShadowMatricesID = Shader.PropertyToID("_SpotLightShadowMatrices");
        public static readonly int k_SpotLightShadowBiasID = Shader.PropertyToID("_SpotLightShadowBias");
        public static readonly int k_SpotLightShadowParamsID = Shader.PropertyToID("_SpotLightShadowParams");
        public static readonly int k_SpotLightDepthParamsID = Shader.PropertyToID("_SpotLightDepthParams");
        
        public static readonly int k_PointLightColorsId = Shader.PropertyToID("_PointLightColors");
        public static readonly int k_PointLightPositionsId = Shader.PropertyToID("_PointLightPositions");
        public static readonly int k_PointLightParamsId = Shader.PropertyToID("_PointLightParams");
        public static readonly int k_PointLightShadowMatricesID = Shader.PropertyToID("_PointLightShadowMatrices");
        public static readonly int k_PointLightShadowBiasID = Shader.PropertyToID("_PointLightShadowBias");
        public static readonly int k_PointLightShadowParamsID = Shader.PropertyToID("_PointLightShadowParams");
        public static readonly int k_PointLightDepthParamsID = Shader.PropertyToID("_PointLightDepthParams");
        
        // Params Per Shadow Caster
        public static readonly int k_ShadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_BloomParamsId = Shader.PropertyToID("_BloomParams");
        public static readonly int k_BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        
        public static readonly int k_ColorGradingLUTParamsId = Shader.PropertyToID("_ColorGradingLUTParams");
        
        public static readonly int k_ColorAdjustmentsParamsId = Shader.PropertyToID("_ColorAdjustmentsParams");
        public static readonly int k_ColorFilterId = Shader.PropertyToID("_ColorFilter");
        public static readonly int k_WhiteBalanceId = Shader.PropertyToID("_WhiteBalance");
        
        public static readonly int k_CurveMaster  = Shader.PropertyToID("_CurveMaster");
        public static readonly int k_CurveRed = Shader.PropertyToID("_CurveRed");
        public static readonly int k_CurveGreen = Shader.PropertyToID("_CurveGreen");
        public static readonly int k_CurveBlue = Shader.PropertyToID("_CurveBlue");
        public static readonly int k_CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");
        public static readonly int k_CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");
        public static readonly int k_CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");
        public static readonly int k_CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");
        
        public static readonly int k_SMHShadowsID = Shader.PropertyToID("_SMHShadows");
        public static readonly int k_SMHMidtonesID = Shader.PropertyToID("_SMHMidtones");
        public static readonly int k_SMHHighlightsID = Shader.PropertyToID("_SMHHighlights");
        public static readonly int k_SMHRangeID = Shader.PropertyToID("_SMHRange");
        
        public static readonly int k_LGGLiftID = Shader.PropertyToID("_LGGLift");
        public static readonly int k_LGGGammaID = Shader.PropertyToID("_LGGGamma");
        public static readonly int k_LGGGainID = Shader.PropertyToID("_LGGGain");
        
        public static readonly int k_ToneMappingParamsId = Shader.PropertyToID("_ToneMappingParams");
        
        // ----------------------------------------------------------------------------------------------------
        // Uber Post Related Texture or Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_SpectralLutID = Shader.PropertyToID("_SpectralLut");
        public static readonly int k_ChromaticAberrationParamsID = Shader.PropertyToID("_ChromaticAberrationParams");
        
        public static readonly int k_BloomTexID = Shader.PropertyToID("_BloomTex");
        
        public static readonly int k_VignetteColorId = Shader.PropertyToID("_VignetteColor");
        public static readonly int k_VignetteParams1Id = Shader.PropertyToID("_VignetteParams1");
        public static readonly int k_VignetteParams2Id = Shader.PropertyToID("_VignetteParams2");
        
        public static readonly int k_ColorGradingLutParamsId = Shader.PropertyToID("_ColorGradingLutParams");
        
        public static readonly int k_ExtraLutId = Shader.PropertyToID("_ExtraLut");
        public static readonly int k_ExtraLutParamsID = Shader.PropertyToID("_ExtraLutParams");
        
        public static readonly int k_FilmGrainTexID = Shader.PropertyToID("_FilmGrainTex");
        public static readonly int k_FilmGrainParamsID = Shader.PropertyToID("_FilmGrainParams");
        public static readonly int k_FilmGrainTexParamsID = Shader.PropertyToID("_FilmGrainTexParams");
    }

    public static class YPipelineKeywords
    {
        // ----------------------------------------------------------------------------------------------------
        // Lighting Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        public const string k_ShadowMaskDistance = "_SHADOW_MASK_DISTANCE";
        public const string k_ShadowMaskNormal = "_SHADOW_MASK_NORMAL";
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        public const string k_BloomBicubicUpsampling = "_BLOOM_BICUBIC_UPSAMPLING";
        
        public const string k_Bloom = "_BLOOM";
        public const string k_ChromaticAberration = "_CHROMATIC_ABERRATION";
        public const string k_Vignette = "_VIGNETTE";
        public const string k_ExtraLut = "_EXTRA_LUT";
        public const string k_FilmGrain = "_FILM_GRAIN";
    }
}