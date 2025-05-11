using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;

namespace YPipeline
{
    public class ForwardLightingNode : PipelineNode
    {
        private class ForwardLightingNodeData
        {
            public bool isAlreadySet = false;
            public TextureHandle envBRDFLut;
            // PCSS 和 PCF 整合后待修改
            public Vector4 cascadeSettings;
            public Vector4 shadowBias;
            public Vector4 sunLightShadowSettings;
            public Vector4 punctualLightShadowSettings;
            
            public bool useShadowMask;
            
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
        // 
        // ----------------------------------------------------------------------------------------------------
        
        private RTHandle m_EnvBRDFLut;
        
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
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardLightingNodeData>("Lighting & Shadows", out var nodeData))
            {
                RecordLightingNodeData(ref data, builder, nodeData);
                CreateSunLightShadowMap(ref data, builder, nodeData);
                CreateSpotLightShadowMap(ref data, builder, nodeData);
                CreatePointLightShadowMap(ref data, builder, nodeData);
                
                builder.SetRenderFunc((ForwardLightingNodeData data, RenderGraphContext context) =>
                {
                    if (!data.isAlreadySet)
                    {
                        context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_EnvBRDFLutID, data.envBRDFLut);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_CascadeSettingsID, data.cascadeSettings);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ShadowBiasID, data.shadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowSettingsID, data.sunLightShadowSettings);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightShadowSettingsID, data.punctualLightShadowSettings);
                        data.isAlreadySet = true;
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

