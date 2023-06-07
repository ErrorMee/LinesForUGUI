// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "DashedLine"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _FadeRadius("AA FadeRadius", Range(0, 128)) = 8
        _OnionRadius("OnionRadius", Range(0, 128)) = 0
        _StencilComp("Stencil Comparison", Float) = 1
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
                float4 custom0  : TEXCOORD0;//abPos
                float4 custom1  : TEXCOORD1;//thickness, blankStart, blankLen, roundRadius
                float4 custom2  : TEXCOORD2;//os(xy), lineDis, offsetA
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex           : SV_POSITION;
                float4 color            : COLOR;
                float4 worldPosition    : TEXCOORD0;
                float4 custom0          : TEXCOORD1;
                float4 custom1          : TEXCOORD2;
                float4 custom2          : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;
            float _FadeRadius;
            float _OnionRadius;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color;
                OUT.worldPosition = v.vertex;
                
                OUT.custom0 = v.custom0;//abPos

                //15 0128.999
                float lackStart = floor(v.custom1.x * 0.0001);
                float thickness = v.custom1.x - lackStart * 10000;

                float lackEnd = floor(v.custom1.y * 0.001);
                float blankStart = v.custom1.y - lackEnd * 1000;
                
                //thickness, blankStart, blankLen, roundRadius
                OUT.custom1.x = thickness; OUT.custom1.y = blankStart;
                OUT.custom1.z = v.custom1.z; OUT.custom1.w = v.custom1.w;

                OUT.custom2 = v.custom2;//os(xy), lineDis, offsetA
                
                OUT.worldPosition.z = lackStart; OUT.worldPosition.w = lackEnd;
                
                return OUT;
            }

            float opIntersection(float d1, float d2) { return max(d1, d2); }

            float opOnion( in float sdf, in float thickness )
            {
                return abs(sdf)-thickness;
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
                float blankStart = IN.custom1.y;
                float roundRadius = IN.custom1.w;
                float solidLen = blankStart + roundRadius * 2;
                float blankLen = IN.custom1.z;
                float localLen = solidLen + blankLen;
                float lineDis = IN.custom2.z;
                float2 os = IN.custom2.xy;


                float2 a2bDir = normalize(abPos.zw - abPos.xy);
                int localIndex = floor((lineDis - IN.custom2.w) / localLen);

                float4 abPosLocal;
                abPosLocal.xy = abPos.xy + (localIndex * localLen + roundRadius + IN.custom2.w) * a2bDir;
                abPosLocal.zw = abPosLocal.xy + blankStart * a2bDir;
                float sdLocal = sdOrientedBox(os, abPosLocal.xw, abPosLocal.zy, thickness) - roundRadius;
                sdLocal -= step(min(blankStart, blankLen), 0) * 1024;

                float sd = sdLocal;

                half4 globalAB; 
                globalAB.xy = abPos.xy + (roundRadius - IN.worldPosition.z) * a2bDir;
                globalAB.zw = abPos.zw - (roundRadius - IN.worldPosition.w) * a2bDir;
                float sdGlobal = sdOrientedBox(os, globalAB.xw, globalAB.zy, thickness) - roundRadius;

                sd = opIntersection(sd, sdGlobal);

                float isOnion = step(_FadeRadius * 0.5, _OnionRadius);
                sd = (1 - isOnion) * sd + isOnion * opOnion(sd + _OnionRadius, _OnionRadius);
                
                half4 color = IN.color;
                float fade = saturate(-sd * (1.0 / _FadeRadius)); fade *= fade; fade *= fade;
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