using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using UnityEditor.VersionControl;

namespace YPipeline
{
    public class ForwardLightingNode : PipelineNode
    {
        // ----------------------------------------------------------------------------------------------------
        // Constants
        private const int m_MaxDirectionalLightCount = 1;  // Only Support One Directional Light - Sunlight
        private const int m_MaxCascadeCount = 4;
        
        // ----------------------------------------------------------------------------------------------------
        // CBuffer ID
        private static readonly int m_SunLightColorId = Shader.PropertyToID("_SunLightColor");
        private static readonly int m_SunLightDirectionId = Shader.PropertyToID("_SunLightDirection");
        private static readonly int m_SunLightShadowParamsId = Shader.PropertyToID("_SunLightShadowParams");
        private static readonly int m_SunLightShadowFadeParamsId = Shader.PropertyToID("_SunLightShadowFadeParams");
        private static readonly int m_SunLightShadowArrayID = Shader.PropertyToID("_SunLightShadowArray");
        private static readonly int m_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        private static readonly int m_SunLightShadowMatricesID = Shader.PropertyToID("_SunLightShadowMatrices");
        
        private static readonly int m_ShadowBiasID = Shader.PropertyToID("_ShadowBias");
        
        // ----------------------------------------------------------------------------------------------------
        // Keywords string
        private const string m_ShadowMaskDistance = "_SHADOW_MASK_DISTANCE";
        private const string m_ShadowMaskNormal = "_SHADOW_MASK_NORMAL";
        
        // ----------------------------------------------------------------------------------------------------
        // Global Keywords
        private static GlobalKeyword _ShadowMaskDistance;
        private static GlobalKeyword _ShadowMaskNormal;
        
        // ----------------------------------------------------------------------------------------------------
        // reference type fields
        private Vector4[] m_CascadeCullingSpheres;
        private Matrix4x4[] m_SunLightShadowMatrices;
        
        // ----------------------------------------------------------------------------------------------------
        // value type fields
        private int m_sunLightCount;
        private int m_shadowingSunLightCount;
        private int m_SunLightIndex;
        private float m_SunLightNearPlaneOffset;
        private Vector4 m_sunLightColor;
        private Vector4 m_sunLightDirection;

        private bool m_UseShadowMask;
        private float m_ShadowMaskChannel;
        
        
        // ----------------------------------------------------------------------------------------------------
        // 
        // private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        // private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize()
        {
            // Global Keywords initialize
            _ShadowMaskDistance = GlobalKeyword.Create(m_ShadowMaskDistance);
            _ShadowMaskNormal = GlobalKeyword.Create(m_ShadowMaskNormal);
            
            // reference fields initialize
            m_CascadeCullingSpheres = new Vector4[m_MaxCascadeCount];
            m_SunLightShadowMatrices = new Matrix4x4[m_MaxDirectionalLightCount * m_MaxCascadeCount];
        }
        
        protected override void Dispose()
        {
            //TODO:释放 TemporaryRT
        }

        protected override void OnSetup(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnSetup(asset, ref data);
            
            // 只需传递一次的贴图或者变量
            Shader.SetGlobalTexture("_EnvBRDFLut", asset.environmentBRDFLut);
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            RecordDirectLightData(asset, ref data);
            DeliverDirectLightData(asset, ref data);
            
            CreateAndRenderSunLightShadowArray(asset, ref data);
            DeliverShadowData(asset, ref data);
            
            SetKeywords(asset, ref data);
            
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }
        private void RecordDirectLightData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            m_sunLightCount = 0;
            m_shadowingSunLightCount = 0;
            m_UseShadowMask = false;
            m_ShadowMaskChannel = -1.0f;
            
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    if (m_sunLightCount >= m_MaxDirectionalLightCount) continue;
                    
                    m_SunLightIndex = i;
                    m_SunLightNearPlaneOffset = visibleLight.light.shadowNearPlane;
                    var color = visibleLight.finalColor;
                    m_sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    if (visibleLight.light.shadows != LightShadows.None && visibleLight.light.shadowStrength > 0f)
                    {
                        if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds)) m_shadowingSunLightCount++;
                        m_sunLightColor = new Vector4(color.r, color.g, color.b, visibleLight.light.shadowStrength);

