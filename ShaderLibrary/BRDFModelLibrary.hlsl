﻿#ifndef YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED
#define YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED

#include "Assets/ShaderLibrary/BRDFTermsLibrary.hlsl"
// --------------------------------------------------------------------------------
// Standard PBR Model
struct StandardPBRParams
{
    float3 albedo;
    float roughness;
    float metallic;
    float3 F0;
    float3 N; // L、H is related to the light，see XXXLightsLibrary.
    float3 V;
    float NoV;
};

float3 StandardPBR(BRDFParams BRDFParams, StandardPBRParams standardPBRParams)
{
    float roughness = clamp(standardPBRParams.roughness, 0.02, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float3 diffuse = Fd_RenormalizedBurley_Disney(standardPBRParams.NoV, BRDFParams.NoL, BRDFParams.LoH, roughness, standardPBRParams.albedo);
    
    float D = D_GGX(BRDFParams.NoH, roughness);
    float V = V_SmithGGXCorrelated(standardPBRParams.NoV, BRDFParams.NoL, roughness);
    float3 F = F_Schlick(1, standardPBRParams.F0, BRDFParams.VoH);
    float3 specular = D * V * F;
    
    //float reflectance = (F.r + F.g + F.b) / 3;
    
    return (diffuse * (1 - F) * (1 - standardPBRParams.metallic) + specular) * BRDFParams.NoL;
}

float3 StandardPBR_EnergyCompensation(BRDFParams BRDFParams, StandardPBRParams standardPBRParams, float3 energyCompensation)
{
    float roughness = clamp(standardPBRParams.roughness, 0.02, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float3 diffuse = Fd_RenormalizedBurley_Disney(standardPBRParams.NoV, BRDFParams.NoL, BRDFParams.LoH, roughness, standardPBRParams.albedo);

    float D = D_GGX(BRDFParams.NoH, roughness);
    float V = V_SmithGGXCorrelated(standardPBRParams.NoV, BRDFParams.NoL, roughness);
    float3 F = F_Schlick(1, standardPBRParams.F0, BRDFParams.VoH);
    float3 specular = D * V * F;
    
    float reflectance = (F.r + F.g + F.b) / 3;
    
    return (diffuse * (1 - reflectance) * (1 - standardPBRParams.metallic) + specular * energyCompensation) * BRDFParams.NoL;
}

// --------------------------------------------------------------------------------
// Anisotropic model
struct AnisotropicModelParams
{
    float anisotropy;
    float3 albedo;
    float roughness;
    float metallic;
    float3 F0;
    float3 N; //L、H is related to the light，see XXXLightsLibrary.
    float3 V;
};

#endif