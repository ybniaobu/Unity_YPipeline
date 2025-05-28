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
            public SunLightConstantBuffer sunLightCBuffer;
        }
        
        protected override void Initialize() { }
        
        struct SunLightConstantBuffer
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
                sunLightShadowColor = lightsData.sunLightShadowColor;
                sunLightPenumbraColor = lightsData.sunLightPenumbraColor;
                sunLightShadowBias = lightsData.sunLightShadowBias;
                sunLightShadowParams = lightsData.sunLightShadowParams;
                sunLightShadowParams2 = lightsData.sunLightShadowParams2;
            }
        }

        struct PunctualLightStructuredBuffer
        {
            
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<ForwardLightsPassData>("Set Global Light Data", out var passData))
            {
                RecordLightsData(ref data);
                passData.sunLightCBuffer.Setup(data.lightsData);
                passData.isSunLightShadowing = data.lightsData.shadowingSunLightCount > 0;
                
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                builder.SetRenderFunc((ForwardLightsPassData data, RenderGraphContext context) =>
                {
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightCBuffer.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightCBuffer.sunLightDirection);
                    
                    if (data.isSunLightShadowing)
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowColorID, data.sunLightCBuffer.sunLightShadowColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightPenumbraColorID, data.sunLightCBuffer.sunLightPenumbraColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, data.sunLightCBuffer.sunLightShadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, data.sunLightCBuffer.sunLightShadowParams);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParams2ID, data.sunLightCBuffer.sunLightShadowParams2);
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
                
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                    {
                        data.lightsData.sunLightShadowColor = yLight.shadowTint;
                        data.lightsData.sunLightShadowColor.w = light.shadowStrength;
                        data.lightsData.sunLightPenumbraColor = yLight.penumbraTint;
                        data.lightsData.sunLightShadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        data.lightsData.sunLightShadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.sunLightShadowParams2 = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingSunLightCount++;
                        data.lightsData.sunLightDirection.w = 1;
                    }
                    sunLightCount++;
                }
                // else if (visibleLight.lightType == LightType.Spot)
                // {
                //     if (spotLightCount >= k_MaxSpotLightCount) continue;
                //
                //     shadowsPassData.spotLightColors[spotLightCount] = visibleLight.finalColor;
                //     shadowsPassData.spotLightColors[spotLightCount].w = 0;
                //     shadowsPassData.spotLightPositions[spotLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                //     shadowsPassData.spotLightDirections[spotLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                //
                //     float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                //     float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                //     float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                //     float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                //     shadowsPassData.spotLightParams[spotLightCount] =
                //         new Vector4(invRadiusSquare, invAngleRange, cosOuterAngle,
                //             -1.0f); // when w is -1, light should skip shadow calculation
                //
                //     if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                //     {
                //         if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) &&
                //             shadowingSpotLightCount <= k_MaxShadowingSpotLightCount)
                //         {
                //             shadowsPassData.spotLightParams[spotLightCount].w = shadowingSpotLightCount;
                //             shadowsPassData.shadowingSpotLightIndices[shadowingSpotLightCount] = i;
                //             shadowsPassData.spotLightShadowBias[shadowingSpotLightCount] = new Vector4(
                //                 yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias,
                //                 yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                //             shadowsPassData.spotLightPCFParams[shadowingSpotLightCount] =
                //                 new Vector4(yPipelineLight.penumbraWidth, yPipelineLight.sampleNumber);
                //             shadowsPassData.spotLightShadowParams[shadowingSpotLightCount] = new Vector4(
                //                 yPipelineLight.lightSize, yPipelineLight.penumbraScale,
                //                 yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                //             shadowingSpotLightCount++;
                //         }
                //
                //         shadowsPassData.spotLightColors[spotLightCount].w = light.shadowStrength;
                //     }
                //
                //     spotLightCount++;
                // }
                // else if (visibleLight.lightType == LightType.Point)
                // {
                //     if (pointLightCount >= k_MaxPointLightCount) continue;
                //
                //     shadowsPassData.pointLightColors[pointLightCount] = visibleLight.finalColor;
                //     shadowsPassData.pointLightColors[pointLightCount].w = 0;
                //     shadowsPassData.pointLightPositions[pointLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                //
                //     float invRadiusSquare = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
                //     shadowsPassData.pointLightParams[pointLightCount] =
                //         new Vector4(invRadiusSquare, 0.0f, 0.0f,
                //             -1.0f); // when w is -1, light should skip shadow calculation
                //
                //     if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
                //     {
                //         if (data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds) &&
                //             shadowingPointLightCount <= k_MaxShadowingPointLightCount)
                //         {
                //             shadowsPassData.pointLightParams[pointLightCount].w = shadowingPointLightCount;
                //             shadowsPassData.shadowingPointLightIndices[shadowingPointLightCount] = i;
                //             shadowsPassData.pointLightShadowBias[shadowingPointLightCount] = new Vector4(
                //                 yPipelineLight.depthBias, yPipelineLight.slopeScaledDepthBias,
                //                 yPipelineLight.normalBias, yPipelineLight.slopeScaledNormalBias);
                //             shadowsPassData.pointLightPCFParams[shadowingPointLightCount] =
                //                 new Vector4(yPipelineLight.penumbraWidth, yPipelineLight.sampleNumber);
                //             shadowsPassData.pointLightShadowParams[shadowingPointLightCount] = new Vector4(
                //                 yPipelineLight.lightSize, yPipelineLight.penumbraScale,
                //                 yPipelineLight.blockerSearchSampleNumber, yPipelineLight.filterSampleNumber);
                //             shadowingPointLightCount++;
                //         }
                //
                //         shadowsPassData.pointLightColors[pointLightCount].w = light.shadowStrength;
                //     }
                //
                //     pointLightCount++;
                // }
            }

            data.lightsData.cascadeCount = data.asset.cascadeCount;
            data.lightsData.sunLightCount = sunLightCount;
            data.lightsData.shadowingSunLightCount = shadowingSunLightCount;
            // data.lightsData.spotLightCount = spotLightCount;
            // data.lightsData.shadowingSpotLightCount = shadowingSpotLightCount;
            // data.lightsData.pointLightCount = pointLightCount;
            // data.lightsData.shadowingPointLightCount = shadowingPointLightCount;
        }
    }
}