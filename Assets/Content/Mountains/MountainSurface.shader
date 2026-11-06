// Gebirge: schlichter Shader für das Relief (Low-Poly mit Farben statt Texturen). Die Farbe steckt in den
// Vertexfarben (nach Höhe und Steilheit gesetzt), die Normalen sind je Dreieck flach. uv.x ist der Dunst
// (0 = klar, 1 = ganz im Horizontton) für das Gebirge außerhalb der Karte. Licht, Schatten empfangen und werfen
// wie die übrigen Objekte; Nebel der Szene wird angewendet. Für URP.
Shader "EpochCity/Gebirge"
{
    Properties
    {
        _HazeColor ("Dunst", Color) = (0.74, 0.81, 0.86, 1)
        _Tint ("Tönung", Color) = (1, 1, 1, 1)
        _Ambient ("Umgebungslicht-Anteil", Range(0, 1.5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HazeColor;
                half4 _Tint;
                half _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half haze : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.haze = input.uv.x;
                output.fogFactor = ComputeFogFactor(p.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, light.direction));
                half3 ambient = SampleSH(n) * _Ambient;
                half3 albedo = input.color.rgb * _Tint.rgb;
                half3 lit = albedo * (light.color * ndl * light.shadowAttenuation + ambient);
                lit = lerp(lit, _HazeColor.rgb, saturate(input.haze));
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
