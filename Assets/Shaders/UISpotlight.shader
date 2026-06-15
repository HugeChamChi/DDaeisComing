Shader "Bathhouse/UISpotlight"
{
    // 화면을 덮는 어두운 UI 이미지에 적용.
    // _TouchUV 위치를 중심으로 _Radius 반경 안쪽만 투명(밝게)해지고, 바깥은 _Color 로 어두워진다.
    // _Active 가 0 이면(손 뗌) 전체가 어두워진다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dark Color", Color) = (0,0,0,0.96)
        _TouchUV ("Touch UV", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius (height fraction)", Float) = 0.12
        _Softness ("Edge Softness", Float) = 0.03
        _Aspect ("Aspect (w/h)", Float) = 1.0
        _Active ("Active (0=off,1=on)", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _TouchUV;
            float  _Radius;
            float  _Softness;
            float  _Aspect;
            float  _Active;
            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // 종횡비 보정해서 원이 찌그러지지 않게
                float2 d = IN.texcoord - _TouchUV.xy;
                d.x *= _Aspect;
                float dist = length(d);

                // 원 안쪽 = 0(투명), 바깥 = 1(어둠)
                float mask = smoothstep(_Radius - _Softness, _Radius, dist);

                // 손 뗐으면(_Active=0) 전체 어둡게
                mask = lerp(1.0, mask, _Active);

                fixed4 col = IN.color;
                col.a *= mask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
