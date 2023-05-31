// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "LinesForUGUI1"
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
                float4 custom0 : TEXCOORD0;//ctrPos(xy) fadeRadius(z)
                float4 custom1 : TEXCOORD1;//radius, blankStart, blankLen, lineDis
                float4 custom2 : TEXCOORD2;//os(xy)
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

                OUT.custom0 = v.custom0;//circleCenter(xy) fadeRadius(z)
                OUT.custom1 = v.custom1;//radius, blankStart, blankLen, lineDis
                OUT.custom2 = v.custom2;//os(xy)
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float2 sdfPos = IN.custom2.xy;
                float2 circleCenter = IN.custom0.xy;

                float sd = length(sdfPos - circleCenter) - IN.custom1.x;

                /*float blankStart = IN.custom1.y;
                float blankLen = IN.custom1.z;
                float lineDis = IN.custom1.w;
                float cycleLen = fmod(lineDis, blankStart + blankLen);
                float blank = step(blankStart, cycleLen);
                sd += blank * 999;*/
                    
                half4 color = IN.color;
                float fade = saturate(-sd * (1 / IN.custom0.z));
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