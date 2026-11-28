// Edge Outline: draws lines where depth or surface direction changes, for
// a comic, technical or "ink on paper" look. Applied with URP's Full Screen
// Pass Renderer Feature with Depth, Normal and Color requirements.
// URP 17 (Unity 6). Scripts For Unity, MIT licensed.
Shader "Scripts For Unity/Full Screen/Edge Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.05, 0.05, 0.07, 1)
        _Thickness ("Thickness (pixels)", Range(0.5, 4)) = 1
        _DepthThreshold ("Depth Threshold", Range(0.005, 0.5)) = 0.08
        _NormalThreshold ("Normal Threshold", Range(0.05, 2)) = 0.4
        _FadeDistance ("Fade Out Distance", Range(1, 500)) = 60
        [Toggle(_OUTLINE_ONLY)] _OutlineOnly ("Outline Only (Paper)", Float) = 0
        _PaperColor ("Paper Color", Color) = (0.96, 0.94, 0.88, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Edge Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _OUTLINE_ONLY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _Thickness;
                float _DepthThreshold;
                float _NormalThreshold;
                float _FadeDistance;
                float _OutlineOnly;
                half4 _PaperColor;
            CBUFFER_END

            float SfuEyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = _Thickness / _ScreenParams.xy;

                // Roberts cross: compare the two diagonals around this pixel
                float2 a = uv + texel;
                float2 b = uv - texel;
                float2 c = uv + float2(texel.x, -texel.y);
                float2 d = uv + float2(-texel.x, texel.y);

                // Depth edges, relative to distance so far objects are not over-outlined
                float centerDepth = SfuEyeDepth(uv);
                float depthAB = SfuEyeDepth(a) - SfuEyeDepth(b);
                float depthCD = SfuEyeDepth(c) - SfuEyeDepth(d);
                float depthEdge = step(_DepthThreshold, sqrt(depthAB * depthAB + depthCD * depthCD) / max(centerDepth, 0.0001));

                // Normal edges: creases and corners where depth barely changes
                float3 normalAB = SampleSceneNormals(a) - SampleSceneNormals(b);
                float3 normalCD = SampleSceneNormals(c) - SampleSceneNormals(d);
                float normalEdge = step(_NormalThreshold, sqrt(dot(normalAB, normalAB) + dot(normalCD, normalCD)));

                float edge = max(depthEdge, normalEdge) * (1.0 - saturate(centerDepth / _FadeDistance));

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            #if defined(_OUTLINE_ONLY)
                color.rgb = _PaperColor.rgb;
            #endif
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return color;
            }
            ENDHLSL
        }
    }
}
