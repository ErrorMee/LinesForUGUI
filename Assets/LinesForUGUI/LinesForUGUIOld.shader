// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "LinesForUGUIOld"
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
                    float4 abPos : TEXCOORD0;
                    float4 custom : TEXCOORD1;//(x:Box thickness) (y:round) (z:blank start) (w:blank len) 
                    float4 custom1 : TEXCOORD2;//(xy:os) (z:fadeRadius) (w:offsetStart)
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    float4 color : COLOR;
                    float4 abPos  : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    float4 custom : TEXCOORD2;
                    float4 custom1 : TEXCOORD3;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                fixed4 _TextureSampleAdd;
                float4 _ClipRect;
                float4 _MainTex_ST;

                v2f vert(appdata_t v)
                {
                    v2f OUT;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                    OUT.worldPosition = v.vertex;
                    OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                    OUT.abPos = v.abPos;
                    OUT.color = v.color;
                    OUT.custom = v.custom;
                    OUT.custom1 = v.custom1;
                    return OUT;
                }

                float sdOrientedBox(in float2 p, in float2 a, in float2 b, float th)
                {
                    float l = length(b - a);
                    float2 d = (b - a) / l;
                    float2 q = (p - (a + b) * 0.5);
                    q = mul(float2x2(d.x, -d.y, d.y, d.x), q);
                    q = abs(q) - float2(l, th) * 0.5;
                    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
                }

                half4 frag(v2f IN) : SV_Target
                {
                    float round = IN.custom.y;
                    float sdGlobal = sdOrientedBox(IN.custom1.xy, IN.abPos.xw, IN.abPos.zy, IN.custom.x) - round;


                    float offsetStart = IN.custom1.w + round;
                    float2 a2bDir = normalize(IN.abPos.zw - IN.abPos.xy);
                    float round2 = round * 2;
                    float solidLen = IN.custom.z; float blankLen = IN.custom.w;
                    float blockLen = solidLen + blankLen + round2;
                    int blockIndex = floor(offsetStart / blockLen);

                    float4 abPos;
                    float2 aOriPos = IN.abPos.xy + a2bDir * blockLen * blockIndex;
                    abPos.xy = aOriPos;//IN.abPos.xy ??? blockIndex
                    abPos.zw = aOriPos + a2bDir * solidLen;
                    float sdLocal = sdOrientedBox(IN.custom1.xy, abPos.xw, abPos.zy, IN.custom.x) - round;

                    sdLocal -= step(IN.custom.w, 0) * 1024;
                    
                    float sd = max(sdLocal, sdGlobal);
                    sd = sdLocal;
                    half4 color = IN.color;
                    color.a *= saturate(-sd * (1 / IN.custom1.z));

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