using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace YPipeline
{
    public class ForwardLightingNode : PipelineNode
    {
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
        // fields used to record data
        // ----------------------------------------------------------------------------------------------------
        
        private bool m_UseShadowMask;
        
        private int m_SunLightCount;
        private int m_ShadowingSunLightCount;
        private int m_SunLightIndex; // store shadowing sun light visible light index
        private float m_SunLightNearPlaneOffset;
        private Vector4 m_SunLightColor;
        private Vector4 m_SunLightDirection;
        private Vector4[] m_CascadeCullingSpheres;
        private Matrix4x4[] m_SunLightShadowMatrices;
        private Vector4 m_SunLightShadowBias;
        private Vector4 m_SunLightShadowParams;
        private Vector4[] m_SunLightDepthParams;
        
        private int m_SpotLightCount;
        private int m_ShadowingSpotLightCount;
        private Vector4[] m_SpotLightColors;
        private Vector4[] m_SpotLightPositions;
        private Vector4[] m_SpotLightDirections;
        private Vector4[] m_SpotLightParams;
        private Matrix4x4[] m_SpotLightShadowMatrices;
        private int[] m_ShadowingSpotLightIndices; // store shadowing spot light visible light index
        private Vector4[] m_SpotLightShadowBias;
        private Vector4[] m_SpotLightShadowParams;
        private Vector4[] m_SpotLightDepthParams;
        
        
        private int m_PointLightCount;
        private int m_ShadowingPointLightCount;
        private Vector4[] m_PointLightColors;
        private Vector4[] m_PointLightPositions;
        private Vector4[] m_PointLightParams;
        private Matrix4x4[] m_PointLightShadowMatrices;
        private int[] m_ShadowingPointLightIndices; // store shadowing point light visible light index
        private Vector4[] m_PointLightShadowBias;
        private Vector4[] m_PointLightShadowParams;
        private Vector4[] m_PointLightDepthParams;
        
        // ----------------------------------------------------------------------------------------------------
        // YPipeLine Components
        // ----------------------------------------------------------------------------------------------------
        
        private YPipelineLight m_YPipelineLight;
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        // private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize()
        {
            m_CascadeCullingSpheres = new Vector4[k_MaxCascadeCount];
            m_SunLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount];
            m_SunLightDepthParams = new Vector4[k_MaxCascadeCount];
            
            m_SpotLightColors = new Vector4[k_MaxSpotLightCount];
            m_SpotLightPositions = new Vector4[k_MaxSpotLightCount];
            m_SpotLightDirections = new Vector4[k_MaxSpotLightCount];
            m_SpotLightParams = new Vector4[k_MaxSpotLightCount];
            m_SpotLightShadowMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
            m_ShadowingSpotLightIndices = new int[k_MaxShadowingSpotLightCount];
            m_SpotLightShadowBias  = new Vector4[k_MaxShadowingSpotLightCount];
            m_SpotLightShadowParams = new Vector4[k_MaxShadowingSpotLightCount];
            m_SpotLightDepthParams = new Vector4[k_MaxShadowingSpotLightCount];

            m_PointLightColors = new Vector4[k_MaxPointLightCount];
            m_PointLightPositions = new Vector4[k_MaxPointLightCount];
            m_PointLightParams = new Vector4[k_MaxPointLightCount];
            m_PointLightShadowMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
            m_ShadowingPointLightIndices = new int[k_MaxShadowingPointLightCount];
            m_PointLightShadowBias = new Vector4[k_MaxShadowingPointLightCount];
            m_PointLightShadowParams = new Vector4[k_MaxShadowingPointLightCount];
            m_PointLightDepthParams = new Vector4[k_MaxShadowingPointLightCount];
        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        protected override void OnRelease(ref YPipelineData data)
        {
            base.OnRelease(ref data);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_SunLightShadowMapID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_SpotLightShadowMapID);
            data.buffer.ReleaseTemporaryRT(YPipelineShaderIDs.k_PointLightShadowMapID);
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnBegin(ref YPipelineData data)
        {
            base.OnBegin(ref data);
            
            // 只需传递一次的贴图或者变量
            data.buffer.SetGlobalTexture(YPipelineShaderIDs.k_EnvBRDFLutID, new RenderTargetIdentifier(data.asset.pipelineResources.textures.environmentBRDFLut));
            
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_CascadeSettingsID, new Vector4(data.asset.maxShadowDistance, data.asset.distanceFade, data.asset.cascadeCount, data.asset.cascadeEdgeFade));
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_ShadowBiasID, new Vector4(data.asset.depthBias, data.asset.slopeScaledDepthBias, data.asset.normalBias, data.asset.slopeScaledNormalBias));
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowSettingsID, new Vector4(data.asset.sunLightShadowArraySize, data.asset.sunLightShadowSampleNumber, data.asset.sunLightPenumbraWidth, 0.0f)); 
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightShadowSettingsID, new Vector4(data.asset.punctualLightShadowArraySize, data.asset.punctualLightShadowSampleNumber, data.asset.punctualLightPenumbra, 0.0f));
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnRender(ref YPipelineData data)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.LightingNode));
            base.OnRender(ref data);
            RecordLightData(ref data);
            DeliverLightData(ref data);
            
            data.buffer.BeginSample("Shadows");
            CreateAndRenderSunLightShadowArray(ref data);
            CreateAndRenderPunctualLightShadowArray(ref data);
            DeliverShadowData(ref data);
            
            SetKeywords(ref data);
            
            data.buffer.EndSample("Shadows");
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }
        
        private void RecordLightData(ref YPipelineData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            m_SunLightCount = 0;
            m_ShadowingSunLightCount = 0;
            m_SpotLightCount = 0;
            m_ShadowingSpotLightCount = 0;
            m_PointLightCount = 0;
            m_ShadowingPointLightCount = 0;
            m_UseShadowMask = false;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                m_YPipelineLight = light.GetComponent<YPipelineLight>();
                float shadowMaskChannel = -1.0f;
                
                if (visibleLight.lightType == LightType.Directional)
                {
                    if (m_SunLightCount >= k_MaxDirectionalLightCount) continue;
                    
                    m_SunLightIndex = i;
                    m_SunLightNearPlaneOffset = light.shadowNearPlane;

                    m_SunLightColor = visibleLight.finalColor;
                    m_SunLightColor.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)
                    m_SunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                        {
                            m_SunLightShadowBias = new Vector4(m_YPipelineLight.depthBias, m_YPipelineLight.slopeScaledDepthBias, m_YPipelineLight.normalBias, m_YPipelineLight.slopeScaledNormalBias);
                            m_SunLightShadowParams = new Vector4(m_YPipelineLight.lightSize, m_YPipelineLight.penumbraScale, m_YPipelineLight.blockerSearchSampleNumber, m_YPipelineLight.filterSampleNumber);
                            m_ShadowingSunLightCount++;
                        }
                        m_SunLightColor.w = light.shadowStrength;

                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            m_UseShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }

                    m_SunLightDirection.w = shadowMaskChannel;
                    m_SunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    if (m_SpotLightCount >= k_MaxSpotLightCount) continue;

                    m_SpotLightColors[m_SpotLightCount] = visibleLight.finalColor;
                    m_SpotLightColors[m_SpotLightCount].w = 0;
                    m_SpotLightPositions[m_SpotLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    m_SpotLightDirections[m_SpotLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    m_SpotLightParams[m_SpotLightCount] = new Vector4(invRadiusSquare, invAngleRange, cosOuterAngle, -1.0f); // when w is -1, light should skip shadow calculation
                    
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && m_ShadowingSpotLightCount <= k_MaxShadowingSpotLightCount)
                        {
                            m_SpotLightParams[m_SpotLightCount].w = m_ShadowingSpotLightCount;
                            m_ShadowingSpotLightIndices[m_ShadowingSpotLightCount] = i;
                            m_SpotLightShadowBias[m_ShadowingSpotLightCount] = new Vector4(m_YPipelineLight.depthBias, m_YPipelineLight.slopeScaledDepthBias, m_YPipelineLight.normalBias, m_YPipelineLight.slopeScaledNormalBias);
                            m_SpotLightShadowParams[m_ShadowingSpotLightCount] = new Vector4(m_YPipelineLight.lightSize, m_YPipelineLight.penumbraScale, m_YPipelineLight.blockerSearchSampleNumber, m_YPipelineLight.filterSampleNumber);
                            m_ShadowingSpotLightCount++;
                        }
                        
                        m_SpotLightColors[m_SpotLightCount].w = light.shadowStrength;
                        
                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            m_UseShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }
                    
                    m_SpotLightPositions[m_SpotLightCount].w = shadowMaskChannel;
                    m_SpotLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    if (m_PointLightCount >= k_MaxPointLightCount) continue;

                    m_PointLightColors[m_PointLightCount] = visibleLight.finalColor;
                    m_PointLightColors[m_PointLightCount].w = 0;
                    m_PointLightPositions[m_PointLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    m_PointLightParams[m_PointLightCount] = new Vector4(invRadiusSquare, 0.0f, 0.0f, -1.0f); // when w is -1, light should skip shadow calculation

                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) && m_ShadowingPointLightCount <= k_MaxShadowingPointLightCount)
                        {
                            m_PointLightParams[m_PointLightCount].w = m_ShadowingPointLightCount;
                            m_ShadowingPointLightIndices[m_ShadowingPointLightCount] = i;
                            m_PointLightShadowBias[m_ShadowingPointLightCount] = new Vector4(m_YPipelineLight.depthBias, m_YPipelineLight.slopeScaledDepthBias, m_YPipelineLight.normalBias, m_YPipelineLight.slopeScaledNormalBias);
                            m_PointLightShadowParams[m_ShadowingPointLightCount] = new Vector4(m_YPipelineLight.lightSize, m_YPipelineLight.penumbraScale, m_YPipelineLight.blockerSearchSampleNumber, m_YPipelineLight.filterSampleNumber);
                            m_ShadowingPointLightCount++;
                        }
                            
                        m_PointLightColors[m_PointLightCount].w = light.shadowStrength;
                        
                        LightBakingOutput lightBaking = light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            m_UseShadowMask = true;
                            shadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }

                    m_PointLightPositions[m_PointLightCount].w = shadowMaskChannel;
                    m_PointLightCount++;
                }
            }
        }

        private void DeliverLightData(ref YPipelineData data)
        {
            if (m_SunLightCount > 0)
            {
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, m_SunLightColor);
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, m_SunLightDirection);
            }
            else
            {
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, Vector4.zero);
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, Vector4.zero);
            }
            
            data.buffer.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(m_SpotLightCount, m_PointLightCount, 0.0f, 0.0f));
            
            if (m_SpotLightCount > 0)
            {
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightColorsID, m_SpotLightColors);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightPositionsID, m_SpotLightPositions);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightDirectionsID, m_SpotLightDirections);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightParamsID, m_SpotLightParams);
            }
                
            if (m_PointLightCount > 0)
            {
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightColorsID, m_PointLightColors);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightPositionsID, m_PointLightPositions);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightParamsID, m_PointLightParams);
            }
        }

        private void CreateAndRenderSunLightShadowArray(ref YPipelineData data)
        {
            if (m_ShadowingSunLightCount > 0)
            {
                data.buffer.GetTemporaryRTArray(YPipelineShaderIDs.k_SunLightShadowMapID, data.asset.sunLightShadowArraySize, data.asset.sunLightShadowArraySize
                    ,m_ShadowingSunLightCount * data.asset.cascadeCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                RenderSunLightShadowArray(ref data);
            }
        }

        private void RenderSunLightShadowArray(ref YPipelineData data)
        {
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_SunLightIndex);
            data.buffer.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 1.0f);
            
            for (int i = 0; i < data.asset.cascadeCount; i++)
            {
                data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(m_SunLightIndex, i, data.asset.cascadeCount, data.asset.SpiltRatios
                    , data.asset.sunLightShadowArraySize, m_SunLightNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                splitData.shadowCascadeBlendCullingFactor = 1f;
                shadowDrawingSettings.splitData = splitData;
                
                m_CascadeCullingSpheres[i] = splitData.cullingSphere;
                m_SunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                m_SunLightDepthParams[i] = SystemInfo.usesReversedZBuffer
                    ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23)
                    : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                data.buffer.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_SunLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }
        
        private void CreateAndRenderPunctualLightShadowArray(ref YPipelineData data)
        {
            if (m_ShadowingSpotLightCount > 0)
            {
                data.buffer.GetTemporaryRTArray(YPipelineShaderIDs.k_SpotLightShadowMapID, data.asset.punctualLightShadowArraySize, data.asset.punctualLightShadowArraySize
                    ,m_ShadowingSpotLightCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                
                RenderSpotLightShadowArray(ref data);
            }
            
            if (m_ShadowingPointLightCount > 0)
            {
                data.buffer.GetTemporaryRT(YPipelineShaderIDs.k_PointLightShadowMapID, new RenderTextureDescriptor()
                {
                    colorFormat = RenderTextureFormat.Shadowmap,
                    depthBufferBits = 32,
                    dimension = TextureDimension.CubeArray,
                    width = data.asset.punctualLightShadowArraySize,
                    height = data.asset.punctualLightShadowArraySize,
                    volumeDepth = m_ShadowingPointLightCount * 6,
                    msaaSamples = 1
                });
                RenderPointLightShadowArray(ref data);
            }
        }
        
        private void RenderSpotLightShadowArray(ref YPipelineData data)
        {
            data.buffer.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
            for (int i = 0; i < m_ShadowingSpotLightCount; i++)
            {
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_ShadowingSpotLightIndices[i]);
                data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(m_ShadowingSpotLightIndices[i], out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                shadowDrawingSettings.splitData = splitData;
                
                m_SpotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                m_SpotLightDepthParams[i] = SystemInfo.usesReversedZBuffer
                    ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23)
                    : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                data.buffer.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_SpotLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }
        
        private void RenderPointLightShadowArray(ref YPipelineData data)
        {
            data.buffer.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
            for (int i = 0; i < m_ShadowingPointLightCount; i++)
            {
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_ShadowingPointLightIndices[i]);

                for (int j = 0; j < 6; j++)
                {
                    data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(m_ShadowingPointLightIndices[i], (CubemapFace) j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    shadowDrawingSettings.splitData = splitData;
                    viewMatrix.m11 = -viewMatrix.m11;
                    viewMatrix.m12 = -viewMatrix.m12;
                    viewMatrix.m13 = -viewMatrix.m13;
                    m_PointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    m_PointLightDepthParams[i] = SystemInfo.usesReversedZBuffer
                        ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23)
                        : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                
                    data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                    data.buffer.SetRenderTarget(new RenderTargetIdentifier(YPipelineShaderIDs.k_PointLightShadowMapID, 0, CubemapFace.Unknown, i * 6 + j), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    data.buffer.ClearRenderTarget(true, false, Color.clear);
                    RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                    data.buffer.DrawRendererList(shadowRendererList);
                }
            }
        }

        private void DeliverShadowData(ref YPipelineData data)
        {
            if (m_ShadowingSunLightCount > 0)
            {
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, m_CascadeCullingSpheres);
                data.buffer.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, m_SunLightShadowMatrices);
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, m_SunLightShadowBias);
                data.buffer.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, m_SunLightShadowParams);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, m_SunLightDepthParams);
            }

            if (m_ShadowingSpotLightCount > 0)
            {
                data.buffer.SetGlobalMatrixArray(YPipelineShaderIDs.k_SpotLightShadowMatricesID, m_SpotLightShadowMatrices);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowBiasID, m_SpotLightShadowBias);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightShadowParamsID, m_SpotLightShadowParams);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_SpotLightDepthParamsID, m_SpotLightDepthParams);
            }

            if (m_ShadowingPointLightCount > 0)
            {
                data.buffer.SetGlobalMatrixArray(YPipelineShaderIDs.k_PointLightShadowMatricesID, m_PointLightShadowMatrices);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowBiasID, m_PointLightShadowBias);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightShadowParamsID, m_PointLightShadowParams);
                data.buffer.SetGlobalVectorArray(YPipelineShaderIDs.k_PointLightDepthParamsID, m_PointLightDepthParams);
            }
        }

        private void SetKeywords(ref YPipelineData data)
        {
            if (QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask)
            {
                CoreUtils.SetKeyword(data.buffer, YPipelineKeywords.k_ShadowMaskDistance, m_UseShadowMask);
                CoreUtils.SetKeyword(data.buffer, YPipelineKeywords.k_ShadowMaskNormal, false);
            }
            else
            {
                CoreUtils.SetKeyword(data.buffer, YPipelineKeywords.k_ShadowMaskNormal, m_UseShadowMask);
                CoreUtils.SetKeyword(data.buffer, YPipelineKeywords.k_ShadowMaskDistance, false);
            }
        }
    }
}