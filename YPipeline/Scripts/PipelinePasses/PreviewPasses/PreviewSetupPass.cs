using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class PreviewSetupPass : PipelinePass
    {
        private class SetupPassData
        {
            public Camera camera;
            
            // Resource Setup
            public TextureHandle envBRDFLut;
            
            public Vector2Int bufferSize;
            public Vector4 jitter;
            public Vector4 timeParams;
            public Vector4 cascadeSettings;
            public Vector4 shadowMapSizes;
            
            // Light Setup
            public SunLightConstantBuffer sunLightData = new SunLightConstantBuffer();
            public BufferHandle punctualLightsBuffer;
            public PunctualLightStructuredBuffer[] punctualLightsData = new PunctualLightStructuredBuffer[YPipelineLightsData.k_MaxPunctualLightCount];
        }
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        
        protected override void Initialize() { }
        
        private struct SunLightConstantBuffer
        {
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;

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
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PunctualLightStructuredBuffer
        {
            public Vector4 punctualLightColors;
            public Vector4 punctualLightPositions;
            public Vector4 punctualLightDirections;
            public Vector4 punctualLightParams;
        }

        public override void OnRecord(ref YPipelineData data)
        {
            using (RenderGraphBuilder builder = data.renderGraph.AddRenderPass<SetupPassData>("Preview Resource & Light Setup", out var passData))
            {
                passData.camera = data.camera;
                
                // ----------------------------------------------------------------------------------------------------
                // Imported texture resources
                // ----------------------------------------------------------------------------------------------------
                
                ImportBackBuffers(ref data);
                
                if (m_EnvBRDFLut == null || m_EnvBRDFLut.externalTexture != data.asset.pipelineResources.textures.environmentBRDFLut)
                {
                    m_EnvBRDFLut?.Release();
                    m_EnvBRDFLut = RTHandles.Alloc(data.asset.pipelineResources.textures.environmentBRDFLut);
                }
                passData.envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
                builder.ReadTexture(passData.envBRDFLut);
                
                // ----------------------------------------------------------------------------------------------------
                // Setup Light Data
                // ----------------------------------------------------------------------------------------------------
                
                RecordLightsData(ref data);
                
                passData.sunLightData.Setup(data.lightsData);
                
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

                builder.SetRenderFunc((SetupPassData data, RenderGraphContext context) =>
                {
                    context.cmd.SetupCameraProperties(data.camera);
                    
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightData.sunLightDirection);
                    
                    // Punctual Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(0, 0));
                    context.cmd.SetBufferData(data.punctualLightsBuffer, data.punctualLightsData);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightDataID, data.punctualLightsBuffer);
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
                // YPipelineLight yLight = light.GetYPipelineLight();

                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightsData.k_MaxDirectionalLightCount) continue;
                
                    data.lightsData.sunLightIndex = i;
                    data.lightsData.sunLightNearPlaneOffset = light.shadowNearPlane;
                    data.lightsData.sunLightColor = visibleLight.finalColor * Mathf.PI * 1.5f; // 乘以 pi * 1.5 是为了 preview 看起来正常点
                    data.lightsData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    data.lightsData.sunLightDirection.w = 0;
                    sunLightCount++;
                }
            }
            
            data.lightsData.sunLightCount = sunLightCount;
            data.lightsData.shadowingSunLightCount = shadowingSunLightCount;
            data.lightsData.punctualLightCount = punctualLightCount;
            data.lightsData.shadowingSpotLightCount = shadowingSpotLightCount;
            data.lightsData.shadowingPointLightCount = shadowingPointLightCount;
        }
        
        private void ImportBackBuffers(ref YPipelineData data)
        {
            RenderTargetIdentifier targetColorId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.Depth;
            
            if (m_CameraColorTarget == null || m_CameraColorTarget.nameID != targetColorId)
            {
                m_CameraColorTarget?.Release();
                m_CameraColorTarget = RTHandles.Alloc(targetColorId, "Backbuffer Color");
            }

            if (m_CameraDepthTarget == null || m_CameraDepthTarget.nameID != targetDepthId)
            {
                m_CameraDepthTarget?.Release();
                m_CameraDepthTarget = RTHandles.Alloc(targetDepthId, "Backbuffer Depth");
            }
            
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            
            if (data.camera.targetTexture == null)
            {
                importInfoColor.width = Screen.width;
                importInfoColor.height = Screen.height;
                importInfoColor.volumeDepth = 1;
                importInfoColor.msaaSamples = 1;

                importInfoColor.format = SystemInfo.GetGraphicsFormat(data.camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

                importInfoDepth = importInfoColor;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
                importInfoColor.width = data.camera.targetTexture.width;
                importInfoColor.height = data.camera.targetTexture.height;
                importInfoColor.volumeDepth = data.camera.targetTexture.volumeDepth;
                importInfoColor.msaaSamples = data.camera.targetTexture.antiAliasing;
                importInfoColor.format = data.camera.targetTexture.graphicsFormat;

                importInfoDepth = importInfoColor;
                importInfoDepth.format = data.camera.targetTexture.depthStencilFormat;
            }
            
            if (importInfoDepth.format == GraphicsFormat.None)
            {
                throw new System.Exception("In the render graph API, the output Render Texture must have a depth buffer.");
            }
            
            ImportResourceParams importBackbufferParams = new ImportResourceParams()
            {
                clearOnFirstUse = true,
                clearColor = Color.clear,
                discardOnLastUse = false
            };
            
            data.CameraColorTarget = data.renderGraph.ImportTexture(m_CameraColorTarget, importInfoColor, importBackbufferParams);
            data.CameraDepthTarget = data.renderGraph.ImportTexture(m_CameraDepthTarget, importInfoDepth, importBackbufferParams);
        }
    }
}