Shader "Custom/ObjectOutlines"
{
    Properties
    {
        _OutlineRadius ("Outline Radius", Integer) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}

        Pass
        {
            ZWrite On
            
            HLSLPROGRAM
            
            #pragma exclude_renderers d3d11_9x
            #pragma exclude_renderers d3d9
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineRadius;
            CBUFFER_END

            struct appData
            {
                float4 position: POSITION;
                float2 uv: TEXCOORD0;
            };

            struct v2f
            {
                float4 position: POSITION;
                float2 uv: TEXCOORD0;
            };

            TEXTURE2D(_OutlineId);
            SAMPLER(sampler_OutlineId);
            float4 _OutlineId_TexelSize;

            TEXTURE2D(_OutlineColors);
            SAMPLER(sampler_OutlineColors);
            
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            v2f vert(appData i)
            {
                v2f o;
                o.position = TransformObjectToHClip(i.position.xyz);
                o.uv = i.uv;
                return o;
            }

            float4 GetOutline(float id, float depth, int radius, float2 uv, out float fragDepth)
            {
                if(radius <= 0)
                    return 0;
                
                fragDepth = depth;
                
                [loop] //For performance reasons, the compiler tries "unroll" loops, but sometimes dynamic loops cannot be unrolled, so we need to explicitly tell the compiler to treat the loop as an actual loop
                for (int i = 1; i <= radius; i++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if(x == 0 && y == 0)
                                continue;

                            float2 otherUV = uv + (float2(x, y) * radius * _OutlineId_TexelSize);
                            float otherID = SAMPLE_TEXTURE2D(_OutlineId, sampler_OutlineId, otherUV).r;

                            if(otherID == id)
                                continue;

                            float otherDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, otherUV);

                            if(otherDepth > depth)
                            {
                                fragDepth = otherDepth;
                                return SAMPLE_TEXTURE2D(_OutlineColors, sampler_OutlineColors, otherUV);
                            }
                        }
                    }
                }
                
                return 0;
            }

            float4 frag(v2f i, out float depth : SV_Depth) : SV_Target
            {
                float sourceID = SAMPLE_TEXTURE2D(_OutlineId, sampler_OutlineId, i.uv).r;
                float sourceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);

                float4 finalColor = GetOutline(sourceID, sourceDepth, _OutlineRadius, i.uv, depth);
                return finalColor;
            }

            ENDHLSL
        }
    }
}
