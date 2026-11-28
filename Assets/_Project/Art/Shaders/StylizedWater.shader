// Stylized Water: rolling vertex waves, a deep-to-horizon colour ramp,
// noise foam on the crests, a sun glint, and optional shore foam from the
// depth texture. Transparent. URP 17 (Unity 6).
// Scripts For Unity, MIT licensed.
// Patched: multi-frequency waves, two-layer warped foam, depth gradient,
// crest shading, soft shore foam Ч all with adjustable parameters.
Shader "Scripts For Unity/Stylized Water"
{
    Properties
    {
        [Header(Water Colour)]
        _DeepColor ("Deep Color", Color) = (0.05, 0.32, 0.5, 0.92)
        _HorizonColor ("Horizon Color", Color) = (0.35, 0.78, 0.86, 0.8)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3

        [Header(Depth Gradient)]
        [Toggle(_DEPTH_GRADIENT)] _DepthGradient ("Enable Depth Gradient (needs Depth Texture)", Float) = 0
        _ShallowColor ("Shallow Color", Color) = (0.08, 0.25, 0.28, 0.9)
        _DepthFadeDistance ("Depth Fade Distance (m)", Range(0.1, 10)) = 3

        [Header(Waves)]
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.12
        _WaveFrequency ("Wave Frequency", Range(0.1, 5)) = 1.2
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.1
        _CrestShadeStrength ("Crest Shade Strength", Range(0, 0.6)) = 0.25

        [Header(Crest Foam)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamScale ("Foam Scale", Range(0.1, 10)) = 6
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.25

        [Header(Sun Glint)]
        _GlintStrength ("Sun Glint", Range(0, 4)) = 1.2

        [Header(Shore Foam)]
        [Toggle(_SHORE_FOAM)] _ShoreFoam ("Shore Foam (needs Depth Texture)", Float) = 0
        _ShoreDistance ("Shore Foam Distance", Range(0.05, 3)) = 0.6
        _ShoreFoamAmount ("Shore Foam Amount", Range(0, 1)) = 0.5
        _ShoreSoftness ("Shore Foam Softness", Range(0.01, 1)) = 0.35
        _ShoreNoiseStrength ("Shore Foam Noise", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _SHORE_FOAM
            #pragma shader_feature_local _DEPTH_GRADIENT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _HorizonColor;
                float _FresnelPower;
                half4 _ShallowColor;
                float _DepthFadeDistance;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _CrestShadeStrength;
                half4 _FoamColor;
                float _FoamScale;
                float _FoamAmount;
                float _GlintStrength;
                float _ShoreFoam;
                float _ShoreDistance;
                float _ShoreFoamAmount;
                float _ShoreSoftness;
                float _ShoreNoiseStrength;
                float _DepthGradient;
            CBUFFER_END

            float SfuHash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float SfuValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float n000 = SfuHash13(i);
                float n100 = SfuHash13(i + float3(1, 0, 0));
                float n010 = SfuHash13(i + float3(0, 1, 0));
                float n110 = SfuHash13(i + float3(1, 1, 0));
                float n001 = SfuHash13(i + float3(0, 0, 1));
                float n101 = SfuHash13(i + float3(1, 0, 1));
                float n011 = SfuHash13(i + float3(0, 1, 1));
                float n111 = SfuHash13(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                            lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
            }

            float SfuFractalNoise(float3 p)
            {
                float n = SfuValueNoise(p) * 0.5;
                n += SfuValueNoise(p * 2.03) * 0.25;
                n += SfuValueNoise(p * 4.01) * 0.125;
                n += SfuValueNoise(p * 8.05) * 0.0625;
                return n / 0.9375;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float wave : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                float t = _Time.y * _WaveSpeed;
                float f = _WaveFrequency;

                // Wave 1: main direction
                float a1x = positionWS.x * f + t;
                float a1z = positionWS.z * f * 0.8 + t * 0.9;
                float w1 = sin(a1x) * cos(a1z);

                // Wave 2: diagonal
                float a2 = (positionWS.x * 1.73 + positionWS.z * 0.61) * f + t * 1.31;
                float w2 = sin(a2) * 0.5;

                // Wave 3: counter-diagonal
                float a3 = (positionWS.x * 2.31 - positionWS.z * 1.13) * f + t * 0.71;
                float w3 = sin(a3) * 0.35;

                float wave = (w1 + w2 + w3) / 1.85;

                float dW1x = cos(a1x) * cos(a1z) * f;
                float dW1z = -sin(a1x) * sin(a1z) * f * 0.8;
                float dW2x = cos(a2) * 1.73 * f * 0.5;
                float dW2z = cos(a2) * 0.61 * f * 0.5;
                float dW3x = cos(a3) * 2.31 * f * 0.35;
                float dW3z = -cos(a3) * 1.13 * f * 0.35;

                float slopeX = (dW1x + dW2x + dW3x) / 1.85 * _WaveHeight;
                float slopeZ = (dW1z + dW2z + dW3z) / 1.85 * _WaveHeight;

                positionWS.y += wave * _WaveHeight;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalize(float3(-slopeX, 1.0, -slopeZ));
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.wave = wave;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // --- Scene depth (used by depth gradient and shore foam) ---
            #if defined(_SHORE_FOAM) || defined(_DEPTH_GRADIENT)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float waterDepth = max(0.0, sceneEyeDepth - input.screenPos.w);
            #endif

                // --- Base colour: depth gradient (optional) + fresnel ---
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

            #if defined(_DEPTH_GRADIENT)
                float depthFade = saturate(waterDepth / max(_DepthFadeDistance, 0.01));
                half4 baseWater = lerp(_ShallowColor, _DeepColor, depthFade);
            #else
                half4 baseWater = _DeepColor;
            #endif

                half4 water = lerp(baseWater, _HorizonColor, fresnel);

                // --- Crest shading: darken troughs, brighten crests ---
                water.rgb *= 1.0 + input.wave * _CrestShadeStrength;

                // --- Crest foam: two warped noise layers ---
                float2 drift1 = float2( 1.0, 0.6) * _Time.y * 0.15;
                float2 drift2 = float2(-0.7, 0.9) * _Time.y * 0.11;

                float3 p1 = float3(input.positionWS.xz * _FoamScale        + drift1, _Time.y * 0.10);
                float3 p2 = float3(input.positionWS.xz * _FoamScale * 2.71 + drift2, _Time.y * 0.07);

                float n1 = SfuFractalNoise(p1);
                float n2 = SfuFractalNoise(p2 + n1 * 1.7);
                float noise = n1 * 0.55 + n2 * 0.45;

                float crest = saturate(input.wave * 0.5 + 0.5);
                crest = lerp(crest, noise, 0.25);

                float foamMask = noise * lerp(0.6, 1.25, crest);
                float foam = smoothstep(1.0 - _FoamAmount, 1.02 - _FoamAmount, foamMask);

                // --- Soft shore foam with noise, replacing the hard step ---
            #if defined(_SHORE_FOAM)
                float shore = 1.0 - saturate((sceneEyeDepth - input.screenPos.w) / _ShoreDistance);
                float shoreNoise = SfuFractalNoise(float3(input.positionWS.xz * 3.0 + drift1, _Time.y * 0.1));
                float shoreMask = shore * (0.6 + shoreNoise * _ShoreNoiseStrength);
                float shoreEdge = smoothstep(
                    1.0 - _ShoreFoamAmount - _ShoreSoftness,
                    1.0 - _ShoreFoamAmount + _ShoreSoftness,
                    shoreMask);
                foam = max(foam, shoreEdge);
            #endif

                // --- Sun glint ---
                Light mainLight = GetMainLight();
                float3 halfDirWS = normalize(mainLight.direction + viewDirWS);
                float glintBase = pow(saturate(dot(normalWS, halfDirWS)), 64.0);
                // Ѕолее м€гкий порог (0.80 - 0.98 вместо 0.92 - 0.95) + разбиваем шумом
                float glint = smoothstep(0.80, 0.98, glintBase) * noise * _GlintStrength * 2.0; 

                float3 color = lerp(water.rgb, _FoamColor.rgb, foam) + glint * mainLight.color;
                float alpha = saturate(max(water.a, foam * _FoamColor.a) + glint);
                return half4(MixFog(color, input.fogFactor), alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}