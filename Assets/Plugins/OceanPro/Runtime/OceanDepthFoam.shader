Shader "OceanPro/Depth Foam Water"
{
    Properties
    {
        _ShallowColor ("Shallow Water", Color) = (0.04, 0.44, 0.60, 1)
        _DeepColor ("Deep Water", Color) = (0.005, 0.035, 0.12, 1)
        _FoamColor ("Foam", Color) = (0.82, 0.94, 1, 1)
        _WaveHeight ("Wave Height", Float) = 1.8
        _WaveSpeed ("Wave Speed", Float) = 1
        _Steepness ("Wave Steepness", Float) = 0.62
        _DepthDensity ("Depth Darkening", Float) = 0.045
        _Opacity ("Water Opacity", Range(0.1, 1)) = 0.82
        _FresnelStrength ("Fresnel Highlight", Range(0, 1)) = 0.3
        _FoamDepth ("Intersection Foam Depth", Float) = 0.7
        _CrestFoamStrength ("Crest Foam", Range(0, 2)) = 0.8
        _IntersectionFoamStrength ("Object/shore Foam", Range(0, 2)) = 1.1
        _FoamScale ("Foam Pattern Scale", Float) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            fixed4 _FoamColor;
            float4 _WaveDirections[4];
            float4 _WaveSettings[4];
            float3 _FieldCenter;
            float4 _AmplitudeField;
            float _WaveHeight;
            float _WaveSpeed;
            float _Steepness;
            float _DepthDensity;
            float _Opacity;
            float _FresnelStrength;
            float _FoamDepth;
            float _CrestFoamStrength;
            float _IntersectionFoamStrength;
            float _FoamScale;
            float4 _WakeData[4];
            float4 _WakeMotion[4];
            float _WakeCount;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
                float waveDisplacement : TEXCOORD4;
                float3 baseWorldPosition : TEXCOORD5;
            };

            void AddWave(inout float3 p, inout float3 tangentX, inout float3 tangentZ,
                         float3 basePosition, float2 direction, float wavelength,
                         float amplitude, float phaseOffset, float speed)
            {
                direction = normalize(direction);
                float k = 6.2831853 / wavelength;
                float angularSpeed = sqrt(9.81 * k) * _WaveSpeed * speed;
                float phase = k * dot(direction, basePosition.xz) - _Time.y * angularSpeed + phaseOffset;
                float s = sin(phase);
                float c = cos(phase);
                float horizontal = _Steepness * amplitude;

                p.x += direction.x * horizontal * c;
                p.z += direction.y * horizontal * c;
                p.y += amplitude * s;

                tangentX += float3(-_Steepness * amplitude * k * direction.x * direction.x * s,
                                    amplitude * k * direction.x * c,
                                   -_Steepness * amplitude * k * direction.x * direction.y * s);
                tangentZ += float3(-_Steepness * amplitude * k * direction.x * direction.y * s,
                                    amplitude * k * direction.y * c,
                                   -_Steepness * amplitude * k * direction.y * direction.y * s);
            }

            float AmplitudeScale(float2 worldXZ)
            {
                float t = saturate((distance(worldXZ, _FieldCenter.xz) - _AmplitudeField.x)
                    / max(0.01, _AmplitudeField.y - _AmplitudeField.x));
                return lerp(_AmplitudeField.z, _AmplitudeField.w, t * t * (3.0 - 2.0 * t));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 basePosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 p = basePosition;
                float3 tangentX = float3(1, 0, 0);
                float3 tangentZ = float3(0, 0, 1);

                float scale = _WaveHeight * AmplitudeScale(basePosition.xz);
                [unroll]
                for (int waveIndex = 0; waveIndex < 4; waveIndex++)
                {
                    float4 wave = _WaveDirections[waveIndex];
                    float4 settings = _WaveSettings[waveIndex];
                    AddWave(p, tangentX, tangentZ, basePosition, wave.xy, wave.z,
                        scale * wave.w, settings.y, settings.x);
                }

                float3 viewPosition = mul(UNITY_MATRIX_V, float4(p, 1)).xyz;
                o.position = mul(UNITY_MATRIX_P, float4(viewPosition, 1));
                o.screenPosition = ComputeScreenPos(o.position);
                o.worldPosition = p;
                o.worldNormal = normalize(cross(tangentZ, tangentX));
                o.baseWorldPosition = basePosition;
                o.eyeDepth = -viewPosition.z;
                o.waveDisplacement = p.y - basePosition.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPosition));
                float sceneEyeDepth = LinearEyeDepth(rawDepth);
                float thickness = max(0.0, sceneEyeDepth - i.eyeDepth);
                float depthFactor = 1.0 - exp(-thickness * _DepthDensity);
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(depthFactor));

                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPosition);
                float fresnel = pow(1.0 - saturate(abs(dot(N, V))), 3.0) * _FresnelStrength;
                waterColor = lerp(waterColor, _ShallowColor.rgb * 0.45 + 0.25, fresnel);

                float2 foamUV = i.worldPosition.xz * max(_FoamScale, 0.001);
                float n1 = sin(dot(foamUV, float2(1.7, 2.3)) + _Time.y * 0.22);
                float n2 = sin(dot(foamUV, float2(-2.1, 1.4)) - _Time.y * 0.17);
                float n3 = sin(dot(foamUV, float2(4.2, 3.1)) + _Time.y * 0.31);
                float foamNoise = saturate(0.5 + 0.25 * n1 + 0.18 * n2 + 0.12 * n3);
                float crest = saturate((i.waveDisplacement / max(_WaveHeight, 0.001) - 0.34) * 3.0);
                float crestFoam = smoothstep(0.48, 0.78, crest) * _CrestFoamStrength;
                float intersection = 1.0 - smoothstep(0.0, max(_FoamDepth, 0.001), thickness);
                float wakeFoam = 0.0;
                [unroll]
                for (int wakeIndex = 0; wakeIndex < 4; wakeIndex++)
                {
                    if (wakeIndex < _WakeCount)
                    {
                        float4 wake = _WakeData[wakeIndex];
                        float4 motion = _WakeMotion[wakeIndex];
                        float2 delta = i.worldPosition.xz - wake.xy;
                        float2 direction = normalize(motion.xy + float2(0.0001, 0.0001));
                        float behind = -dot(delta, direction);
                        float lateral = delta.x * direction.y - delta.y * direction.x;
                        float radius = max(wake.z, 0.2);
                        float speedFactor = saturate(motion.z * 0.45);
                        float trail = exp(-(lateral * lateral) / (radius * radius * 0.32))
                                    * exp(-max(behind, 0.0) / (radius * 3.5))
                                    * smoothstep(0.1, 0.65, behind) * speedFactor;
                        float distanceToObject = length(delta);
                        float ring = (0.5 + 0.5 * sin(distanceToObject * 3.4 - _Time.y * (1.5 + motion.z)))
                                   * exp(-distanceToObject / (radius * 2.2)) * speedFactor * 0.25;
                        wakeFoam = max(wakeFoam, (trail * 0.85 + ring) * wake.w);
                    }
                }
                float foamMask = max(max(crestFoam, intersection * _IntersectionFoamStrength), wakeFoam);
                float brokenFoam = smoothstep(0.30, 0.72, foamMask * foamNoise);

                waterColor = lerp(waterColor, _FoamColor.rgb, saturate(brokenFoam));
                float alpha = saturate(_Opacity * (0.66 + 0.34 * depthFactor) + brokenFoam * 0.16);
                return fixed4(waterColor, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
