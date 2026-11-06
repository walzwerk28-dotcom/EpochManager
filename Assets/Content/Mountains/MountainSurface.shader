// Gebirge: Shader für das Relief. Die Farbe entsteht im Fragment-Shader aus Welthöhe, Steilheit (Normale) und Krümmung
// (Vertexfarbe r: 0,5 = eben, größer = Mulde, kleiner = Grat):
//  - unten dieselbe Wiesentextur wie der Boden (gleiche Welt-UV), damit der Hangfuß ohne Kante und Farbsprung übergeht,
//    darüber trockenes Gras und Ockererde,
//  - steile Flächen und große Höhen: grauer, leicht warmer Kalkstein mit feinen Gesteinsschichten (Bänder aus Höhe
//    plus Rauschen, nicht schnurgerade),
//  - Mulden dunkler und grüner, Grate heller, dezentes Rauschen auf allem.
// uv.x ist der Dunst (0 = klar, 1 = ganz im Horizontton) für das Gebirge außerhalb der Karte. Licht, Schatten und Nebel
// wie bei den übrigen Objekten. Für URP.
Shader "EpochCity/Gebirge"
{
    Properties
    {
        _GroundTex ("Wiesentextur des Bodens", 2D) = "white" {}
        _GroundRect ("Boden: Ursprung x, z und 1 / Größe x, z", Vector) = (0, 0, 0.0078125, 0.0078125)
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

            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GroundRect;
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
                half curvature : TEXCOORD2;
                half haze : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.curvature = input.color.r;
                output.haze = input.uv.x;
                output.fogFactor = ComputeFogFactor(p.positionCS.z);
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float3 S(float3 srgb) { return SRGBToLinear(srgb); }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                float2 xz = input.positionWS.xz;
                float h = input.positionWS.y;

                // Boden: dieselbe Textur und Welt-UV wie die Wiese
                float2 guv = (xz - _GroundRect.xy) * _GroundRect.zw;
                float3 ground = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, guv).rgb;

                float nl = VNoise(xz / 26.0 + 3.0);
                float n1 = VNoise(xz / 6.0 + 11.0);
                float n2 = VNoise(xz / 2.4 + 29.0);
                float n3 = VNoise(xz / 0.9 + 5.0);
                float steep = 1.0 - saturate(n.y);
                float curv = (input.curvature - 0.5) * 2.0;
                float valley = saturate(curv * 1.4);
                float ridge = saturate(-curv * 1.4);

                // Bewuchs: Gras (Boden) -> trockenes Gras -> Ockererde
                float3 scrub = S(float3(0.38, 0.47, 0.25));
                float3 dryGrass = S(float3(0.50, 0.52, 0.31));
                float3 earth = S(float3(0.54, 0.46, 0.32));
                float3 hollow = S(float3(0.20, 0.34, 0.17));
                float tScrub = smoothstep(0.8, 4.0, h + (n1 - 0.5) * 3.0 - valley * 1.0 + ridge * 0.6);
                float3 veg = lerp(ground, scrub, tScrub * 0.8);
                float tDry = smoothstep(4.0, 9.0, h + (nl - 0.5) * 4.0 + ridge * 1.0);
                veg = lerp(veg, dryGrass, tDry * 0.45 * (0.55 + 0.9 * n1));
                float tEarth = smoothstep(6.0, 11.0, h + (nl - 0.5) * 4.0 + steep * 5.0);
                veg = lerp(veg, earth, tEarth * 0.3 * (0.4 + 1.2 * n2));
                veg = lerp(veg, hollow, valley * 0.55 * (1.0 - smoothstep(7.0, 12.0, h)));

                // Kalkstein mit Gesteinsschichten: feine Fugen (Bänder aus Höhe plus Rauschen) und breitere Bänke
                float3 limestone = S(float3(0.50, 0.51, 0.54));
                float3 warm = S(float3(0.57, 0.55, 0.51));
                float3 rock = lerp(limestone, warm, smoothstep(0.25, 0.8, n1));
                float warp = VNoise(xz / 5.5 + 71.0) * 4.0 + VNoise(xz / 13.0 + 9.0) * 4.0 + n2 * 0.8;
                float s = h * 3.4 + warp;
                float band = frac(s);
                float seam = 1.0 - smoothstep(0.0, 0.10, band) * (1.0 - smoothstep(0.90, 1.0, band));
                float bank = Hash21(float2(floor(h * 0.9 + warp * 0.25), 7.0));
                rock *= lerp(1.0, 0.82, seam * 0.5) * (0.92 + 0.16 * bank);
                rock *= 1.0 + ridge * 0.14 - valley * 0.16;

                float rockT = smoothstep(0.38, 0.58, steep + (n2 - 0.5) * 0.12 + ridge * 0.06 - valley * 0.08);
                rockT = max(rockT, smoothstep(9.0, 14.0, h + (n1 - 0.5) * 3.0 + steep * 6.0) * 0.8);
                float3 albedo = lerp(veg, rock, saturate(rockT));

                // dezentes Rauschen, damit große Flächen nicht flach wirken
                albedo *= 1.0 + (n3 - 0.5) * 0.14 + (n2 - 0.5) * 0.10;

                // am Fuß exakt die Farbe des Bodens
                float w = smoothstep(0.05, 1.8, h);
                albedo = lerp(ground, albedo, w);
                albedo *= _Tint.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, light.direction));
                half3 ambient = SampleSH(n) * _Ambient;
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