                        LightBakingOutput lightBaking = visibleLight.light.bakingOutput;
                        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
                        {
                            m_UseShadowMask = true;
                            m_ShadowMaskChannel = lightBaking.occlusionMaskChannel;
                        }
                    }
                    else
                    {
                        m_sunLightColor = new Vector4(color.r, color.g, color.b, 0);
                    }
                    m_sunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    
                }
            }
        }

        private void DeliverDirectLightData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (m_sunLightCount != 0)
            {
                data.buffer.SetGlobalVector(m_SunLightColorId, m_sunLightColor);
                data.buffer.SetGlobalVector(m_SunLightDirectionId, m_sunLightDirection);
            }
            else
            {
                data.buffer.SetGlobalVector(m_SunLightColorId, Vector4.zero);
                data.buffer.SetGlobalVector(m_SunLightDirectionId, Vector4.zero);
            }
        }

        private void CreateAndRenderSunLightShadowArray(YRenderPipelineAsset asset, ref PipelinePerFrameData data)//渲染到整张阴影贴图，所有生成阴影的直接光
        {
            if (m_shadowingSunLightCount > 0)
            {
                data.buffer.GetTemporaryRTArray(m_SunLightShadowArrayID, asset.sunLightShadowArraySize, asset.sunLightShadowArraySize
                    ,m_MaxDirectionalLightCount * m_MaxCascadeCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                RenderToDirShadowMapTile(asset,ref data);
            }
            else
            {
                data.buffer.GetTemporaryRTArray(m_SunLightShadowArrayID, 1, 1
                    ,m_MaxDirectionalLightCount * m_MaxCascadeCount, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderToDirShadowMapTile(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, m_SunLightIndex);
            
            for (int i = 0; i < asset.cascadeCount; i++)
            {
                data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives( m_SunLightIndex, i, asset.cascadeCount, asset.SpiltRatios
                    , asset.sunLightShadowArraySize, m_SunLightNearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                splitData.shadowCascadeBlendCullingFactor = 1f;
                shadowDrawingSettings.splitData = splitData;
                
                m_CascadeCullingSpheres[i] = splitData.cullingSphere;
                m_SunLightShadowMatrices[i] = ShadowUtility.GetWorldToSunLightScreenMatrix(projectionMatrix * viewMatrix);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                data.buffer.SetRenderTarget(new RenderTargetIdentifier(m_SunLightShadowArrayID, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }

        private void DeliverShadowData(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            data.buffer.SetGlobalVector(m_ShadowBiasID, new Vector4(asset.depthBias, asset.slopeScaledDepthBias, asset.normalBias, asset.slopeScaledNormalBias));
            data.buffer.SetGlobalVector(m_SunLightShadowParamsId, new Vector4(asset.cascadeCount, asset.sunLightShadowArraySize, asset.shadowSampleNumber, asset.penumbraWidth));
            data.buffer.SetGlobalVector(m_SunLightShadowFadeParamsId, new Vector4(1.0f / asset.maxShadowDistance, 1.0f / asset.distanceFade, 1.0f / asset.cascadeEdgeFade, m_ShadowMaskChannel));
            data.buffer.SetGlobalVectorArray(m_CascadeCullingSpheresID, m_CascadeCullingSpheres);
            data.buffer.SetGlobalMatrixArray(m_SunLightShadowMatricesID, m_SunLightShadowMatrices);
        }

        private void SetKeywords(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            if (QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask)
            {
                data.buffer.SetKeyword(_ShadowMaskDistance, m_UseShadowMask);
                data.buffer.SetKeyword(_ShadowMaskNormal, false);
            }
            else
            {
                data.buffer.SetKeyword(_ShadowMaskNormal, m_UseShadowMask);
                data.buffer.SetKeyword(_ShadowMaskDistance, false);
            }
        }
    }
}