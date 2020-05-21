#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

//The name of the function must the EXACTLY the same as the name of the function. The function MUST have a precision suffix (the "_float"), but it MUST NOT be in the name of the function on the custom function node. (In this case, the node will call a function named "MyFunction" instead of "MyFunction_float")
void MyFunction_float(float A, float B, out float Out1, out float Out2) {
	Out1 = 0;
	Out2 = 1;
}

void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten){
    //Check if we're in preview mode (inside shader graph)
    #if SHADERGRAPH_PREVIEW
        //Hardcoded data, used for the preview shader inside the graph where light functions are not available
        Direction = half3(0.5, 0.5, 0);
        Color = 1;
        DistanceAtten = 1;
        ShadowAtten = 1;
    #else
        //Actual light data from the pipeline
        #if SHADOWS_SCREEN
            half4 clipPos = TransformWorldToHClip(WorldPos);
            half4 shadowCoord = ComputeScreenPos(clipPos);
        #else
            half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        #endif

        Light mainLight = GetMainLight(shadowCoord);
        Direction = mainLight.direction;
        Color = mainLight.color;
        DistanceAtten = mainLight.distanceAttenuation;
        ShadowAtten = mainLight.shadowAttenuation;
    #endif
}

void AdditionalLights_half(half3 SpecColor, half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView, out half3 Diffuse, out half3 Specular)
{
    half3 diffuseColor = 0;
    half3 specularColor = 0;

    #ifndef SHADERGRAPH_PREVIEW
        Smoothness = exp2(10 * Smoothness + 1);
        WorldNormal = normalize(WorldNormal);
        WorldView = SafeNormalize(WorldView);
        int pixelLightCount = GetAdditionalLightsCount();

        for (int i = 0; i < pixelLightCount; ++i)
        {
            Light light = GetAdditionalLight(i, WorldPosition);
            half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
            diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
            specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
        }
    #endif

    Diffuse = diffuseColor;
    Specular = specularColor;
}

#endif