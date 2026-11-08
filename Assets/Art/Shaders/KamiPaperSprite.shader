// Kami's sprite shader: flat paper-cut look with grain, plus the crease / tear / ink-stain effects
// that show Kami's state. Unlit (the art is flat), works with SpriteRenderer, flipX/flipY, tint,
// sorting layers and the 2D Renderer.
//
// Materials: M_Paper (world), M_PaperMid / M_PaperFar (faded background layers), M_Kami (the hero).
// Drive _Creases, _Tear, _Stain and _Flash per renderer with the PaperLook component.
// Crease and tear effects are laid out in the sprite's own UVs, so keep Kami's sprites out of
// Sprite Atlases (the importer gives them Full Rect meshes and padding for the tear).
Shader "Kami/Paper Sprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Paper)]
        _GrainStrength ("Grain Strength", Range(0, 0.3)) = 0.07
        _GrainSize ("Grain Size (px at 128 PPU)", Range(0.5, 12)) = 3
        [ToggleUI] _GrainWorld ("Grain In World Space (off for moving sprites)", Float) = 1
        _FadeColor ("Distance Fade Colour", Color) = (0.957, 0.925, 0.867, 1)
        _Fade ("Distance Fade", Range(0, 1)) = 0

        [Header(Creases)]
        _Creases ("Creases", Range(0, 12)) = 0
        _CreaseLimit ("Crease Limit", Range(1, 12)) = 8
        _CreaseColor ("Crease Colour", Color) = (0.55, 0.48, 0.40, 1)
        _CreaseWarnColor ("Crease Colour Near Limit", Color) = (0.64, 0.18, 0.18, 1)
        _CreaseWidth ("Crease Width (px)", Range(0.5, 4)) = 1.8

        [Header(Effects)]
        _Tear ("Tear", Range(0, 1)) = 0
        _Stain ("Ink Stain", Range(0, 1)) = 0
        _StainColor ("Ink Colour", Color) = (0.173, 0.173, 0.165, 1)
        _FlashColor ("Flash Colour", Color) = (1,1,1,1)
        _Flash ("Flash", Range(0, 1)) = 0

        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        // Kept so the material falls back gracefully to the legacy sprite shader.
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "KamiCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _GrainStrength;
                half _GrainSize;
                half _GrainWorld;
                half4 _FadeColor;
                half _Fade;
                half _Creases;
                half _CreaseLimit;
                half4 _CreaseColor;
                half4 _CreaseWarnColor;
                half _CreaseWidth;
                half _Tear;
                half _Stain;
                half4 _StainColor;
                half4 _FlashColor;
                half _Flash;
            CBUFFER_END

            // One line per fold: centre (uv), angle (radians), dash phase. Revealed in order.
            static const float4 kCreases[12] =
            {
                float4(0.50, 0.55,  0.79, 0.00),
                float4(0.42, 0.40, -0.62, 0.30),
                float4(0.60, 0.68,  2.20, 0.55),
                float4(0.48, 0.30,  0.15, 0.10),
                float4(0.55, 0.48,  1.75, 0.70),
                float4(0.38, 0.62, -1.20, 0.40),
                float4(0.62, 0.36,  0.95, 0.85),
                float4(0.50, 0.80, -0.35, 0.20),
                float4(0.45, 0.52,  2.60, 0.65),
                float4(0.58, 0.58, -2.00, 0.05),
                float4(0.40, 0.25,  1.30, 0.45),
                float4(0.52, 0.45,  0.40, 0.90),
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            float4 SampleSprite(float2 uv)
            {
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * inside;
            }

            // Jagged vertical tear line, in uv.
            float TearSeam(float y)
            {
                return 0.5 + (KamiValueNoise(float2(y * 10.0, 4.2)) - 0.5) * 0.14 + sin(y * 95.0) * 0.012;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texSize = _MainTex_TexelSize.zw;

                // Tear: split along the seam, left piece drifts left/up, right piece right/down.
                float tearOn = saturate(_Tear * 40.0);
                float2 uvL = uv - float2(-0.10, 0.04) * _Tear;
                float2 uvR = uv - float2(0.10, -0.03) * _Tear;
                float dL = TearSeam(uvL.y) - uvL.x;
                float dR = uvR.x - TearSeam(uvR.y);
                float4 cL = SampleSprite(uvL) * step(0.0, dL);
                float4 cR = SampleSprite(uvR) * step(0.0, dR);
                float4 col = cL.a >= cR.a ? cL : cR;
                float2 pieceUV = cL.a >= cR.a ? uvL : uvR;
                float seamDist = cL.a >= cR.a ? dL : dR;

                col *= i.color;
                float2 px = pieceUV * texSize;

                // Only paper (light) areas take creases and grain; ink outlines and the mask stay clean.
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float paper = smoothstep(0.55, 0.8, lum);

                // Creases: dashed fold lines, one per fold, red when within 2 of the limit.
                float warn = step(_CreaseLimit - 2.0, _Creases);
                float3 creaseCol = lerp(_CreaseColor.rgb, _CreaseWarnColor.rgb, warn);
                float creaseMask = 0.0;
                float highlight = 0.0;
                [unroll]
                for (int k = 0; k < 12; k++)
                {
                    float on = step(k + 0.5, _Creases);
                    float4 c = kCreases[k];
                    float2 dir = float2(cos(c.z), sin(c.z));
                    float2 p = px - c.xy * texSize;
                    float across = dot(p, float2(-dir.y, dir.x));
                    float along = dot(p, dir);
                    float dash = step(frac(along / 14.0 + c.w), 0.62);
                    float w = _CreaseWidth * 0.5;
                    creaseMask = max(creaseMask, on * dash * (1.0 - smoothstep(w, w + 1.0, abs(across))));
                    highlight = max(highlight, on * dash * (1.0 - smoothstep(0.6, 1.4, abs(across - w - 1.2))));
                }
                col.rgb = lerp(col.rgb, creaseCol, creaseMask * paper);
                col.rgb = lerp(col.rgb, float3(1, 1, 1), highlight * paper * 0.5 * (1.0 - creaseMask));

                // Torn edge: fibrous white rim along the seam.
                float rim = (1.0 - smoothstep(0.0, 0.012, seamDist)) * tearOn;
                col.rgb = lerp(col.rgb, lerp(float3(1, 1, 1), _CreaseWarnColor.rgb, 0.25), rim * col.a);

                // Ink stain: blotches spread as _Stain rises.
                float n = KamiValueNoise(px / 18.0 + 7.1) * 0.65 + KamiValueNoise(px / 6.0) * 0.35;
                float stainEdge = 1.0 - _Stain * 0.8;
                float stain = smoothstep(stainEdge, stainEdge + 0.05, n) * step(0.001, _Stain);
                col.rgb = lerp(col.rgb, _StainColor.rgb, stain * 0.92);

                // Paper grain (world space for static scenery, sprite space for moving sprites).
                float2 gp = lerp(px, i.positionWS.xy * 128.0, _GrainWorld) / max(_GrainSize, 0.5);
                col.rgb *= 1.0 + KamiPaperGrain(gp) * _GrainStrength * (0.35 + 0.65 * paper);

                col.rgb = lerp(col.rgb, _FadeColor.rgb, _Fade);
                col.rgb = lerp(col.rgb, _FlashColor.rgb, _Flash);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
