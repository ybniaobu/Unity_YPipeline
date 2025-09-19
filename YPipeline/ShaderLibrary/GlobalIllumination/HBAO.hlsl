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

#define NUM_DIRECTIONS 4
#define NUM_STEPS 4

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

inline float InvLength(float2 v)
{
    return rsqrt(dot(v,v));
}

inline float GetHorizonAngleTan(float3 P, float3 S)
{
    return (P.z - S.z) * InvLength(S.xy - P.xy);
}
// //
// inline float GetTangentAngleTan(float3 T)
// {
//     return -T.y * InvLength(T.xz);
// }
//
// inline float GetBiasedTangentAngleTan(float3 T)
// {
//     return GetTangentAngleTan(T) + g_TanAngleBias;
// }
//
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

float Falloff(float d2, float radius)
{
    return saturate(1.0 - d2 * rcp(radius * radius));
}

float HorizonOcclusion(float2 dir, float2 pixelDelta, uint2 pixelCoord, float3 P, float3 normalVS, float numSteps, float rand, float radius)
{
    float ao = 0;
    float weightSum = 0;

    // Randomize starting point within the first sample distance
    pixelCoord += rand * pixelDelta;
    float sinT = -dot(normalVS, float3(dir.x, dir.y, 0));
    float lastSinH = 0;

    for (float j = 0; j < numSteps; ++j)
    {
        pixelCoord = clamp(pixelCoord, 0, _TextureSize.zw - 1);
        float2 screenUV = (pixelCoord + 0.5) * _TextureSize.xy;
        float sDepth = LoadDepth(pixelCoord);
        float3 S = FetchViewPosition(screenUV, sDepth);

        float d2 = Length2(S - P);
        
        // // float tanH = GetHorizonAngleTan(P, S);
        // // float sinH = TanToSin(tanH);
        // float3 H = normalize(S - P);
        // float sinH = -H.z;
        //
        // if (d2 < radius * radius && (sinH > sinT))
        // {
        //     float weight = Falloff(d2, radius);
        //     ao += weight * (sinH - sinT);
        //     sinT = sinH;
        // }

        float3 H = normalize(S - P);
        float sinH = saturate(dot(H, normalVS));
        
        if (d2 < radius * radius && sinH >= lastSinH)
        {
            float weight = Falloff(d2, radius);
            ao += weight * (sinH - lastSinH);
            lastSinH = sinH;
        }
        
        
        pixelCoord += pixelDelta;
    }

    return saturate(ao);
    // return saturate(ao / weightSum);
}

#endif