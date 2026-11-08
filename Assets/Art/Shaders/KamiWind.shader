// Wind column: rising, swaying brush streaks over a faint blue haze. Put it on a Square sprite
// scaled to the column (e.g. 1.5 x 6 units) above a wind_vent. Fades out at the sides and top.
Shader "Kami/Wind"
{
    Properties
    {
        _MainTex ("Sprite (mask)", 2D) = "white" {}
        _HazeColor ("Haze Colour", Color) = (0.663, 0.8, 0.867, 0.22)
        _LineColor ("Streak Colour", Color) = (0.353, 0.561, 0.659, 0.95)
        _Speed ("Rise Speed (units/s)", Float) = 2.5
        _Lanes ("Streak Lanes Per Unit", Float) = 2.2
        _Sway ("Sway", Range(0, 0.4)) = 0.22
        _StreakLength ("Streak Length (0-1)", Range(0.1, 0.9)) = 0.45

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
                half4 _HazeColor;
                half4 _LineColor;
                float _Speed;
                float _Lanes;
                float _Sway;
                float _StreakLength;
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
                float2 size = KamiQuadWorldSize(i.positionWS.xy, i.uv);
                float t = _Time.y;

                // Lanes across the column; each lane has its own speed, phase and sway.
                float lane = i.uv.x * size.x * _Lanes;
                float laneId = floor(lane);
                float h = KamiHash21(float2(laneId, 1.7));
                float y = i.uv.y * size.y - t * _Speed * (0.7 + 0.6 * h);
                float lx = frac(lane) - 0.5;
                float sway = sin(y * 2.2 + h * 6.2832) * _Sway;
                float d = abs(lx - sway);
                float aa = max(fwidth(d), 1e-4);
                float seg = frac(y * 0.45 + h);
                float dash = smoothstep(0.0, 0.06, seg) * smoothstep(_StreakLength, _StreakLength - 0.06, seg);
                float streak = (1.0 - smoothstep(0.06, 0.06 + aa * 1.5, d)) * dash;

                float fade = smoothstep(0.0, 0.18, i.uv.x) * smoothstep(1.0, 0.82, i.uv.x)
                           * smoothstep(1.0, 0.7, i.uv.y);

                half4 col;
                col.rgb = lerp(_HazeColor.rgb, _LineColor.rgb, streak);
                col.a = saturate(_HazeColor.a + streak * _LineColor.a) * fade;

                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                col.rgb *= mask.rgb;
                col.a *= mask.a;
                return col;
            }
            ENDHLSL
        }
    }
}
