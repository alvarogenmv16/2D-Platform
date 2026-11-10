// Copy of URP's Sprite-Lit-Default (same 2D lighting) with a vertex sway for
// flames: the upper part of each sprite sways sideways and bobs, the base
// stays put - same motion as FlameSeaWaves on the rising sea's crest, but
// done per vertex so it works on whole Tilemaps (hundreds of tiles in one
// renderer) without touching any collider.
//
// How "height in the sprite" is known: from the texture V coordinate.
// _BaseV/_HeightV = bottom and height of the flame sprites inside their sheet
// (0..1). Defaults match fire0/fire1 in columns3.png (y 28px, ~441px tall,
// sheet 1024px tall). Sprites from another sheet need their own material
// with those two values adjusted.
Shader "Custom/2D/Sprite-Lit-FlameSway"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Flame Sway)]
        _SwayAmplitude("Sway Amplitude", Float) = 0.25
        _SwaySpeed("Sway Speed", Float) = 0.8
        _BobAmplitude("Bob Amplitude", Float) = 0.08
        _BobSpeed("Bob Speed", Float) = 2.3
        _PhaseScale("Phase Per World Unit", Float) = 0.35
        _BaseV("Flame Base (texture V)", Float) = 0.02734
        _HeightV("Flame Height (texture V)", Float) = 0.43066

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SwayAmplitude;
                float _SwaySpeed;
                float _BobAmplitude;
                float _BobSpeed;
                float _PhaseScale;
                float _BaseV;
                float _HeightV;
            CBUFFER_END

            // Base of the flame (weight 0) stays still, its tip (weight 1)
            // moves the most. Phase comes from world position, so each tile
            // flickers out of step with its neighbours.
            float3 FlameSway(float3 positionOS, float2 uv)
            {
                float weight = saturate((uv.y - _BaseV) / max(_HeightV, 0.0001));
                weight *= weight;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float phase = positionWS.x * _PhaseScale;
                float t = _Time.y;
                positionOS.x += sin(t * _SwaySpeed + phase) * _SwayAmplitude * weight;
                positionOS.y += sin(t * _BobSpeed + phase * 1.7) * _BobAmplitude * weight;
                return positionOS;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS = FlameSway(input.positionOS, input.uv);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                return CommonLitFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float _SwayAmplitude;
                float _SwaySpeed;
                float _BobAmplitude;
                float _BobSpeed;
                float _PhaseScale;
                float _BaseV;
                float _HeightV;
            CBUFFER_END

            // Base of the flame (weight 0) stays still, its tip (weight 1)
            // moves the most. Phase comes from world position, so each tile
            // flickers out of step with its neighbours.
            float3 FlameSway(float3 positionOS, float2 uv)
            {
                float weight = saturate((uv.y - _BaseV) / max(_HeightV, 0.0001));
                weight *= weight;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float phase = positionWS.x * _PhaseScale;
                float t = _Time.y;
                positionOS.x += sin(t * _SwaySpeed + phase) * _SwayAmplitude * weight;
                positionOS.y += sin(t * _BobSpeed + phase * 1.7) * _BobAmplitude * weight;
                return positionOS;
            }

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS = FlameSway(input.positionOS, input.uv);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                // Setup instancing for SpriteFlip is used in NormalsRenderingShared
                SetUpSpriteInstanceProperties();

                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SwayAmplitude;
                float _SwaySpeed;
                float _BobAmplitude;
                float _BobSpeed;
                float _PhaseScale;
                float _BaseV;
                float _HeightV;
            CBUFFER_END

            // Base of the flame (weight 0) stays still, its tip (weight 1)
            // moves the most. Phase comes from world position, so each tile
            // flickers out of step with its neighbours.
            float3 FlameSway(float3 positionOS, float2 uv)
            {
                float weight = saturate((uv.y - _BaseV) / max(_HeightV, 0.0001));
                weight *= weight;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float phase = positionWS.x * _PhaseScale;
                float t = _Time.y;
                positionOS.x += sin(t * _SwaySpeed + phase) * _SwayAmplitude * weight;
                positionOS.y += sin(t * _BobSpeed + phase * 1.7) * _BobAmplitude * weight;
                return positionOS;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS = FlameSway(input.positionOS, input.uv);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
