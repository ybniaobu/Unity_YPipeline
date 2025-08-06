using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ForwardShadowsPass : PipelinePass
    {
        private class ForwardShadowsPassData
        {
            public bool isPCSSEnabled;

            public TextureHandle sunLightShadowMap;
            public TextureHandle spotLightShadowMap;
            public TextureHandle pointLightShadowMap;
            
            // Sun Light Shadow
            public int cascadeCount;
            public int shadowingSunLightCount;
            public Matrix4x4[] sunLightViewMatrices = new Matrix4x4[YPipelineLightsData.k_MaxCascadeCount];
            public Matrix4x4[] sunLightProjectionMatrices = new Matrix4x4[YPipelineLightsData.k_MaxCascadeCount];

            public SunLightShadowConstantBuffer sunLightShadowData = new SunLightShadowConstantBuffer();
            public RendererListHandle[] sunLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxCascadeCount];
            
            // Point Light Shadow
            public int shadowingPointLightCount;
            public Matrix4x4[] pointLightViewMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            public Matrix4x4[] pointLightProjectionMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            
            public PointLightShadowStructuredBuffer[] pointLightsShadowData = new PointLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingPointLightCount];
            public RendererListHandle[] pointLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            public BufferHandle pointLightShadowBuffer;
            
            public Matrix4x4[] pointLightShadowMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            public BufferHandle pointLightShadowMatricesBuffer;
            
            // Spot Light Shadow
            public int shadowingSpotLightCount;
            public Matrix4x4[] spotLightViewMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public Matrix4x4[] spotLightProjectionMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingSpotLightCount];

            public SpotLightShadowStructuredBuffer[] spotLightsShadowData = new SpotLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public RendererListHandle[] spotLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public BufferHandle spotLightShadowBuffer;
            
            public Matrix4x4[] spotLightShadowMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public BufferHandle spotLightShadowMatricesBuffer;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Buffers
        // ----------------------------------------------------------------------------------------------------
        struct SunLightShadowConstantBuffer
        {
            public Vector4[] cascadeCullingSpheres;
            public Matrix4x4[] sunLightShadowMatrices;
            public Vector4[] sunLightDepthParams;

            public void Setup(YPipelineLightsData lightsData)
            {
                if (lightsData.shadowingSunLightCount > 0)
                {
                    cascadeCullingSpheres = lightsData.cascadeCullingSpheres;
                    sunLightShadowMatrices = lightsData.sunLightShadowMatrices;
                    sunLightDepthParams = lightsData.sunLightDepthParams;
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct PointLightShadowStructuredBuffer
        {
            public Vector4 pointLightShadowColors;
            public Vector4 pointLightPenumbraColors;
            public Vector4 pointLightShadowBias;
            public Vector4 pointLightShadowParams;
            public Vector4 pointLightShadowParams2;
            public Vector4 pointLightDepthParams;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.shadowingPointLightCount > 0)
                {
                    pointLightShadowColors = lightsData.pointLightShadowColors[index];
                    pointLightPenumbraColors = lightsData.pointLightPenumbraColors[index];
                    pointLightShadowBias = lightsData.pointLightShadowBias[index];
                    pointLightShadowParams = lightsData.pointLightShadowParams[index];
                    pointLightShadowParams2 = lightsData.pointLightShadowParams2[index];
                    pointLightDepthParams = lightsData.pointLightDepthParams[index];
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct SpotLightShadowStructuredBuffer
        {
            public Vector4 spotLightShadowColors;
            public Vector4 spotLightPenumbraColors;
            public Vector4 spotLightShadowBias;
            public Vector4 spotLightShadowParams;
            public Vector4 spotLightShadowParams2;
            public Vector4 spotLightDepthParams;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.shadowingSpotLightCount > 0)
                {
                    spotLightShadowColors = lightsData.spotLightShadowColors[index];
                    spotLightPenumbraColors = lightsData.spotLightPenumbraColors[index];
                    spotLightShadowBias = lightsData.spotLightShadowBias[index];
                    spotLightShadowParams = lightsData.spotLightShadowParams[index];
                    spotLightShadowParams2 = lightsData.spotLightShadowParams2[index];
                    spotLightDepthParams = lightsData.spotLightDepthParams[index];
                }
            }
        }
        
        
        // ----------------------------------------------------------------------------------------------------
        // Shadow Culling Related Fields
        // ----------------------------------------------------------------------------------------------------
        
        private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize()
        {

        }
        
        protected override void OnDispose()
        {
            //DestroyImmediate(this);
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardShadowsPassData>("Render Shadow Maps", out var passData))
            {
                m_CullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(data.cullingResults.visibleLights.Length, Allocator.Temp);
                m_ShadowSplitDataPerLight = new NativeArray<ShadowSplitData>(m_CullingInfoPerLight.Length * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                
                passData.isPCSSEnabled = data.asset.shadowMode == ShadowMode.PCSS;
                passData.cascadeCount = data.lightsData.cascadeCount;
                passData.shadowingSunLightCount = data.lightsData.shadowingSunLightCount;
                passData.shadowingPointLightCount = data.lightsData.shadowingPointLightCount;
                passData.shadowingSpotLightCount = data.lightsData.shadowingSpotLightCount;
                
                // Shadow Culling & Shadow Map Creation & Renderer List Preparation
                CreateSunLightShadowMap(ref data, builder, passData);
                CreateSpotLightShadowMap(ref data, builder, passData);
                CreatePointLightShadowMap(ref data, builder, passData);

                if (passData.shadowingSunLightCount + passData.shadowingPointLightCount + passData.shadowingSpotLightCount > 0)
                {
                    data.context.CullShadowCasters(data.cullingResults, new ShadowCastersCullingInfos
                    {
                        perLightInfos = m_CullingInfoPerLight,
                        splitBuffer = m_ShadowSplitDataPerLight
                    });
                }
                
                // Buffers Preparation
                passData.sunLightShadowData.Setup(data.lightsData);
                for (int i = 0; i < data.lightsData.shadowingPointLightCount; i++)
                {
                    passData.pointLightsShadowData[i].Setup(data.lightsData, i);
                }

                for (int i = 0; i < data.lightsData.shadowingSpotLightCount; i++)
                {
                    passData.spotLightsShadowData[i].Setup(data.lightsData, i);
                }

                data.PointLightShadowBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingPointLightCount,
                    stride = 16 * 6,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Point Lights Shadows Data"
                });
                passData.pointLightShadowBuffer = builder.WriteBuffer(data.PointLightShadowBufferHandle);
                
                data.PointLightShadowMatricesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingPointLightCount * 6,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Point Lights Shadow Matrices Data"
                });
                passData.pointLightShadowMatricesBuffer = builder.WriteBuffer(data.PointLightShadowMatricesBufferHandle);
                
                data.SpotLightShadowBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingSpotLightCount,
                    stride = 16 * 6,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Spot Lights Shadows Data"
                });
                passData.spotLightShadowBuffer = builder.WriteBuffer(data.SpotLightShadowBufferHandle);
                
                data.SpotLightShadowMatricesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingSpotLightCount,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Spot Lights Shadow Matrices Data"
                });
                passData.spotLightShadowMatricesBuffer = builder.WriteBuffer(data.SpotLightShadowMatricesBufferHandle);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);
                
                builder.SetRenderFunc((ForwardShadowsPassData data, RenderGraphContext context) =>
                {
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
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, data.sunLightShadowData.cascadeCullingSpheres);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, data.sunLightShadowData.sunLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, data.sunLightShadowData.sunLightDepthParams);
                    }
                    context.cmd.EndSample("Sun Light Shadows");
                    
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
                        context.cmd.SetBufferData(data.pointLightShadowBuffer, data.pointLightsShadowData, 0, 0, data.shadowingPointLightCount);
                        context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowDataID, data.pointLightShadowBuffer);
                        context.cmd.SetBufferData(data.pointLightShadowMatricesBuffer, data.pointLightShadowMatrices, 0, 0, data.shadowingPointLightCount * 6);
                        context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowMatricesID, data.pointLightShadowMatricesBuffer);
                    }
                    context.cmd.EndSample("Point Light Shadows");
                    
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
                        context.cmd.SetBufferData(data.spotLightShadowBuffer, data.spotLightsShadowData, 0, 0, data.shadowingSpotLightCount);
                        context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowDataID, data.spotLightShadowBuffer);
                        context.cmd.SetBufferData(data.spotLightShadowMatricesBuffer, data.spotLightShadowMatrices, 0, 0, data.shadowingSpotLightCount);
                        context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowMatricesID, data.spotLightShadowMatricesBuffer);
                    }
                    context.cmd.EndSample("Spot Light Shadows");
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }

        private void CreateSunLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardShadowsPassData passData)
        {
            data.isSunLightShadowMapCreated = false;
            if (data.lightsData.shadowingSunLightCount > 0)
            {
                int size = (int) data.asset.sunLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16, // DepthBits.Depth32
                    dimension = TextureDimension.Tex2DArray,
                    slices = data.lightsData.shadowingSunLightCount * data.lightsData.cascadeCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    msaaSamples = MSAASamples.None,
                    name = "Sun Light Shadow Map"
                };

                data.SunLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.sunLightShadowMap = builder.WriteTexture(data.SunLightShadowMap);
                data.isSunLightShadowMapCreated = true;

                int visibleLightIndex = data.lightsData.sunLightIndex;
                int cascadeCount = data.lightsData.cascadeCount;
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);
                int splitOffset = visibleLightIndex * 6;
                m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Orthographic,
                    splitRange = new RangeInt(splitOffset, cascadeCount)
                };
            
                for (int i = 0; i < cascadeCount; i++)
                {
                    data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(visibleLightIndex, i, cascadeCount, data.asset.SpiltRatios
                        , size, data.lightsData.sunLightNearPlaneOffset + 0.8f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                    //splitData.shadowCascadeBlendCullingFactor = 1f;
                    m_ShadowSplitDataPerLight[splitOffset + i] = splitData;
                    //shadowDrawingSettings.splitData = splitData;
                
                    data.lightsData.cascadeCullingSpheres[i] = splitData.cullingSphere;
                    data.lightsData.sunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    data.lightsData.sunLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    
                    passData.sunLightViewMatrices[i] = viewMatrix;
                    passData.sunLightProjectionMatrices[i] = projectionMatrix;
                    passData.sunLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.sunLightShadowRendererList[i]);
                }
            }
        }

        private void CreateSpotLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardShadowsPassData passData)
        {
            data.isSpotLightShadowMapCreated = false;
            if (data.lightsData.shadowingSpotLightCount > 0)
            {
                int size = (int) data.asset.spotLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16,
                    dimension = TextureDimension.Tex2DArray,
                    slices = data.lightsData.shadowingSpotLightCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    discardBuffer = true,
                    msaaSamples = MSAASamples.None,
                    name = "Spot Light Shadow Map"
                };
                
                data.SpotLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.spotLightShadowMap = builder.WriteTexture(data.SpotLightShadowMap);
                data.isSpotLightShadowMapCreated = true;
                
                for (int i = 0; i < data.lightsData.shadowingSpotLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingSpotLightIndices[i];
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);
                    data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    
                    int splitOffset = visibleLightIndex * 6;
                    m_ShadowSplitDataPerLight[splitOffset] = splitData;
                    m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitOffset, 1)
                    };
                    //shadowDrawingSettings.splitData = splitData;
                    
                    passData.spotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    data.lightsData.spotLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    
                    passData.spotLightViewMatrices[i] = viewMatrix;
                    passData.spotLightProjectionMatrices[i] = projectionMatrix;
                    passData.spotLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.spotLightShadowRendererList[i]);
                }
            }
        }

        private void CreatePointLightShadowMap(ref YPipelineData data, RenderGraphBuilder builder, ForwardShadowsPassData passData)
        {
            data.isPointLightShadowMapCreated = false;
            if (data.lightsData.shadowingPointLightCount > 0)
            {
                int size = (int) data.asset.pointLightShadowMapSize;

                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16,
                    dimension = TextureDimension.CubeArray,
                    slices = data.lightsData.shadowingPointLightCount * 6,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    discardBuffer = true,
                    msaaSamples = MSAASamples.None,
                    name = "Point Light Shadow Map"
                };
                
                data.PointLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.pointLightShadowMap = builder.WriteTexture(data.PointLightShadowMap);
                data.isPointLightShadowMapCreated = true;
                
                for (int i = 0; i < data.lightsData.shadowingPointLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingPointLightIndices[i];
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);
                    int splitOffset = visibleLightIndex * 6;
                    m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitOffset, 6)
                    };

                    for (int j = 0; j < 6; j++)
                    {
                        data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(visibleLightIndex, (CubemapFace) j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                        
                        m_ShadowSplitDataPerLight[splitOffset + j] = splitData;
                        //shadowDrawingSettings.splitData = splitData;
                        
                        // TODO: 解决正面剔除的问题
                        // viewMatrix.m11 = -viewMatrix.m11;
                        // viewMatrix.m12 = -viewMatrix.m12;
                        // viewMatrix.m13 = -viewMatrix.m13;
                        
                        // projectionMatrix.m11 = -projectionMatrix.m11;
                        
                        passData.pointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                        data.lightsData.pointLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                        
                        passData.pointLightViewMatrices[i * 6 + j] = viewMatrix;
                        passData.pointLightProjectionMatrices[i * 6 + j] = projectionMatrix;
                        passData.pointLightShadowRendererList[i * 6 + j] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                        builder.UseRendererList(passData.pointLightShadowRendererList[i * 6 + j]);
                    }
                }
            }
        }
    }
}