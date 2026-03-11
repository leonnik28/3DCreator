Shader "Projector/DecalProjector" 
{
    Properties 
    {
        _MainTex ("Decal Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Falloff ("Falloff", Range(0, 1)) = 0.5
    }
    
    Subshader 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        Pass 
        {
            ZWrite Off
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct v2f 
            {
                float4 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
            };
            
            float4x4 unity_Projector;
            
            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = mul(unity_Projector, vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            sampler2D _MainTex;
            float4 _Color;
            float _Falloff;
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Проецируем UV координаты
                float2 uv = i.uv.xy / i.uv.w;
                
                // Обрезаем всё что выходит за пределы [0,1]
                clip(uv - 0.01);
                clip(1.01 - uv);
                
                fixed4 tex = tex2D(_MainTex, uv);
                fixed4 res = tex * _Color;
                
                // Применяем falloff по краям
                float falloff = 1 - length(uv - 0.5) * 2;
                falloff = saturate(falloff / _Falloff);
                res.a *= falloff;
                
                UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(0,0,0,0));
                return res;
            }
            ENDCG
        }
    }
    
    Fallback "Transparent/VertexLit"
}