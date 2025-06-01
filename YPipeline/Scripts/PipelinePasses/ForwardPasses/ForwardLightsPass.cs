using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ForwardLightsPass : PipelinePass
    {
        private class ForwardLightsPassData
        {
            public bool isSunLightShadowing;
            public SunLightConstantBuffer sunLightData = new SunLightConstantBuffer();

            public BufferHandle punctualLightsBuffer;
            public int punctualLightsCount;
            public PunctualLightStructuredBuffer[] punctualLightsData = new PunctualLightStructuredBuffer[YPipelineLightsData.k_MaxPunctualLightCount];
        }
        
        protected override void Initialize() { }
        
        private struct SunLightConstantBuffer
        {
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;
            public Vector4 sunLightShadowColor;
            public Vector4 sunLightPenumbraColor; 
            public Vector4 sunLightShadowBias;
            public Vector4 sunLightShadowParams; 
            public Vector4 sunLightShadowParams2;

            public void Setup(YPipelineLightsData lightsData)
            {
                if (lightsData.sunLightCount > 0)
                {
                    sunLightColor = lightsData.sunLightColor;
                    sunLightDirection = lightsData.sunLightDirection;
                }
                else
                {
                    sunLightColor = Vector4.zero;
                    sunLightDirection = Vector4.zero;
                }

                if (lightsData.shadowingSunLightCount > 0)
                {
                    sunLightShadowColor = lightsData.sunLightShadowColor;
                    sunLightPenumbraColor = lightsData.sunLightPenumbraColor;
                    sunLightShadowBias = lightsData.sunLightShadowBias;
                    sunLightShadowParams = lightsData.sunLightShadowParams;
                    sunLightShadowParams2 = lightsData.sunLightShadowParams2;
                }
            }
        }

        private struct PunctualLightStructuredBuffer
        {
            public Vector4 punctualLightColors;
            public Vector4 punctualLightPositions;
            public Vector4 punctualLightDirections;
            public Vector4 punctualLightParams;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.punctualLightCount > 0)
                {
                    punctualLightColors = lightsData.punctualLightColors[index];
                    punctualLightPositions = lightsData.punctualLightPositions[index];
                    punctualLightDirections = lightsData.punctualLightDirections[index];
                    punctualLightParams = lightsData.punctualLightParams[index];
                }
            }
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardLightsPassData>("Set Global Light Data", out var passData))
            {
                RecordLightsData(ref data);
                
                passData.sunLightData.Setup(data.lightsData);
                for (int i = 0; i < data.lightsData.punctualLightCount; i++)
                {
                    passData.punctualLightsData[i].Setup(data.lightsData, i);
                }
                
                passData.isSunLightShadowing = data.lightsData.shadowingSunLightCount > 0;
                passData.punctualLightsCount = data.lightsData.punctualLightCount;
                
                data.PunctualLightBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxPunctualLightCount,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Punctual Lights Data"
                });
                passData.punctualLightsBuffer = builder.WriteBuffer(data.PunctualLightBufferHandle);
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((ForwardLightsPassData data, RenderGraphContext context) =>
                {
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightData.sunLightDirection);
                    
                    if (data.isSunLightShadowing)
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowColorID, data.sunLightData.sunLightShadowColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightPenumbraColorID, data.sunLightData.sunLightPenumbraColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, data.sunLightData.sunLightShadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, data.sunLightData.sunLightShadowParams);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParams2ID, data.sunLightData.sunLightShadowParams2);
                    }
                    
                    // Punctual Light Data
                    if (data.punctualLightsCount > 0)
                    {
                        context.cmd.SetBufferData(data.punctualLightsBuffer, data.punctualLightsData, 0, 0, data.punctualLightsCount);
                        context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightDataID, data.punctualLightsBuffer);
                    }
                    
                    context.renderContext.ExecuteCommandBuffer(context.cmd);
                    context.cmd.Clear();
                });
            }
        }

        private void RecordLightsData(ref YPipelineData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            int sunLightCount = 0;
            int shadowingSunLightCount = 0;
            int punctualLightCount = 0;
            int shadowingSpotLightCount = 0;
            int shadowingPointLightCount = 0;

            Array.Clear(data.lightsData.lightsBound, 0, data.lightsData.lightsBound.Length);

            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                YPipelineLight yLight = light.GetComponent<YPipelineLight>();

                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightsData.k_MaxDirectionalLightCount) continue;
                
                    data.lightsData.sunLightIndex = i;
                    data.lightsData.sunLightNearPlaneOffset = light.shadowNearPlane;
                    data.lightsData.sunLightColor = visibleLight.finalColor;
                    data.lightsData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    data.lightsData.sunLightDirection.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)
                
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.sunLightDirection.w = 1;
                        data.lightsData.sunLightShadowColor = yLight.shadowTint;
                        data.lightsData.sunLightShadowColor.w = light.shadowStrength;
                        data.lightsData.sunLightPenumbraColor = yLight.penumbraTint;
                        data.lightsData.sunLightShadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        data.lightsData.sunLightShadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.sunLightShadowParams2 = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingSunLightCount++;
                    }
                    sunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    if (punctualLightCount >= YPipelineLightsData.k_MaxPunctualLightCount) continue;
                
                    data.lightsData.punctualLightColors[punctualLightCount] = visibleLight.finalColor;
                    data.lightsData.punctualLightColors[punctualLightCount].w = 1;
                    data.lightsData.punctualLightPositions[punctualLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    data.lightsData.punctualLightPositions[punctualLightCount].w = -1; // when w is -1, light should skip shadow calculation
                    data.lightsData.punctualLightDirections[punctualLightCount] = Vector4.zero;
                
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    data.lightsData.punctualLightParams[punctualLightCount] = new Vector4(invRadiusSquare, yLight.rangeAttenuationScale, 0.0f, 0.0f);

                    Rect bound = visibleLight.screenRect;
                    data.lightsData.lightsBound[punctualLightCount] = new Vector4(bound.xMin, bound.yMin, bound.xMax, bound.yMax);
                    
                    bool isPointLightShadowing = light.shadows != LightShadows.None && light.shadowStrength > 0f && shadowingPointLightCount < YPipelineLightsData.k_MaxShadowingPointLightCount;
                
                    if (isPointLightShadowing && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.shadowingPointLightIndices[shadowingPointLightCount] = i;
                        data.lightsData.punctualLightPositions[punctualLightCount].w = shadowingPointLightCount;
                        data.lightsData.pointLightShadowColors[shadowingPointLightCount] = yLight.shadowTint;
                        data.lightsData.pointLightShadowColors[shadowingPointLightCount].w = light.shadowStrength;
                        data.lightsData.pointLightPenumbraColors[shadowingPointLightCount] = yLight.penumbraTint;
                        data.lightsData.pointLightShadowBias[shadowingPointLightCount] = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        Vector4 shadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.pointLightShadowParams[shadowingPointLightCount] = shadowParams;
                        data.lightsData.pointLightShadowParams2[shadowingPointLightCount] = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingPointLightCount++;
                    }
                    punctualLightCount++;
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    if (punctualLightCount >= YPipelineLightsData.k_MaxPunctualLightCount) continue;
                
                    data.lightsData.punctualLightColors[punctualLightCount] = visibleLight.finalColor;
                    data.lightsData.punctualLightColors[punctualLightCount].w = 2;
                    data.lightsData.punctualLightPositions[punctualLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    data.lightsData.punctualLightPositions[punctualLightCount].w = -1; // when w is -1, light should skip shadow calculation
                    data.lightsData.punctualLightDirections[punctualLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                
                    float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    data.lightsData.punctualLightParams[punctualLightCount] = new Vector4(invRadiusSquare, yLight.rangeAttenuationScale, invAngleRange, cosOuterAngle);
                    
                    Rect bound = visibleLight.screenRect;
                    data.lightsData.lightsBound[punctualLightCount] = new Vector4(bound.xMin, bound.yMin, bound.xMax, bound.yMax);

                    bool isSpotLightShadowing = light.shadows != LightShadows.None && light.shadowStrength > 0f && shadowingSpotLightCount < YPipelineLightsData.k_MaxShadowingSpotLightCount;
                
                    if (isSpotLightShadowing && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.shadowingSpotLightIndices[shadowingSpotLightCount] = i;
                        data.lightsData.punctualLightPositions[punctualLightCount].w = shadowingSpotLightCount;
                        data.lightsData.spotLightShadowColors[shadowingSpotLightCount] = yLight.shadowTint;
                        data.lightsData.spotLightShadowColors[shadowingSpotLightCount].w = light.shadowStrength;
                        data.lightsData.spotLightPenumbraColors[shadowingSpotLightCount] = yLight.penumbraTint;
                        data.lightsData.spotLightShadowBias[shadowingSpotLightCount] = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        Vector4 shadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.spotLightShadowParams[shadowingSpotLightCount] = shadowParams;
                        data.lightsData.spotLightShadowParams2[shadowingSpotLightCount] = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingSpotLightCount++;
                    }
                    punctualLightCount++;
                }
            }

            data.lightsData.cascadeCount = data.asset.cascadeCount;
            data.lightsData.sunLightCount = sunLightCount;
            data.lightsData.shadowingSunLightCount = shadowingSunLightCount;
            data.lightsData.punctualLightCount = punctualLightCount;
            data.lightsData.shadowingSpotLightCount = shadowingSpotLightCount;
            data.lightsData.shadowingPointLightCount = shadowingPointLightCount;
        }
    }
}