Shader "Custom/HighlightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (1,1,0,0.5)
        _HighlightIntensity ("Highlight Intensity", Range(0, 1)) = 0.5
        _HighlightWidth ("Highlight Width", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _HighlightColor;
            float _HighlightIntensity;
            float _HighlightWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Вычисляем расстояние до края текстуры
                float2 edgeDist = min(i.uv, 1.0 - i.uv);
                float minEdgeDist = min(edgeDist.x, edgeDist.y);
                
                // Создаем рамку там, где расстояние до края меньше ширины рамки
                float isEdge = step(minEdgeDist, _HighlightWidth);
                
                // Плавное затухание рамки
                float edgeFactor = smoothstep(0, _HighlightWidth, minEdgeDist);
                edgeFactor = 1.0 - edgeFactor;
                
                // Смешиваем оригинальный цвет с цветом подсветки
                float3 highlighted = lerp(col.rgb, _HighlightColor.rgb, edgeFactor * _HighlightIntensity);
                
                return fixed4(highlighted, col.a);
            }
            ENDCG
        }
    }
}