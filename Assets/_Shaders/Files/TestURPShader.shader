Shader "Test/TestURPShader"
{
    //We still use Shaderlab for the SRP (Scriptable Render Pipeline)
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        //We can define which SRP this shader is for in the tags (we don't need to, though)
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline"}
        LOD 100

        //For the new SRP, we now use HLSL instead of CG
        //We can use a HLSLINCLUDE block to define things that needs to be available to all passes, instead of rewritting it for each pass (we can also declare these inside the "HLSLPROGRAM" block inside each pass, if we want)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" //Imports the standart URL shader library

        //To make sure that the shader is compatible with the SRP Batcher (which is basically a way to render things faster), we need to declare the properties inside a CBUFFER block with the name "UnityPerMaterial"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
        CBUFFER_END

        //Textures are special, don't need to be declared inside the CBuffer
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex); //we need to declare a sampler for each texture as well. Just put "sampler_" in front of the texture name

        //Here we declare the structs used to pass data between each shader (vertex, fragment, etc.)
        struct appData
        {
            float4 position: POSITION;
            float2 uv: TEXCOORD0;
        }; //Don't forget the semicolon

        struct v2f
        {
            float4 position: POSITION;
            float2 uv: TEXCOORD0;
        };

        ENDHLSL

        //Remember that a pass is a single render of object. Multiple passes means multiples renders. We can use an aditional pass do render an outline effect using the ivnerted hull method, for example.
        Pass
        {
            HLSLPROGRAM
            //Kust like before, here we define which methods are gonna beexecuted in the Vertex and Frament stages
            #pragma vertex vert
            #pragma fragment frag

            //"i" for "input" and "o" for "output" (arbitrary names)
            v2f vert(appData i)
            {
                v2f o;
                o.position = TransformObjectToHClip(i.position.xyz); //New function to transform vertex position from local pos to clip pos
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
