Shader "Custom/CustomShadows_2"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Toggle] _UseStep("UseStep", Float) = 1
        _Step("Step", Range(0.0, 1.0)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // _ADDITIONAL_LIGHTS_VERTEX affects the vertex stage, so this stays as multi_compile.
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // Everything below is fragment-only; multi_compile_fragment avoids extra vertex variants.
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _UseStep;
                float _Step;
            CBUFFER_END

            float CalculateShading(float3 normalWS, float3 lightDirection)
            {
                // Raw Lambert
                return saturate(dot(normalWS, lightDirection));
            }

            // Returns the accumulated weighted light contribution (color × diffuse × attenuation).
            // Because each term is multiplied by the light's color, disabled/out-of-range lights
            // contribute nothing — their color is black or their attenuation is zero.
            float3 CalculateLighting(float3 normalWS, float3 positionWS, float4 positionHCS)
            {
                // Convert world position to shadow map UV and sample the main light.
                // shadowAttenuation is 0 in shadow, 1 in full light.
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightColor = mainLight.color * CalculateShading(normalWS, mainLight.direction) * mainLight.shadowAttenuation;

                #ifdef _ADDITIONAL_LIGHTS
                    // InputData must be a local named exactly 'inputData' — LIGHT_LOOP_BEGIN reads
                    // it directly to locate the correct tile/cluster in Forward+.
                    InputData inputData = (InputData)0;
                    inputData.positionWS = positionWS;
                    inputData.normalWS = normalWS;
                    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionHCS);

                    // Forward+ stores extra directional lights (beyond the main one) at the
                    // front of the light buffer. The cluster iterator skips them, so they
                    // need their own loop indexed from 0..URP_FP_DIRECTIONAL_LIGHTS_COUNT.
                    #if USE_CLUSTER_LIGHT_LOOP
                        UNITY_LOOP for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                        {
                            Light light = GetAdditionalLight(dirIndex, positionWS, half4(1, 1, 1, 1));
                            lightColor += light.color * CalculateShading(normalWS, light.direction) * light.distanceAttenuation * light.shadowAttenuation;
                        }
                    #endif

                    // In Forward, iterates 0..GetAdditionalLightsCount().
                    // In Forward+, LIGHT_LOOP_BEGIN ignores pixelLightCount and uses the
                    // cluster tile data from inputData to visit only nearby point/spot lights.
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, positionWS, half4(1, 1, 1, 1));
                        // distanceAttenuation handles 1/d² falloff and range cutoff.
                        // Directional lights always return 1 here; point/spot lights vary.
                        lightColor += light.color * CalculateShading(normalWS, light.direction) * light.distanceAttenuation * light.shadowAttenuation;
                    LIGHT_LOOP_END
                #endif

                return lightColor;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 lightColor = CalculateLighting(normal, IN.positionWS, IN.positionHCS);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                if (_UseStep)
                    color.rgb *= step(_Step, lightColor);
                else
                    color.rgb *= lightColor;
                
                return color;
            }
            ENDHLSL
        }
        
        // Writes this object's depth into the shadow map so other objects can receive its shadow.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0  // depth-only pass; no colour output needed

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            // Compiles a second variant for point/spot lights, which need per-vertex light direction.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"  // ApplyShadowBias

            // Set by URP before the pass runs.
            // _LightDirection is used for directional lights; _LightPosition for point/spot.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Direction toward the light, computed per-vertex.
                // Point/spot lights diverge from a position; directional lights are parallel.
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                // ApplyShadowBias nudges the position slightly along the normal and away
                // from the light to prevent shadow acne (self-shadowing artefacts).
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                // Clamp depth to the near clip plane so geometry behind the light doesn't
                // write incorrect values into the shadow map. The comparison is reversed on
                // platforms that flip the depth buffer (e.g. Metal, Vulkan).
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            // No colour output; the pipeline only cares about the depth written by the vertex stage.
            half4 ShadowPassFragment(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}