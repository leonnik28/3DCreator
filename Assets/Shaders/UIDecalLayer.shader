Shader "UI/UIDecalLayer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Selection)]
        [Toggle] _Selected ("Selected", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,0.8,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _DimColor ("Dim Color", Color) = (0.5,0.5,0.5,0.5)
        
        [Header(Mask)]
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
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _OutlineColor;
            float4 _DimColor;
            float _Selected;  // Это должно соответствовать имени в Properties
            float _OutlineWidth;
            
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Основной цвет текстуры
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Вычисляем расстояние до края текстуры
                float2 uv = IN.texcoord;
                float2 edgeDist = min(uv, 1.0 - uv);
                float minEdgeDist = min(edgeDist.x, edgeDist.y);
                
                half4 finalColor;
                
                // Проверяем, выделена ли декаль
                if (_Selected > 0.5)
                {
                    // ВЫДЕЛЕННАЯ: оригинальная текстура + обводка
                    finalColor = texColor;
                    
                    // Вычисляем обводку
                    float outline = 1.0 - smoothstep(0, _OutlineWidth, minEdgeDist);
                    
                    // Добавляем обводку
                    finalColor.rgb = lerp(finalColor.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                    finalColor.a = 1.0;
                }
                else
                {
                    // НЕВЫДЕЛЕННАЯ: затемнённая версия, без обводки
                    finalColor = texColor;
                    finalColor.rgb = finalColor.rgb * _DimColor.rgb;
                    finalColor.a = finalColor.a * _DimColor.a;
                }
                
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif
                
                return finalColor;
               
            }
            ENDCG
        }
    }
}