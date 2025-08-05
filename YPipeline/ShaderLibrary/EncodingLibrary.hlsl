#ifndef YPIPELINE_ENCODING_LIBRARY_INCLUDED
#define YPIPELINE_ENCODING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// R8G8B8A8
float3 EncodeNormalInto888(float3 normalWS)
{
    // From HDRP
    // The sign of the Z component of the normal MUST round-trip through the G-Buffer, otherwise
    // the reconstruction of the tangent frame for anisotropic GGX creates a seam along the Z axis.
    // The constant was eye-balled to not cause artifacts.
    // TODO: find a proper solution. E.g. we could re-shuffle the faces of the octahedron
    // s.t. the sign of the Z component round-trips.
    // const float seamThreshold = 1.0 / 1024.0;
    // normalWS.z = CopySign(max(seamThreshold, abs(normalWS.z)), normalWS.z);
    
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    return packNormalWS;
}

// R8G8B8A8
float3 DecodeNormalFrom888(float3 packNormalWS)
{
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    float3 normalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
    return normalWS;
}

#endif