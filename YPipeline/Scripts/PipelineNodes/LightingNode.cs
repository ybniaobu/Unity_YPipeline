using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace YPipeline
{
    public class LightingNode : PipelineNode
    {
        // --------------------------------------------------------------------------------
        // CBuffer ID
        private static int m_DirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static int m_DirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static int m_DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
        private static int m_DirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
        
        private static int m_DirShadowMapID = Shader.PropertyToID("_DirectionalShadowMap");
        private static int m_DirShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static int m_CascadeCountID = Shader.PropertyToID("_CascadeCount");
        private static int m_CascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
        private static int m_ShadowDistanceID = Shader.PropertyToID("_ShadowDistance");
        
        // --------------------------------------------------------------------------------
        // CBuffer Data
        private int m_DirLightCount;                        //场景中的可见方向光数量
        private Vector4[] m_DirLightColors;
        private Vector4[] m_DirLightDirections;
        private Vector4[] m_DirLightShadowData;
        
        private Matrix4x4[] m_DirShadowMatrices;
        private Vector4[] m_CascadeCullingSpheres;
        
        // --------------------------------------------------------------------------------
        // Cascade fields
        private const int m_MaxCascadeCount = 4;
        
        // --------------------------------------------------------------------------------
        // Directional Light fields
        private const int m_MaxDirLightCount = 4;           //同时也是能投射阴影的方向光的最大数量
        
        // Directional Light Shadow fields
        struct ShadowingDirLight                            //存储能投射阴影的可见方向光的数据
        {
            public int visibleLightIndex;                   
        }
        private int m_ShadowingDirLightCount;               //场景中能投射阴影的可见方向光数量
        private ShadowingDirLight[] m_ShadowingDirLights;   //能投射阴影的可见方向光的数据数组
        
        protected override void Initialize()
        {
            // CBuffer data initialize
            m_DirLightColors = new Vector4[m_MaxDirLightCount];
            m_DirLightDirections = new Vector4[m_MaxDirLightCount];
            m_DirLightShadowData = new Vector4[m_MaxDirLightCount];
            m_DirShadowMatrices = new Matrix4x4[m_MaxDirLightCount * m_MaxCascadeCount];
            m_CascadeCullingSpheres = new Vector4[m_MaxCascadeCount];
            
            // other fields
            m_ShadowingDirLights = new ShadowingDirLight[m_MaxDirLightCount];
        }
        
        protected override void Dispose()
        {
            //TODO:释放 TemporaryRT
        }

        protected override void OnSetup(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnSetup(asset, ref data);
            SetupDirectionalLights(asset, ref data);
        }

        protected override void OnRender(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            base.OnRender(asset, ref data);
            RenderToDirShadowMap(asset, ref data);
            data.context.ExecuteCommandBuffer(data.buffer);
            data.buffer.Clear();
        }
        private void SetupDirectionalLights(YRenderPipelineAsset asset, ref PipelinePerFrameData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;

            m_DirLightCount = 0;
            m_ShadowingDirLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    //判断可见方向光是否开启了阴影、阴影强度是否不为 0、以及可见光是否能影响到开启了阴影投射的物体。
                    //若是，则认为它是 ShadowingDirLight，并记录下它的可见光序号及其他数据。
                    if (visibleLight.light.shadows != LightShadows.None 
                        && visibleLight.light.shadowStrength > 0f
                        && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        m_ShadowingDirLights[m_ShadowingDirLightCount] = new ShadowingDirLight 
                        {
                            visibleLightIndex = i
                        };
                        
                        m_DirLightShadowData[m_DirLightCount] = new Vector2(visibleLight.light.shadowStrength, 
                            asset.cascadeCount * m_ShadowingDirLightCount);
                        m_ShadowingDirLightCount++;
                    }
                    else
                    {
                        m_DirLightShadowData[m_DirLightCount] = Vector2.zero;
                    }
                        
                    //记录可见方向光数据
                    m_DirLightColors[m_DirLightCount] = (Vector4) visibleLight.finalColor;
                    m_DirLightDirections[m_DirLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                    m_DirLightCount++;
                    if (m_DirLightCount >= m_MaxDirLightCount) break;
                }
            }
            
            data.buffer.SetGlobalInt(m_DirLightCountId, m_DirLightCount);
            data.buffer.SetGlobalVectorArray(m_DirLightColorsId, m_DirLightColors);
            data.buffer.SetGlobalVectorArray(m_DirLightDirectionsId, m_DirLightDirections);
            data.buffer.SetGlobalVectorArray(m_DirLightShadowDataId, m_DirLightShadowData);
        }

        private void RenderToDirShadowMap(YRenderPipelineAsset asset, ref PipelinePerFrameData data)//渲染到整张阴影贴图，所有生成阴影的直接光
        {
            if (m_ShadowingDirLightCount > 0)
            {
                //获取阴影贴图并设置为 RenderTarget
                data.buffer.GetTemporaryRT(m_DirShadowMapID, 
                    asset.directionalShadowMapSize, asset.directionalShadowMapSize, 
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                
                data.buffer.SetRenderTarget(
                    new RenderTargetIdentifier(m_DirShadowMapID),
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            
                data.buffer.ClearRenderTarget(true, false, Color.clear);
                
                //渲染至阴影贴图，每张阴影贴图分为 4 个 tiles 分别存储一个 ShadowingDirLight 产生的贴图
                int tiles = asset.cascadeCount * m_ShadowingDirLightCount;
                int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
                int tileSize = asset.directionalShadowMapSize / split;
                
                for (int i = 0; i < m_ShadowingDirLightCount; i++) 
                {
                    RenderToDirShadowMapTile(asset,ref data, i, split, tileSize);
                }
            }
            else
            {
                //这部分可以不要
                data.buffer.GetTemporaryRT(m_DirShadowMapID, 
                    1, 1, 
                    32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }

            data.buffer.SetGlobalInt(m_CascadeCountID, asset.cascadeCount);
            data.buffer.SetGlobalVectorArray(m_CascadeCullingSpheresID, m_CascadeCullingSpheres);
            data.buffer.SetGlobalMatrixArray(m_DirShadowMatricesID, m_DirShadowMatrices);
            data.buffer.SetGlobalFloat(m_ShadowDistanceID, asset.maxShadowDistance);
        }

        private void RenderToDirShadowMapTile(YRenderPipelineAsset asset, ref PipelinePerFrameData data, int shadowingDirLightIndex, int split, int tileSize)
        {
            ShadowingDirLight shadowingDirLight = m_ShadowingDirLights[shadowingDirLightIndex];
            ShadowDrawingSettings shadowDrawingSettings = 
                new ShadowDrawingSettings(data.cullingResults, shadowingDirLight.visibleLightIndex);
            
            int cascadeCount = asset.cascadeCount;
            int tileOffset = shadowingDirLightIndex * cascadeCount;
            Vector3 ratios = asset.SpiltRatios;
            
            for (int i = 0; i < cascadeCount; i++)
            {
                data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    shadowingDirLight.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                shadowDrawingSettings.splitData = splitData;

                if (shadowingDirLightIndex == 0)
                {
                    Vector4 cullingSphere = splitData.cullingSphere;
                    cullingSphere.w *= cullingSphere.w;
                    m_CascadeCullingSpheres[i] = cullingSphere;
                }
                
                int tileIndex = tileOffset + i;
                
                Vector2 offset = new Vector2(tileIndex % split, tileIndex / split);
                data.buffer.SetViewport(new Rect(
                    offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
                ));
                m_DirShadowMatrices[tileIndex] = ShadowUtility.GetTiledDirLightWorldToShadowMatrix(
                    projectionMatrix * viewMatrix, offset, split);
                
                data.buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                RendererList shadowRendererList = data.context.CreateShadowRendererList(ref shadowDrawingSettings);
                data.buffer.DrawRendererList(shadowRendererList);
            }
        }
    }
}