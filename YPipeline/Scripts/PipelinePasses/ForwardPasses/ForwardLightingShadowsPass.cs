using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

using System.Runtime.InteropServices;

namespace YPipeline
{
    public class ForwardLightingShadowsPass : PipelinePass
    {
        private class ForwardLightingShadowsPassData
        {
            public Vector4 cascadeSettings;
            public Vector4 shadowMapSizes;
            
            public bool isPCSSEnabled;

            public TextureHandle sunLightShadowMap;
            public TextureHandle spotLightShadowMap;
            public TextureHandle pointLightShadowMap;
            
            public int sunLightCount;
            public int cascadeCount;
            public int shadowingSunLightCount;
            public int sunLightIndex; // store shadowing sun light visible light index
            public float sunLightNearPlaneOffset;
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;
            public Vector4[] cascadeCullingSpheres = new Vector4[k_MaxCascadeCount];
            public Matrix4x4[] sunLightViewMatrices = new Matrix4x4[k_MaxCascadeCount];
            public Matrix4x4[] sunLightProjectionMatrices = new Matrix4x4[k_MaxCascadeCount];
            public Matrix4x4[] sunLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount];
            public Vector4 sunLightShadowBias;
            public Vector4 sunLightPCFParams;
            public Vector4 sunLightShadowParams;
            public Vector4[] sunLightDepthParams = new Vector4[k_MaxCascadeCount];
            public RendererListHandle[] sunLightShadowRendererList = new RendererListHandle[k_MaxCascadeCount];
            
            public int spotLightCount;
            public int shadowingSpotLightCount;
            public Vector4[] spotLightColors = new Vector4[k_MaxSpotLightCount];
            public Vector4[] spotLightPositions = new Vector4[k_MaxSpotLightCount];
            public Vector4[] spotLightDirections = new Vector4[k_MaxSpotLightCount];
            public Vector4[] spotLightParams = new Vector4[k_MaxSpotLightCount];
            public Matrix4x4[] spotLightViewMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
            public Matrix4x4[] spotLightProjectionMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
            public Matrix4x4[] spotLightShadowMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
            public int[] shadowingSpotLightIndices = new int[k_MaxShadowingSpotLightCount]; // store shadowing spot light visible light index
            public Vector4[] spotLightShadowBias = new Vector4[k_MaxShadowingSpotLightCount];
            public Vector4[] spotLightPCFParams = new Vector4[k_MaxShadowingSpotLightCount];
            public Vector4[] spotLightShadowParams = new Vector4[k_MaxShadowingSpotLightCount];
            public Vector4[] spotLightDepthParams = new Vector4[k_MaxShadowingSpotLightCount];
            public RendererListHandle[] spotLightShadowRendererList = new RendererListHandle[k_MaxShadowingSpotLightCount];
            
            public int pointLightCount;
            public int shadowingPointLightCount;
            public Vector4[] pointLightColors = new Vector4[k_MaxPointLightCount];
            public Vector4[] pointLightPositions = new Vector4[k_MaxPointLightCount];
            public Vector4[] pointLightParams = new Vector4[k_MaxPointLightCount];
            public Matrix4x4[] pointLightViewMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
            public Matrix4x4[] pointLightProjectionMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
            public Matrix4x4[] pointLightShadowMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
            public int[] shadowingPointLightIndices = new int[k_MaxShadowingPointLightCount]; // store shadowing point light visible light index
            public Vector4[] pointLightShadowBias = new Vector4[k_MaxShadowingPointLightCount];
            public Vector4[] pointLightPCFParams = new Vector4[k_MaxShadowingPointLightCount];
            public Vector4[] pointLightShadowParams = new Vector4[k_MaxShadowingPointLightCount];
            public Vector4[] pointLightDepthParams = new Vector4[k_MaxShadowingPointLightCount];
            public RendererListHandle[] pointLightShadowRendererList = new RendererListHandle[k_MaxShadowingPointLightCount * 6];
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        private const int k_MaxDirectionalLightCount = 1;  // Only Support One Directional Light - Sunlight
        private const int k_MaxCascadeCount = 4;
        private const int k_MaxSpotLightCount = 64;
        private const int k_MaxShadowingSpotLightCount = 32;
        private const int k_MaxPointLightCount = 32;
        private const int k_MaxShadowingPointLightCount = 8;
        
