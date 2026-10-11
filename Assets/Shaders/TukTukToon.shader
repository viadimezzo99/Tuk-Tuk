Shader "TukTuk/Toon"
{
    // Shader cartoon per URP: texture + luce a due fasce + contorno nero + nebbia.
    Properties
    {
        _BaseColor ("Colore", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Colore contorno", Color) = (0.1,0.07,0.05,1)
        _OutlineWidth ("Spessore contorno", Float) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Passaggio 1: contorno, facce posteriori gonfiate lungo le normali.
        Pass
        {
            Name "Outline"
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float fogFactor : TEXCOORD0;
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                float3 c = MixFog(_OutlineColor.rgb, IN.fogFactor);
                return half4(c, _OutlineColor.a);
            }
            ENDHLSL
        }

        // Passaggio 2: colore + texture con ombreggiatura a fasce, sfumata dalla nebbia.
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings ToonVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 ToonFrag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                float ndl = saturate(dot(n, mainLight.direction));

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 albedo = _BaseColor.rgb * tex.rgb;

                // Due fasce nette invece della sfumatura realistica.
                float band = ndl > 0.5 ? 1.0 : 0.6;
                float3 color = albedo * mainLight.color * band + albedo * 0.25;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
