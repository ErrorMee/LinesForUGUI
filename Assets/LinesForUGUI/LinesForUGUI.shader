// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "LinesForUGUI"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 custom0 : TEXCOORD0;//abPos
                float4 custom1 : TEXCOORD1;//thickness, blankStart, blankLen, roundRadius
                float4 custom2 : TEXCOORD2;//os(xy), lineDis, fadeRadius
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD0;
                float4 custom0  : TEXCOORD1;
                float4 custom1 : TEXCOORD2;
                float4 custom2 : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.color = v.color;

                OUT.custom0 = v.custom0;//abPos
                OUT.custom1 = v.custom1;//thickness, blankStart, blankLen, roundRadius
                OUT.custom2 = v.custom2;//os(xy), lineDis, fadeRadius
                return OUT;
            }

            float opUnion(float d1, float d2) { return min(d1, d2); }
            float opSubtraction(float d1, float d2) { return max(-d1, d2); }

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            float sdSegment(in float2 p, in float2 a, in float2 b, float round = 0)
            {
                float2 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - round;
            }

            float sdOrientedBox(in float2 p, in float2 a, in float2 b, float thickness)
            {
                float l = length(b - a);
                float2 d = (b - a) / l;
                float2 q = (p - (a + b) * 0.5);
                q = mul(float2x2(d.x, -d.y, d.y, d.x), q);
                q = abs(q) - float2(l, thickness) * 0.5;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            half4 frag(v2f IN) : SV_Target
            {
                float4 abPos = IN.custom0;
                float thickness = IN.custom1.x;
                float roundRadius = IN.custom1.w;
                float2 os = IN.custom2.xy;
                float fadeRadius = IN.custom2.w;

                float sd = sdOrientedBox(os, abPos.xw, abPos.zy, thickness) - roundRadius;
                sd = opSubtraction(opUnion(sdCircle(os - abPos.xy, 3), sdCircle(os - abPos.zw, 3)), sd);
                half4 color = IN.color;
                float fade = saturate(-sd * (1 / fadeRadius));
                fade *= fade; fade *= fade;
                color.a *= fade;
                   
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                    
                return color;
            }
            ENDCG
        }
    }
}