                    if (data.shadowingSunLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 1.0f);
                        for (int i = 0; i < data.cascadeCount; i++)
                        { 
                            context.cmd.SetViewProjectionMatrices(data.sunLightViewMatrices[i], data.sunLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_SunLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.sunLightShadowRendererList[i]);
                        }
                        
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, data.cascadeCullingSpheres);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, data.sunLightShadowMatrices);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, data.sunLightShadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, data.sunLightShadowParams);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, data.sunLightDepthParams);
                    }

                    if (data.shadowingSpotLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingSpotLightCount; i++)
                        {
                            context.cmd.SetViewProjectionMatrices(data.spotLightViewMatrices[i], data.spotLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_SpotLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.spotLightShadowRendererList[i]);
                        }
                        
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SpotLightShadowMatricesID, data.spotLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowBiasID, data.spotLightShadowBias);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowParamsID, data.spotLightShadowParams);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightDepthParamsID, data.spotLightDepthParams);
                    }

                    if (data.shadowingPointLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingPointLightCount; i++)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                context.cmd.SetViewProjectionMatrices(data.pointLightViewMatrices[i * 6 + j], data.pointLightProjectionMatrices[i * 6 + j]);
                                context.cmd.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_PointLightShadowMapID, 0, CubemapFace.Unknown, i * 6 + j), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                                context.cmd.ClearRenderTarget(true, false, Color.clear);
                                context.cmd.DrawRendererList(data.pointLightShadowRendererList[i * 6 + j]);
                            }
                        }
                        
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_PointLightShadowMatricesID, data.pointLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowBiasID, data.pointLightShadowBias);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowParamsID, data.pointLightShadowParams);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightDepthParamsID, data.pointLightDepthParams);
                    }
                    
                    if (QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask)
                    {
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowMaskDistance, data.useShadowMask);
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowMaskNormal, false);
                    }
                    else
                    {
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowMaskNormal, data.useShadowMask);
                        CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowMaskDistance, false);
                    }
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }
        
        private void RecordLightingNodeData(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingNodeData nodeData)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            int sunLightCount = 0;
            int shadowingSunLightCount = 0;
            int spotLightCount = 0;
            int shadowingSpotLightCount = 0;
            int pointLightCount = 0;
            int shadowingPointLightCount = 0;
            bool useShadowMask = false;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                YPipelineLight yPipelineLight = light.GetComponent<YPipelineLight>();
                float shadowMaskChannel = -1.0f;
                
                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= k_MaxDirectionalLightCount) continue;
                    
                    nodeData.sunLightIndex = i;
                    nodeData.sunLightNearPlaneOffset = light.shadowNearPlane;

                    nodeData.sunLightColor = visibleLight.finalColor;
                    nodeData.sunLightColor.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)
                    nodeData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                        {
                            nodeData.sunLightShadowBias = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            nodeData.sunLightShadowParams = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingSunLightCount++;
                        }
                        nodeData.sunLightColor.w = light.shadowStrength;

                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            useShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }

                    nodeData.sunLightDirection.w = shadowMaskChannel;
                    sunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    if (spotLightCount >= k_MaxSpotLightCount) continue;

                    nodeData.spotLightColors[spotLightCount] = visibleLight.finalColor;
                    nodeData.spotLightColors[spotLightCount].w = 0;
                    nodeData.spotLightPositions[spotLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    nodeData.spotLightDirections[spotLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    nodeData.spotLightParams[spotLightCount] = new Vector4(invRadiusSquare, invAngleRange, cosOuterAngle, -1.0f); // when w is -1, light should skip shadow calculation
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && shadowingSpotLightCount <= k_MaxShadowingSpotLightCount)
                        {
                            nodeData.spotLightParams[spotLightCount].w = shadowingSpotLightCount;
                            nodeData.shadowingSpotLightIndices[shadowingSpotLightCount] = i;
                            nodeData.spotLightShadowBias[shadowingSpotLightCount] = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            nodeData.spotLightShadowParams[shadowingSpotLightCount] = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingSpotLightCount++;
                        }
                        
                        nodeData.spotLightColors[spotLightCount].w = light.shadowStrength;
                        
                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            useShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }
                    
                    nodeData.spotLightPositions[spotLightCount].w = shadowMaskChannel;
                    spotLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    if (pointLightCount >= k_MaxPointLightCount) continue;

                    nodeData.pointLightColors[pointLightCount] = visibleLight.finalColor;
                    nodeData.pointLightColors[pointLightCount].w = 0;
                    nodeData.pointLightPositions[pointLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    nodeData.pointLightParams[pointLightCount] = new Vector4(invRadiusSquare, 0.0f, 0.0f, -1.0f); // when w is -1, light should skip shadow calculation

                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && shadowingPointLightCount <= k_MaxShadowingPointLightCount)
                        {
                            nodeData.pointLightParams[pointLightCount].w = shadowingPointLightCount;
                            nodeData.shadowingPointLightIndices[shadowingPointLightCount] = i;
                            nodeData.pointLightShadowBias[shadowingPointLightCount] = new Vector4(yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias, yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                            nodeData.pointLightShadowParams[shadowingPointLightCount] = new Vector4(yPipelineLight.lightSize, yPipelineLight.penumbraScale, yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                            shadowingPointLightCount++;
                        }
                            
                        nodeData.pointLightColors[pointLightCount].w = light.shadowStrength;
                        
                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            useShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }

                    nodeData.pointLightPositions[pointLightCount].w = shadowMaskChannel;
                    pointLightCount++;
                }
            }
            
            nodeData.cascadeCount = data.asset.cascadeCount;
            nodeData.sunLightCount = sunLightCount;
            nodeData.shadowingSunLightCount = shadowingSunLightCount;
            nodeData.spotLightCount = spotLightCount;
            nodeData.shadowingSpotLightCount = shadowingSpotLightCount;
            nodeData.pointLightCount = pointLightCount;
            nodeData.shadowingPointLightCount = shadowingPointLightCount;
            nodeData.useShadowMask = useShadowMask;
            
            if (m_EnvBRDFLut == null)
            {
                m_EnvBRDFLut = RTHandles.Alloc(data.asset.pipelineResources.textures.environmentBRDFLut);
            }
            TextureHandle envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
            nodeData.envBRDFLut = envBRDFLut;
            builder.ReadTexture(envBRDFLut);
                
            nodeData.cascadeSettings = new Vector4(data.asset.maxShadowDistance, data.asset.distanceFade, data.asset.cascadeCount, data.asset.cascadeEdgeFade);
            nodeData.shadowBias = new Vector4(data.asset.depthBias, data.asset.slopeScaledDepthBias, data.asset.normalBias, data.asset.slopeScaledNormalBias);
            nodeData.sunLightShadowSettings = new Vector4(data.asset.sunLightShadowArraySize, data.asset.sunLightShadowSampleNumber, data.asset.sunLightPenumbraWidth, 0.0f);
            nodeData.punctualLightShadowSettings = new Vector4(data.asset.punctualLightShadowArraySize, data.asset.punctualLightShadowSampleNumber, data.asset.punctualLightPenumbra, 0.0f);
        }

        private void CreateSunLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingNodeData nodeData)
        {
            if (nodeData.shadowingSunLightCount > 0)
            {
                data.cmd.GetTemporaryRTArray(YPipelineShaderIDs.k_SunLightShadowMapID, data.asset.sunLightShadowArraySize, data.asset.sunLightShadowArraySize
                    ,nodeData.shadowingSunLightCount * data.asset.cascadeCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, nodeData.sunLightIndex);
            
                for (int i = 0; i < data.asset.cascadeCount; i++)
                {
                    data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(nodeData.sunLightIndex, i, data.asset.cascadeCount, data.asset.SpiltRatios
                        , data.asset.sunLightShadowArraySize, nodeData.sunLightNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                    splitData.shadowCascadeBlendCullingFactor = 1f;
                    shadowDrawingSettings.splitData = splitData;
                
                    nodeData.cascadeCullingSpheres[i] = splitData.cullingSphere;
                    nodeData.sunLightViewMatrices[i] = viewMatrix;
                    nodeData.sunLightProjectionMatrices[i] = projectionMatrix;
                    nodeData.sunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    nodeData.sunLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    nodeData.sunLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(nodeData.sunLightShadowRendererList[i]);
                }
            }
        }

        private void CreateSpotLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingNodeData nodeData)
        {
            if (nodeData.shadowingSpotLightCount > 0)
            {
                data.cmd.GetTemporaryRTArray(YPipelineShaderIDs.k_SpotLightShadowMapID, data.asset.punctualLightShadowArraySize, data.asset.punctualLightShadowArraySize
                    ,nodeData.shadowingSpotLightCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                
                for (int i = 0; i < nodeData.shadowingSpotLightCount; i++)
                {
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, nodeData.shadowingSpotLightIndices[i]);
                    data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(nodeData.shadowingSpotLightIndices[i], out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    shadowDrawingSettings.splitData = splitData;
                
                    nodeData.spotLightViewMatrices[i] = viewMatrix;
                    nodeData.spotLightProjectionMatrices[i] = projectionMatrix;
                    nodeData.spotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    nodeData.spotLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    nodeData.spotLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(nodeData.spotLightShadowRendererList[i]);
                }
            }
        }

        private void CreatePointLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardLightingNodeData nodeData)
        {
            if (nodeData.shadowingPointLightCount > 0)
            {
                data.cmd.GetTemporaryRT(YPipelineShaderIDs.k_PointLightShadowMapID, new RenderTextureDescriptor()
                {
                    colorFormat = RenderTextureFormat.Shadowmap,
                    depthBufferBits = 32,
                    dimension = TextureDimension.CubeArray,
                    width = data.asset.punctualLightShadowArraySize,
                    height = data.asset.punctualLightShadowArraySize,
                    volumeDepth = nodeData.shadowingPointLightCount * 6,
                    msaaSamples = 1
                });
                
                for (int i = 0; i < nodeData.shadowingPointLightCount; i++)
                {
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, nodeData.shadowingPointLightIndices[i]);

                    for (int j = 0; j < 6; j++)
                    {
                        data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(nodeData.shadowingPointLightIndices[i], (CubemapFace) j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                        shadowDrawingSettings.splitData = splitData;
                        viewMatrix.m11 = -viewMatrix.m11;
                        viewMatrix.m12 = -viewMatrix.m12;
                        viewMatrix.m13 = -viewMatrix.m13;
                        
                        nodeData.pointLightViewMatrices[i * 6 + j] = viewMatrix;
                        nodeData.pointLightProjectionMatrices[i * 6 + j] = projectionMatrix;
                        nodeData.pointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                        nodeData.pointLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                        nodeData.pointLightShadowRendererList[i * 6 + j] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                        builder.UseRendererList(nodeData.pointLightShadowRendererList[i * 6 + j]);
                    }
                }
            }
        }
    }
}