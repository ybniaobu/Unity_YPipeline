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
        public static readonly int k_CameraMotionVectorTextureID = Shader.PropertyToID("_CameraMotionVectorTexture");
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
        
        // Persistent Textures
        public static readonly int k_TAAHistoryID = Shader.PropertyToID("_TAAHistory");
        
        // Result Textures
        public static readonly int k_TAATargetID = Shader.PropertyToID("_TAATarget");
        public static readonly int k_BloomTextureID = Shader.PropertyToID("_BloomTexture");
        public static readonly int k_ColorGradingLutTextureID = Shader.PropertyToID("_ColorGradingLutTexture");
        
        // Process Textures
        public static readonly int k_BloomLowerTextureID = Shader.PropertyToID("_BloomLowerTexture");
        public static readonly int k_BloomPrefilterTextureID = Shader.PropertyToID("_BloomPrefilterTexture");
        
        // ----------------------------------------------------------------------------------------------------
        // Non Builtin Camera Matrix IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_InverseProjectionMatrixID = Shader.PropertyToID("_MatrixIP");
        public static readonly int k_InverseViewProjectionMatrixID = Shader.PropertyToID("_MatrixIVP");
        public static readonly int k_NonJitteredViewProjectionMatrixID = Shader.PropertyToID("_MatrixNonJitteredVP");
        public static readonly int k_NonJitteredInverseViewProjectionMatrixID = Shader.PropertyToID("_MatrixNonJitteredIVP");
        public static readonly int k_PreviousViewProjectionMatrixID = Shader.PropertyToID("_MatrixPreviousVP");
        public static readonly int k_PreviousInverseViewProjectionMatrixID = Shader.PropertyToID("_MatrixPreviousIVP");
        public static readonly int k_NonJitteredPreviousViewProjectionMatrixID = Shader.PropertyToID("_MatrixNonJitteredPreviousVP");
        public static readonly int k_NonJitteredPreviousInverseViewProjectionMatrixID = Shader.PropertyToID("_MatrixNonJitteredPreviousIVP");
        
        // ----------------------------------------------------------------------------------------------------
        // Common Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_BufferSizeID = Shader.PropertyToID("_CameraBufferSize");
        
        // ----------------------------------------------------------------------------------------------------
        // Lights And Shadows Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        // Light Global Params Per Setting
        public static readonly int k_CascadeSettingsID = Shader.PropertyToID("_CascadeSettings");
        public static readonly int k_ShadowMapSizesID = Shader.PropertyToID("_ShadowMapSizes");
        
        // Sun Light & Shadow Data
        public static readonly int k_SunLightColorID = Shader.PropertyToID("_SunLightColor");
        public static readonly int k_SunLightDirectionID = Shader.PropertyToID("_SunLightDirection");
        public static readonly int k_SunLightShadowColorID = Shader.PropertyToID("_SunLightShadowColor");
        public static readonly int k_SunLightPenumbraColorID = Shader.PropertyToID("_SunLightPenumbraColor");
        public static readonly int k_SunLightShadowBiasID = Shader.PropertyToID("_SunLightShadowBias");
        public static readonly int k_SunLightShadowParamsID = Shader.PropertyToID("_SunLightShadowParams");
        public static readonly int k_SunLightShadowParams2ID = Shader.PropertyToID("_SunLightShadowParams2");
        
        public static readonly int k_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        public static readonly int k_SunLightShadowMatricesID = Shader.PropertyToID("_SunLightShadowMatrices");
        public static readonly int k_SunLightDepthParamsID = Shader.PropertyToID("_SunLightDepthParams");
        
        // Punctual Light & Shadow Data
        public static readonly int k_PunctualLightCountID = Shader.PropertyToID("_PunctualLightCount");
        
        public static readonly int k_PunctualLightDataID = Shader.PropertyToID("_PunctualLightData");
        public static readonly int k_PointLightShadowDataID = Shader.PropertyToID("_PointLightShadowData");
        public static readonly int k_PointLightShadowMatricesID = Shader.PropertyToID("_PointLightShadowMatrices");
        public static readonly int k_SpotLightShadowDataID = Shader.PropertyToID("_SpotLightShadowData");
        public static readonly int k_SpotLightShadowMatricesID = Shader.PropertyToID("_SpotLightShadowMatrices");
        
        // Params Per Shadow Caster
        public static readonly int k_ShadowPancakingID = Shader.PropertyToID("_ShadowPancaking");
        
        // ----------------------------------------------------------------------------------------------------
        // Light Culling Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_LightInputInfosID = Shader.PropertyToID("_LightInputInfos");
        public static readonly int k_TileParamsID = Shader.PropertyToID("_TileParams");
        public static readonly int k_CameraNearPlaneLBID = Shader.PropertyToID("_CameraNearPlaneLB");
        public static readonly int k_TileNearPlaneSizeID = Shader.PropertyToID("_TileNearPlaneSize");
        public static readonly int k_TilesLightIndicesBufferID = Shader.PropertyToID("_TilesLightIndicesBuffer");
        
        // ----------------------------------------------------------------------------------------------------
        // Post Processing Related Param IDs
        // ----------------------------------------------------------------------------------------------------
        
        public static readonly int k_TAAParamsID = Shader.PropertyToID("_TAAParams");
        
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
        // Lighting And Shadows Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        
        // public const string k_ShadowMaskDistance = "_SHADOW_MASK_DISTANCE";
        // public const string k_ShadowMaskNormal = "_SHADOW_MASK_NORMAL";
        
        public const string k_ShadowPCF = "_SHADOW_PCF";
        public const string k_ShadowPCSS = "_SHADOW_PCSS";
        
        // ----------------------------------------------------------------------------------------------------
        // Light Culling Related Keywords
        // ----------------------------------------------------------------------------------------------------
        
        public const string k_TileCullingSplitDepth = "_TILE_CULLING_SPLIT_DEPTH";
        
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