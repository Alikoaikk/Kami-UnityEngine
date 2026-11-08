// Water and living ink: put it on a plain Square sprite (Sprites/Shapes/square) and scale the
// sprite to the size of the pool. The top edge of the sprite is the surface: it waves, gets a
// darker rim, and the body has two flat bands (surface / deep) with drifting "~" squiggles, as
// in the GDD. All sizes are in world units, so stretching the sprite never stretches the waves.
//
// M_Water: blue, white squiggles. M_Ink: black, grey squiggles (ink pits, the rising-ink finale).
Shader "Kami/Liquid"
{
    Properties
    {
        _MainTex ("Sprite (mask)", 2D) = "white" {}
        _Color ("Surface Colour", Color) = (0.663, 0.8, 0.867, 0.92)
        _DeepColor ("Deep Colour", Color) = (0.576, 0.737, 0.816, 0.96)
        _EdgeColor ("Surface Rim Colour", Color) = (0.353, 0.561, 0.659, 1)
        _LineColor ("Squiggle Colour", Color) = (1, 1, 1, 0.9)
        _DeepStart ("Deep Band Starts (units below surface)", Float) = 0.9
        _EdgeWidth ("Surface Rim Width (units)", Float) = 0.05
        _WaveHeight ("Wave Height (units)", Float) = 0.08
        _WaveLength ("Wave Length (units)", Float) = 1.6
        _WaveSpeed ("Wave Speed", Float) = 1
        _FlowSpeed ("Current (units/s, + = right)", Float) = 0.3
        _SquiggleDensity ("Squiggles Per Unit", Float) = 0.8

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "KamiCommon.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DeepColor;
                half4 _EdgeColor;
                half4 _LineColor;
                float _DeepStart;
                float _EdgeWidth;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                float _FlowSpeed;
                float _SquiggleDensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = input.uv;
                o.color = input.color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 ws = i.positionWS.xy;
                float t = _Time.y;
                float height = KamiQuadWorldSize(ws, i.uv).y;
                float depth = (1.0 - i.uv.y) * height;              // units below the top edge

                // Surface: two travelling sines; everything above it is cut away.
                float k = 6.2832 / max(_WaveLength, 0.01);
                float surface = _WaveHeight * (0.5 + 0.35 * sin(ws.x * k + t * _WaveSpeed * 2.0)
                                                   + 0.15 * sin(ws.x * k * 2.7 - t * _WaveSpeed * 1.3));
                float aa = max(fwidth(depth), 1e-4);
                float body = smoothstep(surface - aa, surface + aa, depth);

                // Bands: surface colour, then the deep colour below a gently wavy line.
                float deepLine = _DeepStart + 0.05 * sin(ws.x * 2.1 + t * 0.6);
                half4 col = lerp(_Color, _DeepColor, smoothstep(deepLine - aa, deepLine + aa, depth));

                // Rim just under the surface.
                float rim = 1.0 - smoothstep(surface + _EdgeWidth - aa, surface + _EdgeWidth + aa, depth);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, rim * _EdgeColor.a);

                // "~" squiggles drifting with the current, one per grid cell (some cells empty).
                float2 cellSize = float2(1.0, 0.55) / max(_SquiggleDensity, 0.01);
                float2 p = float2(ws.x - t * _FlowSpeed, depth);
                float2 cell = floor(p / cellSize);
                float2 jitter = float2(KamiHash21(cell + 3.1), KamiHash21(cell + 7.7)) - 0.5;
                float2 q = (frac(p / cellSize) - 0.5 - jitter * 0.4) * cellSize;
                float h = KamiHash21(cell);
                float halfLen = 0.17;
                float yWave = 0.028 * sin(q.x / halfLen * 6.2832 + t * 2.0 + h * 6.2832);
                float dist = abs(q.y - yWave);
                float lineAA = max(fwidth(dist), 1e-4);
                float squiggle = (1.0 - smoothstep(0.012, 0.012 + lineAA * 1.5, dist))
                               * step(abs(q.x), halfLen) * step(0.4, h) * step(surface + 0.18, depth);
                col.rgb = lerp(col.rgb, _LineColor.rgb, squiggle * _LineColor.a);

                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                col.a *= body * mask.a;
                col.rgb *= mask.rgb;
                return col;
            }
            ENDHLSL
        }
    }
}
