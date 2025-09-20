//----------------------------------------------------------------------------------
// This file was modified from: NVIDIA-Direct3D-SDK-11/SSAO11/NVSDK_D3D11_SSAO
//----------------------------------------------------------------------------------

//----------------------------------------------------------------------------------
// Copyright (c) 2011 NVIDIA Corporation. All rights reserved.
//
// TO  THE MAXIMUM  EXTENT PERMITTED  BY APPLICABLE  LAW, THIS SOFTWARE  IS PROVIDED
// *AS IS*  AND NVIDIA AND  ITS SUPPLIERS DISCLAIM  ALL WARRANTIES,  EITHER  EXPRESS
// OR IMPLIED, INCLUDING, BUT NOT LIMITED  TO, NONINFRINGEMENT,IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  IN NO EVENT SHALL  NVIDIA 
// OR ITS SUPPLIERS BE  LIABLE  FOR  ANY  DIRECT, SPECIAL,  INCIDENTAL,  INDIRECT,  OR  
// CONSEQUENTIAL DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION,  DAMAGES FOR LOSS 
// OF BUSINESS PROFITS, BUSINESS INTERRUPTION, LOSS OF BUSINESS INFORMATION, OR ANY 
// OTHER PECUNIARY LOSS) ARISING OUT OF THE  USE OF OR INABILITY  TO USE THIS SOFTWARE, 
// EVEN IF NVIDIA HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.
//----------------------------------------------------------------------------------

#ifndef YPIPELINE_HBAO_INCLUDED
#define YPIPELINE_HBAO_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float3 FetchViewPosition(float2 screenUV, float rawDepth)
{
    float4 NDC = GetNDCFromUVAndDepth(screenUV, rawDepth);
    float3 VS = TransformNDCToView(NDC, UNITY_MATRIX_I_P);
    VS.z = -VS.z;
    return VS;
}

inline float3 FetchViewNormal(float3 normalWS)
{
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    normalVS.z = -normalVS.z;
    return normalVS;
}

float3 MinDiff(float3 P, float3 Pr, float3 Pl)
{
    float3 V1 = Pr - P;
    float3 V2 = P - Pl;
    return (Length2(V1) < Length2(V2)) ? V1 : V2;
}

inline float InvLength(float2 v)
{
    return rsqrt(dot(v,v));
}

inline float GetHorizonAngleTan(float3 P, float3 S)
{
    return (P.z - S.z) * InvLength(S.xy - P.xy);
}

inline float GetTangentAngleTan(float3 T)
{
    return -T.z * InvLength(T.xy);
}

// inline float GetBiasedTangentAngleTan(float3 T)
// {
//     return GetTangentAngleTan(T) + g_TanAngleBias;
// }

inline float TanToSin(float x)
{
    return x * rsqrt(x * x + 1.0f);
}

// ----------------------------------------------------------------------------------------------------
// Calculate Ray Steps
// ----------------------------------------------------------------------------------------------------

void ComputeSteps(inout float stepSizeInPixel, inout float numSteps, float rayRadiusPixel)
{
    // Avoid oversampling if NUM_STEPS is greater than the kernel radius in pixels
    numSteps = min(NUM_STEPS, rayRadiusPixel);
    stepSizeInPixel = rayRadiusPixel / numSteps;
}

// ----------------------------------------------------------------------------------------------------
// HBAO Main Function
// ----------------------------------------------------------------------------------------------------

float Falloff(float d2, float r2)
{
    return saturate(1.0 - d2 * rcp(r2));
}

float HorizonOcclusion(float3 viewDir, float2 dir, float2 pixelDelta, uint2 pixelCoord, float3 P, float3 normalVS, float numSteps, float rand, float radius)
{
    float r2 = radius * radius;
    
    float ao = 0;

    // Randomize starting point within the first sample distance
    pixelCoord += rand * pixelDelta;

    // ---------- 官方代码 ----------
    // float2 deltaUV = pixelDelta * _TextureSize.xy;
    // float3 T = deltaUV.x * dPdu + deltaUV.y * dPdv;
    // float tanT = GetTangentAngleTan(T) + 0.1;
    // float sinT = TanToSin(tanT);
    //
    // for (float j = 0; j < numSteps; ++j)
    // {
    //     pixelCoord = clamp(pixelCoord, 0, _TextureSize.zw - 1);
    //     float2 screenUV = (pixelCoord + 0.5) * _TextureSize.xy;
    //     float sDepth = LoadDepth(pixelCoord);
    //     float3 S = FetchViewPosition(screenUV, sDepth);
    //     float d2 = Length2(S - P);
    //
    //     float tanH = GetHorizonAngleTan(P, S);
    //     
    //     [branch]
    //     if ((d2 < r2) && (tanH > tanT))
    //     {
    //         float sinH = TanToSin(tanH);
    //         ao += Falloff(d2, r2) * (sinH - sinT);
    //
    //         tanT = tanH;
    //         sinT = sinH;
    //     }
    //     
    //     pixelCoord += pixelDelta;
    // }

    // ---------- 改变版 ----------
    
    // float2 deltaUV = pixelDelta * _TextureSize.xy;
    // float3 T = deltaUV.x * dPdu + deltaUV.y * dPdv;
    // float tanT = GetTangentAngleTan(T);
    // float sinT = TanToSin(tanT);
    
    float3 sliceNormal = normalize(cross(float3(dir, 0), viewDir));
    float3 T = normalize(cross(normalVS, sliceNormal));
    float tanT = GetTangentAngleTan(T);
    float sinT = TanToSin(tanT);
    float lastSinH = sinT;
    
    for (float j = 0; j < numSteps; ++j)
    {
        pixelCoord = clamp(pixelCoord, 0, _TextureSize.zw - 1);
        float2 screenUV = (pixelCoord + 0.5) * _TextureSize.xy;
        float sDepth = LoadDepth(pixelCoord);
        float3 S = FetchViewPosition(screenUV, sDepth);
        float d2 = Length2(S - P);
    
        // float tanH = GetHorizonAngleTan(P, S);
        // float sinH = TanToSin(tanH);
    
        float3 H = (S - P) * rsqrt(d2);
        float sinH = -H.z;
        
        [branch]
        if (d2 < r2 && sinH > lastSinH)
        {
            ao += Falloff(d2, r2) * (sinH - lastSinH);
            lastSinH = sinH;
        }
        
        pixelCoord += pixelDelta;
    }

    // ---------- HBAO+ ----------
    
    // for (float j = 0; j < numSteps; ++j)
    // {
    //     pixelCoord = clamp(pixelCoord, 0, _TextureSize.zw - 1);
    //     float2 screenUV = (pixelCoord + 0.5) * _TextureSize.xy;
    //     float sDepth = LoadDepth(pixelCoord);
    //     float3 S = FetchViewPosition(screenUV, sDepth);
    //     
    //     float3 V = S - P;
    //     float VdotV = dot(V, V);
    //     float NdotV = dot(normalVS, V) * rsqrt(VdotV);
    //
    //     ao += saturate(NdotV) * saturate(Falloff(VdotV, r2));
    //     
    //     pixelCoord += pixelDelta;
    // }
    // return ao / (int) numSteps;
    
    return ao;
}

#endif