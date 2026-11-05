// Wasserfläche: halbtransparent, Farbe nach Abstand zum Ufer (uv.x: 0 am Ufer, 1 tief), dezent bewegte Wellen aus
// Rauschen im Shader (keine Textur), Glanzlicht der Sonne, Schaum am Ufer. Für URP, ohne Schatten.
Shader "EpochCity/Wasser"
{
    Properties
    {
        _ShallowColor ("Flaches Wasser", Color) = (0.22, 0.56, 0.58, 1)
        _DeepColor ("Tiefes Wasser", Color) = (0.07, 0.34, 0.46, 1)
        _FoamColor ("Schaum", Color) = (0.92, 0.97, 0.95, 1)
        _SkyColor ("Himmelsspiegelung", Color) = (0.62, 0.80, 0.92, 1)
        _ShallowAlpha ("Deckkraft flach", Range(0, 1)) = 0.78
        _DeepAlpha ("Deckkraft tief", Range(0, 1)) = 0.94
        _FoamWidth ("Schaumbreite", Range(0.01, 0.6)) = 0.10
        _WaveScale ("Wellengröße (1 / m)", Float) = 1.1
        _WaveSpeed ("Wellentempo", Float) = 0.18
        _WaveStrength ("Wellenstärke", Float) = 0.3
        _Glossiness ("Glanz", Float) = 220
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half4 _SkyColor;
                half _ShallowAlpha;
                half _DeepAlpha;
                half _FoamWidth;
                float _WaveScale;
                float _WaveSpeed;
                half _WaveStrength;
                half _Glossiness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float depth : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.depth = input.uv.x;
                output.fogFactor = ComputeFogFactor(p.positionCS.z);
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintisch, damit die Rasterzellen nicht durchscheinen
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Höhenfeld der Wellen: zwei Lagen, die in verschiedene Richtungen wandern
            float WaveHeight(float2 p, float t)
            {
                float h = ValueNoise(p * _WaveScale + float2(t, t * 0.6));
                h += 0.6 * ValueNoise(p * _WaveScale * 2.3 + float2(-t * 0.9, t * 0.4));
                return h;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 p = input.positionWS.xz;
                float e = 0.12;
                float h = WaveHeight(p, t);
                float hx = WaveHeight(p + float2(e, 0), t);
                float hz = WaveHeight(p + float2(0, e), t);
                half3 n = normalize(half3(-(hx - h) / e * _WaveStrength, 1.0, -(hz - h) / e * _WaveStrength));

                Light light = GetMainLight();
                half3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = normalize(light.direction + v);
                half spec = pow(saturate(dot(n, halfDir)), _Glossiness) * 0.35;
                half fresnel = pow(1.0 - saturate(dot(n, v)), 3.0);

                half depth = saturate(input.depth);
                half3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, smoothstep(0.0, 1.0, depth));
                // leichte Helligkeitsschwankung durch die Wellen, damit die Fläche lebt
                baseColor *= 0.92 + 0.16 * saturate(h * 0.6);
                half ndl = saturate(dot(n, light.direction));
                baseColor *= 0.72 + 0.28 * ndl * light.color.r;
                baseColor = lerp(baseColor, _SkyColor.rgb, fresnel * 0.3);
                baseColor += light.color * spec;

                // Schaum am Ufer: schmaler Streifen mit unruhiger Kante
                half foamNoise = ValueNoise(p * 3.1 + float2(t * 2.0, -t));
                half foam = 1.0 - smoothstep(0.0, _FoamWidth * (0.6 + 0.8 * foamNoise), depth);
                baseColor = lerp(baseColor, _FoamColor.rgb, foam * 0.7);

                half alpha = lerp(_ShallowAlpha, _DeepAlpha, smoothstep(0.0, 1.0, depth));
                alpha = saturate(alpha + foam * 0.25 + spec * 0.5);
                baseColor = MixFog(baseColor, input.fogFactor);
                return half4(baseColor, alpha);
            }
            ENDHLSL
        }
    }
}
