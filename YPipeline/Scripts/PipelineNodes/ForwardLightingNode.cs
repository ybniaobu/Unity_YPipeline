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
        // CBuffer ID
        // ----------------------------------------------------------------------------------------------------
        
        // Global Params Per Setting
        private static readonly int k_EnvBRDFLutID = Shader.PropertyToID("_EnvBRDFLut");
        private static readonly int k_CascadeSettingsID = Shader.PropertyToID("_CascadeSettings");
        private static readonly int k_ShadowBiasID = Shader.PropertyToID("_ShadowBias");
        private static readonly int k_SunLightShadowSettingsID = Shader.PropertyToID("_SunLightShadowSettings");
        private static readonly int k_PunctualLightShadowSettingsID = Shader.PropertyToID("_PunctualLightShadowSettings");
        
        // Sun Light Params Per Frame
        private static readonly int k_SunLightShadowMapID = Shader.PropertyToID("_SunLightShadowMap");
        private static readonly int k_SunLightColorId = Shader.PropertyToID("_SunLightColor");
        private static readonly int k_SunLightDirectionId = Shader.PropertyToID("_SunLightDirection");
        private static readonly int k_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        private static readonly int k_SunLightShadowMatricesID = Shader.PropertyToID("_SunLightShadowMatrices");
        
        // Spot and Point Light Params Per Frame
        private static readonly int k_PunctualLightCountId = Shader.PropertyToID("_PunctualLightCount");
        
        private static readonly int k_SpotLightShadowMapID = Shader.PropertyToID("_SpotLightShadowMap");
        private static readonly int k_SpotLightColorsId = Shader.PropertyToID("_SpotLightColors");
        private static readonly int k_SpotLightPositionsId = Shader.PropertyToID("_SpotLightPositions");
        private static readonly int k_SpotLightDirectionsId = Shader.PropertyToID("_SpotLightDirections");
        private static readonly int k_SpotLightParamsId = Shader.PropertyToID("_SpotLightParams");
        private static readonly int k_SpotLightShadowMatricesID = Shader.PropertyToID("_SpotLightShadowMatrices");
        
        private static readonly int k_PointLightShadowMapID = Shader.PropertyToID("_PointLightShadowMap");
        private static readonly int k_PointLightColorsId = Shader.PropertyToID("_PointLightColors");
        private static readonly int k_PointLightPositionsId = Shader.PropertyToID("_PointLightPositions");
        private static readonly int k_PointLightParamsId = Shader.PropertyToID("_PointLightParams");
        private static readonly int k_PointLightShadowMatricesID = Shader.PropertyToID("_PointLightShadowMatrices");
        
        // Params Per Shadow Caster
        private static readonly int k_ShadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
        
        // ----------------------------------------------------------------------------------------------------
        // Global Keywords
        // ----------------------------------------------------------------------------------------------------
        
        private const string k_ShadowMaskDistance = "_SHADOW_MASK_DISTANCE";
        private const string k_ShadowMaskNormal = "_SHADOW_MASK_NORMAL";
        
        private static GlobalKeyword m_ShadowMaskDistanceKeyword;
        private static GlobalKeyword m_ShadowMaskNormalKeyword;
        
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
        
        private int m_SpotLightCount;
        private int m_ShadowingSpotLightCount;
        private Vector4[] m_SpotLightColors;
        private Vector4[] m_SpotLightPositions;
        private Vector4[] m_SpotLightDirections;
        private Vector4[] m_SpotLightParams;
        private Matrix4x4[] m_SpotLightShadowMatrices;
        private int[] m_ShadowingSpotLightIndices; // store shadowing spot light visible light index
        
        private int m_PointLightCount;
        private int m_ShadowingPointLightCount;
        private Vector4[] m_PointLightColors;
        private Vector4[] m_PointLightPositions;
        private Vector4[] m_PointLightParams;
        private Matrix4x4[] m_PointLightShadowMatrices;
        private int[] m_ShadowingPointLightIndices; // store shadowing point light visible light index
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        // private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize()
        {
            // Global Keywords initialize
            m_ShadowMaskDistanceKeyword = GlobalKeyword.Create(k_ShadowMaskDistance);
            m_ShadowMaskNormalKeyword = GlobalKeyword.Create(k_ShadowMaskNormal);
            
            // reference fields initialize
            m_CascadeCullingSpheres = new Vector4[k_MaxCascadeCount];
            m_SunLightShadowMatrices = new Matrix4x4[k_MaxDirectionalLightCount * k_MaxCascadeCount];
            
            m_SpotLightColors = new Vector4[k_MaxSpotLightCount];
            m_SpotLightPositions = new Vector4[k_MaxSpotLightCount];
            m_SpotLightDirections = new Vector4[k_MaxSpotLightCount];
            m_SpotLightParams = new Vector4[k_MaxSpotLightCount];
            m_SpotLightShadowMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
            m_ShadowingSpotLightIndices = new int[k_MaxShadowingSpotLightCount];

            m_PointLightColors = new Vector4[k_MaxPointLightCount];
            m_PointLightPositions = new Vector4[k_MaxPointLightCount];
            m_PointLightParams = new Vector4[k_MaxPointLightCount];
            m_PointLightShadowMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
            m_ShadowingPointLightIndices = new int[k_MaxShadowingPointLightCount];
        }
        
        protected override void Dispose()
        {
            DestroyImmediate(this);
        }
        
        protected override void OnRelease(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRelease(asset, ref data);
            data.buffer.ReleaseTemporaryRT(k_SunLightShadowMapID);
            data.buffer.ReleaseTemporaryRT(k_SpotLightShadowMapID);
            data.buffer.ReleaseTemporaryRT(k_PointLightShadowMapID);
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
            data.context.Submit();
        }

        protected override void OnBegin(YRenderPipelineAsset asset, ref ScriptableRenderContext context, CommandBuffer buffer)
        {
            base.OnBegin(asset, ref context, buffer);
            
            // 只需传递一次的贴图或者变量
            buffer.SetGlobalTexture(k_EnvBRDFLutID, new RenderTargetIdentifier(asset.environmentBRDFLut));
            
            buffer.SetGlobalVector(k_CascadeSettingsID, new Vector4(asset.maxShadowDistance, asset.distanceFade, asset.cascadeCount, asset.cascadeEdgeFade));
            buffer.SetGlobalVector(k_ShadowBiasID, new Vector4(asset.depthBias, asset.slopeScaledDepthBias, asset.normalBias, asset.slopeScaledNormalBias));
            buffer.SetGlobalVector(k_SunLightShadowSettingsID, new Vector4(asset.sunLightShadowArraySize, asset.sunLightShadowSampleNumber, asset.sunLightPenumbraWidth * 0.02f, 0.0f)); 
            buffer.SetGlobalVector(k_PunctualLightShadowSettingsID, new Vector4(asset.punctualLightShadowArraySize, asset.punctualLightShadowSampleNumber, asset.punctualLightPenumbra * 0.002f, 0.0f));
            
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
            context.Submit();
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileId.LightingNode));
            base.OnRender(asset, ref data);
            RecordLightData(asset, ref data);
            DeliverLightData(asset, ref data);
            
            CreateAndRenderSunLightShadowArray(asset, ref data);
            CreateAndRenderPunctualLightShadowArray(asset, ref data);
            DeliverShadowData(asset, ref data);
            
            SetKeywords(asset, ref data);
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }
        
        private void RecordLightData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
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
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds)) m_ShadowingSunLightCount++;
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

        private void DeliverLightData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (m_SunLightCount > 0)
            {
                data.buffer.SetGlobalVector(k_SunLightColorId, m_SunLightColor);
                data.buffer.SetGlobalVector(k_SunLightDirectionId, m_SunLightDirection);
            }
            else
            {
                data.buffer.SetGlobalVector(k_SunLightColorId, Vector4.zero);
                data.buffer.SetGlobalVector(k_SunLightDirectionId, Vector4.zero);
            }
            
            data.buffer.SetGlobalVector(k_PunctualLightCountId, new Vector4(m_SpotLightCount, m_PointLightCount, 0.0f, 0.0f));
            
            if (m_SpotLightCount > 0)
            {
                data.buffer.SetGlobalVectorArray(k_SpotLightColorsId, m_SpotLightColors);
                data.buffer.SetGlobalVectorArray(k_SpotLightPositionsId, m_SpotLightPositions);
                data.buffer.SetGlobalVectorArray(k_SpotLightDirectionsId, m_SpotLightDirections);
                data.buffer.SetGlobalVectorArray(k_SpotLightParamsId, m_SpotLightParams);
            }
                
            if (m_PointLightCount > 0)
            {
                data.buffer.SetGlobalVectorArray(k_PointLightColorsId, m_PointLightColors);
                data.buffer.SetGlobalVectorArray(k_PointLightPositionsId, m_PointLightPositions);
                data.buffer.SetGlobalVectorArray(k_PointLightParamsId, m_PointLightParams);
            }
        }

        private void CreateAndRenderSunLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (m_ShadowingSunLightCount > 0)
            {
                data.buffer.GetTemporaryRTArray(k_SunLightShadowMapID, asset.sunLightShadowArraySize, asset.sunLightShadowArraySize
                    ,m_ShadowingSunLightCount * asset.cascadeCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                RenderSunLightShadowArray(asset,ref data);
            }
        }

        private void RenderSunLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_SunLightIndex);
            data.buffer.SetGlobalFloat(k_ShadowPancakingId, 1.0f);
            
            for (int i = 0; i < asset.cascadeCount; i++)
            {
                data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(m_SunLightIndex, i, asset.cascadeCount, asset.SpiltRatios
                    , asset.sunLightShadowArraySize, m_SunLightNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                splitData.shadowCascadeBlendCullingFactor = 1f;
                shadowDrawingSettings.splitData = splitData;
                
                m_CascadeCullingSpheres[i] = splitData.cullingSphere;
                m_SunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                data.buffer.SetRenderTarget(new RenderTargetIdentifier(k_SunLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }
        
        private void CreateAndRenderPunctualLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (m_ShadowingSpotLightCount > 0)
            {
                data.buffer.GetTemporaryRTArray(k_SpotLightShadowMapID, asset.punctualLightShadowArraySize, asset.punctualLightShadowArraySize
                    ,m_ShadowingSpotLightCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                
                RenderSpotLightShadowArray(asset,ref data);
            }
            
            if (m_ShadowingPointLightCount > 0)
            {
                data.buffer.GetTemporaryRT(k_PointLightShadowMapID, new RenderTextureDescriptor()
                {
                    colorFormat = RenderTextureFormat.Shadowmap,
                    depthBufferBits = 32,
                    dimension = TextureDimension.CubeArray,
                    width = asset.punctualLightShadowArraySize,
                    height = asset.punctualLightShadowArraySize,
                    volumeDepth = m_ShadowingPointLightCount * 6,
                    msaaSamples = 1
                });
                RenderPointLightShadowArray(asset,ref data);
            }
        }
        
        private void RenderSpotLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            data.buffer.SetGlobalFloat(k_ShadowPancakingId, 0.0f);
            for (int i = 0; i < m_ShadowingSpotLightCount; i++)
            {
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_ShadowingSpotLightIndices[i]);
                data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(m_ShadowingSpotLightIndices[i], out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                shadowDrawingSettings.splitData = splitData;
                
                m_SpotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                data.buffer.SetRenderTarget(new RenderTargetIdentifier(k_SpotLightShadowMapID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }
        
        private void RenderPointLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            data.buffer.SetGlobalFloat(k_ShadowPancakingId, 0.0f);
            for (int i = 0; i < m_ShadowingPointLightCount; i++)
            {
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_ShadowingPointLightIndices[i]);

                for (int j = 0; j < 6; j++)
                {
                    data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(m_ShadowingPointLightIndices[i], (CubemapFace) j, 0, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    shadowDrawingSettings.splitData = splitData;
                    viewMatrix.m11 = -viewMatrix.m11;
                    viewMatrix.m12 = -viewMatrix.m12;
                    viewMatrix.m13 = -viewMatrix.m13;
                    m_PointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                
                    data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                    data.buffer.SetRenderTarget(new RenderTargetIdentifier(k_PointLightShadowMapID, 0, CubemapFace.Unknown, i * 6 + j), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    data.buffer.ClearRenderTarget(true, false, Color.clear);
                    RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                    data.buffer.DrawRendererList(shadowRendererList);
                }
            }
        }

        private void DeliverShadowData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            data.buffer.SetGlobalVectorArray(k_CascadeCullingSpheresID, m_CascadeCullingSpheres);
            data.buffer.SetGlobalMatrixArray(k_SunLightShadowMatricesID, m_SunLightShadowMatrices);
            
            data.buffer.SetGlobalMatrixArray(k_SpotLightShadowMatricesID, m_SpotLightShadowMatrices);
            data.buffer.SetGlobalMatrixArray(k_PointLightShadowMatricesID, m_PointLightShadowMatrices);
        }

        private void SetKeywords(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask)
            {
                data.buffer.SetKeyword(m_ShadowMaskDistanceKeyword, m_UseShadowMask);
                data.buffer.SetKeyword(m_ShadowMaskNormalKeyword, false);
            }
            else
            {
                data.buffer.SetKeyword(m_ShadowMaskNormalKeyword, m_UseShadowMask);
                data.buffer.SetKeyword(m_ShadowMaskDistanceKeyword, false);
            }
        }
    }
}