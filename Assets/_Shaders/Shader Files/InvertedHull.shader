Shader "Custom/Inverted Hull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 0.5
        _NormalOrPos ("Normal Or Position", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineWidth;
            float _NormalOrPos;
        CBUFFER_END

        struct appData
        {
            float4 position: POSITION;
            float3 normal: NORMAL;
        };

        struct v2f
        {
            float4 position: POSITION;
        };

        ENDHLSL

        Pass
        {
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appData i)
            {
                v2f o;

                float3 withNormal = i.position.xyz + (-i.normal * - _OutlineWidth);
                float3 withPos = i.position.xyz + (-i.position.xyz * - _OutlineWidth);
                float3 newPos = lerp(withNormal, withPos, _NormalOrPos);
                o.position = TransformObjectToHClip(newPos);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}
