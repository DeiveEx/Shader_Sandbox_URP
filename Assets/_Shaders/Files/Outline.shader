Shader "Test/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1"}
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
            float _OutlineWidth;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct appData
        {
            float4 position: POSITION;
            float3 normal: NORMAL;
            float2 uv: TEXCOORD0;
        };

        struct v2f
        {
            float4 position: POSITION;
            float2 uv: TEXCOORD0;
        };

        ENDHLSL
        
        Pass
        {
            ZWrite On
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            v2f vert(appData i)
            {
                v2f o;
                float3 outlinePos = i.position.xyz + (-i.normal * - _OutlineWidth);
                float3 worldPos = TransformObjectToWorld(outlinePos);
                float3 viewPos = TransformWorldToView(worldPos);
                
                float3 objPivot = float3(0, 0, 0);
                float3 worldPivot = TransformObjectToWorld(objPivot);
                float3 viewPivot = TransformWorldToView(worldPivot);
                
                //Override the depth of the vertex to be the same as the pivot, which will basically sort the object as a 2D plane
                viewPos.z = (viewPos.z * 0.01) + viewPivot.z;
                
                o.position = TransformWViewToHClip(viewPos);
                // o.position = TransformObjectToHClip(outlinePos);
                
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 finalColor = mainTex * _BaseColor;

                return finalColor;
            }

            ENDHLSL
        }
    }
}
