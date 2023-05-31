Shader "Custom/ObjectOutlineID"
{
    Properties
    {
        _OutlineID ("Outline ID", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _OutlineID;
            half4 _OutlineColor;
        CBUFFER_END
        
        struct appData
        {
            float4 position: POSITION;
        };

        struct v2f
        {
            float4 position: POSITION;
        };

        ENDHLSL

        Pass
        {
            Tags { "LightMode"="CustomOutlineID"}

            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appData i)
            {
                v2f o;
                o.position = TransformObjectToHClip(i.position.xyz);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return _OutlineID / 255;
            }

            ENDHLSL
        }
        
        Pass
        {
            Tags { "LightMode"="CustomOutlineColor"}

            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appData i)
            {
                v2f o;
                o.position = TransformObjectToHClip(i.position.xyz);
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