        // ----------------------------------------------------------------------------------------------------
        // Buffers
        // ----------------------------------------------------------------------------------------------------
        struct SunLightConstantBuffer
        {
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;
            public Vector4[] cascadeCullingSpheres;
            public Matrix4x4[] sunLightShadowMatrices;
            public Vector4 sunLightShadowBias;
            public Vector4 sunLightPCFParams;
            public Vector4 sunLightShadowParams;
            public Vector4[] sunLightDepthParams;

            public SunLightConstantBuffer(ForwardLightingShadowsPassData shadowsPassData)
            {
                if (shadowsPassData.sunLightCount > 0)
                {
                    sunLightColor = shadowsPassData.sunLightColor;
                    sunLightDirection = shadowsPassData.sunLightDirection;
                }
                else
                {
                    sunLightColor = Vector4.zero;
                    sunLightDirection = Vector4.zero;
                }
                cascadeCullingSpheres = shadowsPassData.cascadeCullingSpheres;
                sunLightShadowMatrices = shadowsPassData.sunLightShadowMatrices;
                sunLightShadowBias = shadowsPassData.sunLightShadowBias;
                sunLightPCFParams = shadowsPassData.sunLightPCFParams;
                sunLightShadowParams = shadowsPassData.sunLightShadowParams;
                sunLightDepthParams = shadowsPassData.sunLightDepthParams;
            }
        }
        
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        // private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize()
        {

        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardLightingShadowsPassData>("Lighting & Shadows", out var passData))
            {
                passData.cascadeSettings = new Vector4(data.asset.maxShadowDistance, data.asset.distanceFade, data.asset.cascadeCount, data.asset.cascadeEdgeFade);
                passData.shadowMapSizes = new Vector4(data.asset.sunLightShadowMapSize, data.asset.spotLightShadowMapSize, data.asset.pointLightShadowMapSize);
                passData.isPCSSEnabled = data.asset.shadowMode == ShadowMode.PCSS;
                
                RecordLightingNodeData(ref data, builder, passData);
                CreateSunLightShadowMap(ref data, builder, passData);
                CreateSpotLightShadowMap(ref data, builder, passData);
                CreatePointLightShadowMap(ref data, builder, passData);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((ForwardLightingShadowsPassData data, RenderGraphContext context) =>
                {
                    // TODO: constant buffer 设置一次就行
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_CascadeSettingsID, data.cascadeSettings);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ShadowMapSizesID, data.shadowMapSizes);

                    if (data.isPCSSEnabled)
                    {
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCSS, true);
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCF, false);
                    }
                    else
                    {
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCSS, false);
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCF, true);
                    }
                    
                    if (data.sunLightCount > 0)
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightDirection);
                    }
                    else
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, Vector4.zero);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, Vector4.zero);
                    }
            
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(data.spotLightCount, data.pointLightCount, 0.0f, 0.0f));
            
                    if (data.spotLightCount > 0)
                    {
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightColorsID, data.spotLightColors);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightPositionsID, data.spotLightPositions);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightDirectionsID, data.spotLightDirections);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightParamsID, data.spotLightParams);
                    }
                
                    if (data.pointLightCount > 0)
                    {
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightColorsID, data.pointLightColors);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightPositionsID, data.pointLightPositions);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightParamsID, data.pointLightParams);
                    }
                    
                    context.cmd.BeginSample("Sun Light Shadows");
                    if (data.shadowingSunLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 1.0f);
                        for (int i = 0; i < data.cascadeCount; i++)
                        { 
                            context.cmd.SetViewProjectionMatrices(data.sunLightViewMatrices[i], data.sunLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.sunLightShadowMap, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.sunLightShadowRendererList[i]);
                        }
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_SunLightShadowMapID, data.sunLightShadowMap);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, data.cascadeCullingSpheres);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, data.sunLightShadowMatrices);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, data.sunLightShadowBias);
                        if (data.isPCSSEnabled)
                        {
                            context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, data.sunLightShadowParams);
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, data.sunLightDepthParams);
                        }
                        else
                        {
                            context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightPCFParamsID, data.sunLightPCFParams);
                        }
                    }
                    context.cmd.EndSample("Sun Light Shadows");
                    
                    context.cmd.BeginSample("Spot Light Shadows");
                    if (data.shadowingSpotLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingSpotLightCount; i++)
                        {
                            context.cmd.SetViewProjectionMatrices(data.spotLightViewMatrices[i], data.spotLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.spotLightShadowMap, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.spotLightShadowRendererList[i]);
                        }
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_SpotLightShadowMapID, data.spotLightShadowMap);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SpotLightShadowMatricesID, data.spotLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowBiasID, data.spotLightShadowBias);
                        if (data.isPCSSEnabled)
                        {
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowParamsID, data.spotLightShadowParams);
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightDepthParamsID, data.spotLightDepthParams);
                        }
                        else
                        {
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightPCFParamsID, data.spotLightPCFParams);
                        }
                    }
                    context.cmd.EndSample("Spot Light Shadows");

                    context.cmd.BeginSample("Point Light Shadows");
                    if (data.shadowingPointLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingPointLightCount; i++)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                context.cmd.SetViewProjectionMatrices(data.pointLightViewMatrices[i * 6 + j], data.pointLightProjectionMatrices[i * 6 + j]);
                                context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.pointLightShadowMap, 0, CubemapFace.Unknown, i * 6 + j), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                                context.cmd.ClearRenderTarget(true, false, Color.clear);
                                context.cmd.DrawRendererList(data.pointLightShadowRendererList[i * 6 + j]);
                            }
                        }
                        
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_PointLightShadowMapID, data.pointLightShadowMap);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_PointLightShadowMatricesID, data.pointLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowBiasID, data.pointLightShadowBias);
                        if (data.isPCSSEnabled)
                        {
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowParamsID, data.pointLightShadowParams);
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightDepthParamsID, data.pointLightDepthParams);
                        }
                        else
                        {
                            context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightPCFParamsID, data.pointLightPCFParams);
                        }
                    }
                    context.cmd.EndSample("Point Light Shadows");
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
        
        private void RecordLightingNodeData(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingShadowsPassData shadowsPassData)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            int sunLightCount = 0;
            int shadowingSunLightCount = 0;
            int spotLightCount = 0;
            int shadowingSpotLightCount = 0;
            int pointLightCount = 0;
            int shadowingPointLightCount = 0;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                YPipelineLight yPipelineLight = light.GetComponent<YPipelineLight>();
                
                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= k_MaxDirectionalLightCount) continue;
                    
                    shadowsPassData.sunLightIndex = i;
                    shadowsPassData.sunLightNearPlaneOffset = light.shadowNearPlane;

                    shadowsPassData.sunLightColor = visibleLight.finalColor;
                    shadowsPassData.sunLightColor.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)
                    shadowsPassData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                        {
                            shadowsPassData.sunLightShadowBias = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            shadowsPassData.sunLightPCFParams = new Vector4(yPipelineLight.penumbraWidth, yPipelineLight.sampleNumber);
                            shadowsPassData.sunLightShadowParams = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingSunLightCount++;
                        }
                        shadowsPassData.sunLightColor.w = light.shadowStrength;
                    }
                    sunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    if (spotLightCount >= k_MaxSpotLightCount) continue;

                    shadowsPassData.spotLightColors[spotLightCount] = visibleLight.finalColor;
                    shadowsPassData.spotLightColors[spotLightCount].w = 0;
                    shadowsPassData.spotLightPositions[spotLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    shadowsPassData.spotLightDirections[spotLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    shadowsPassData.spotLightParams[spotLightCount] = new Vector4(invRadiusSquare, invAngleRange, cosOuterAngle, -1.0f); // when w is -1, light should skip shadow calculation
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && shadowingSpotLightCount <= k_MaxShadowingSpotLightCount)
                        {
                            shadowsPassData.spotLightParams[spotLightCount].w = shadowingSpotLightCount;
                            shadowsPassData.shadowingSpotLightIndices[shadowingSpotLightCount] = i;
                            shadowsPassData.spotLightShadowBias[shadowingSpotLightCount] = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            shadowsPassData.spotLightPCFParams[shadowingSpotLightCount] = new Vector4(yPipelineLight.penumbraWidth, yPipelineLight.sampleNumber);
                            shadowsPassData.spotLightShadowParams[shadowingSpotLightCount] = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingSpotLightCount++;
                        }
                        shadowsPassData.spotLightColors[spotLightCount].w = light.shadowStrength;
                    }
                    spotLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    if (pointLightCount >= k_MaxPointLightCount) continue;

                    shadowsPassData.pointLightColors[pointLightCount] = visibleLight.finalColor;
                    shadowsPassData.pointLightColors[pointLightCount].w = 0;
                    shadowsPassData.pointLightPositions[pointLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    shadowsPassData.pointLightParams[pointLightCount] = new Vector4(invRadiusSquare, 0.0f, 0.0f, -1.0f); // when w is -1, light should skip shadow calculation

                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && shadowingPointLightCount <= k_MaxShadowingPointLightCount)
                        {
                            shadowsPassData.pointLightParams[pointLightCount].w = shadowingPointLightCount;
                            shadowsPassData.shadowingPointLightIndices[shadowingPointLightCount] = i;
                            shadowsPassData.pointLightShadowBias[shadowingPointLightCount] = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            shadowsPassData.pointLightPCFParams[shadowingPointLightCount] = new Vector4(yPipelineLight.penumbraWidth, yPipelineLight.sampleNumber);
                            shadowsPassData.pointLightShadowParams[shadowingPointLightCount] = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingPointLightCount++;
                        }
                        shadowsPassData.pointLightColors[pointLightCount].w = light.shadowStrength;
                    }
                    pointLightCount++;
                }
            }
            
            shadowsPassData.cascadeCount = data.asset.cascadeCount;
            shadowsPassData.sunLightCount = sunLightCount;
            shadowsPassData.shadowingSunLightCount = shadowingSunLightCount;
            shadowsPassData.spotLightCount = spotLightCount;
            shadowsPassData.shadowingSpotLightCount = shadowingSpotLightCount;
            shadowsPassData.pointLightCount = pointLightCount;
            shadowsPassData.shadowingPointLightCount = shadowingPointLightCount;
        }

        private void CreateSunLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingShadowsPassData shadowsPassData)
        {
            data.isSunLightShadowMapCreated = false;
            if (shadowsPassData.shadowingSunLightCount > 0)
            {
                int size = data.asset.sunLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth32,
                    dimension = TextureDimension.Tex2DArray,
                    slices = shadowsPassData.shadowingSunLightCount * data.asset.cascadeCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    msaaSamples = MSAASamples.None,
                    name = "Sun Light Shadow Map"
                };

                data.SunLightShadowMap = data.renderGraph.CreateTexture(desc);
                shadowsPassData.sunLightShadowMap = builder.WriteTexture(data.SunLightShadowMap);
                data.isSunLightShadowMapCreated = true;
                
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, shadowsPassData.sunLightIndex);
            
                for (int i = 0; i < data.asset.cascadeCount; i++)
                {
                    data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(shadowsPassData.sunLightIndex, i, data.asset.cascadeCount, data.asset.SpiltRatios
                        , data.asset.sunLightShadowMapSize, shadowsPassData.sunLightNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                    //splitData.shadowCascadeBlendCullingFactor = 1f;
                    shadowDrawingSettings.splitData = splitData;
                
                    shadowsPassData.cascadeCullingSpheres[i] = splitData.cullingSphere;
                    shadowsPassData.sunLightViewMatrices[i] = viewMatrix;
                    shadowsPassData.sunLightProjectionMatrices[i] = projectionMatrix;
                    shadowsPassData.sunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    shadowsPassData.sunLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    shadowsPassData.sunLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(shadowsPassData.sunLightShadowRendererList[i]);
                }
            }
        }

        private void CreateSpotLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingShadowsPassData shadowsPassData)
        {
            data.isSpotLightShadowMapCreated = false;
            if (shadowsPassData.shadowingSpotLightCount > 0)
            {
                int size = data.asset.spotLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth32,
                    dimension = TextureDimension.Tex2DArray,
                    slices = shadowsPassData.shadowingSpotLightCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    msaaSamples = MSAASamples.None,
                    name = "Spot Light Shadow Map"
                };
                
                data.SpotLightShadowMap = data.renderGraph.CreateTexture(desc);
                shadowsPassData.spotLightShadowMap = builder.WriteTexture(data.SpotLightShadowMap);
                data.isSpotLightShadowMapCreated = true;
                
                for (int i = 0; i < shadowsPassData.shadowingSpotLightCount; i++)
                {
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, shadowsPassData.shadowingSpotLightIndices[i]);
                    data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowsPassData.shadowingSpotLightIndices[i], out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    shadowDrawingSettings.splitData = splitData;
                
                    shadowsPassData.spotLightViewMatrices[i] = viewMatrix;
                    shadowsPassData.spotLightProjectionMatrices[i] = projectionMatrix;
                    shadowsPassData.spotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    shadowsPassData.spotLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    shadowsPassData.spotLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(shadowsPassData.spotLightShadowRendererList[i]);
                }
            }
        }

        private void CreatePointLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingShadowsPassData shadowsPassData)
        {
            data.isPointLightShadowMapCreated = false;
            if (shadowsPassData.shadowingPointLightCount > 0)
            {
                int size = data.asset.pointLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth32,
                    dimension = TextureDimension.CubeArray,
                    slices = shadowsPassData.shadowingPointLightCount * 6,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    msaaSamples = MSAASamples.None,
                    name = "Point Light Shadow Map"
                };
                
                data.PointLightShadowMap = data.renderGraph.CreateTexture(desc);
                shadowsPassData.pointLightShadowMap = builder.WriteTexture(data.PointLightShadowMap);
                data.isPointLightShadowMapCreated = true;
                
                for (int i = 0; i < shadowsPassData.shadowingPointLightCount; i++)
                {
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, shadowsPassData.shadowingPointLightIndices[i]);

                    for (int j = 0; j < 6; j++)
                    {
                        data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(shadowsPassData.shadowingPointLightIndices[i], (CubemapFace) j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                        shadowDrawingSettings.splitData = splitData;
                        
                        // TODO: 解决正面剔除的问题
                        // viewMatrix.m11 = -viewMatrix.m11;
                        // viewMatrix.m12 = -viewMatrix.m12;
                        // viewMatrix.m13 = -viewMatrix.m13;
                        
                        // projectionMatrix.m11 = -projectionMatrix.m11;
                        
                        shadowsPassData.pointLightViewMatrices[i * 6 + j] = viewMatrix;
                        shadowsPassData.pointLightProjectionMatrices[i * 6 + j] = projectionMatrix;
                        shadowsPassData.pointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                        shadowsPassData.pointLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                        shadowsPassData.pointLightShadowRendererList[i * 6 + j] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                        builder.UseRendererList(shadowsPassData.pointLightShadowRendererList[i * 6 + j]);
                    }
                }
            }
        }
    }
}