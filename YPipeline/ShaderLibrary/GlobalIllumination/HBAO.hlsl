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

#define DIRECTIONS 8
#define STEPS 6
#define NUM_STEPS 6
#define NUM_DIRECTIONS 8

// ----------------------------------------------------------------------------------------------------
// Utility Functions
// ----------------------------------------------------------------------------------------------------

// inline float3 FetchEyePos(uint2 uv)
// {
//     
// }

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

inline float TanToSin(float x)
{
    return x * rsqrt(x * x + 1.0f);
}

void ComputeSteps(inout float2 stepSizeUV, inout float numSteps, float rayRadiusPixel, float dither)
{
    // Avoid oversampling if NUM_STEPS is greater than the kernel radius in pixels
    numSteps = min(NUM_STEPS, rayRadiusPixel);

    // Divide by Ns+1 so that the farthest samples are not fully attenuated
    float stepSizePixel = rayRadiusPixel / (numSteps + 1);

    // // Clamp numSteps if it is greater than the max kernel footprint
    // float maxNumSteps = GetHBAOMaxPixelRadius() / stepSizePixel;
    // if (maxNumSteps < numSteps)
    // {
    //     // Use dithering to avoid AO discontinuities
    //     numSteps = floor(maxNumSteps + dither);
    //     numSteps = max(numSteps, 1);
    //     stepSizePixel = GetHBAOMaxPixelRadius() / numSteps;
    // }
    
    // Step size in uv space
    stepSizeUV = stepSizePixel * _TextureSize.xy;
}

#endif