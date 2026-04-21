Shader "Universal Render Pipeline/PosterRectDecalProjection"
{
    Properties
    {
        [Header(Decal)]
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0
        _MirrorU ("Mirror U (0/1)", Float) = 0
        _MirrorV ("Mirror V (0/1)", Float) = 0
        _StretchU ("Stretch U (multiplier)", Float) = 1
        _StretchV ("Stretch V (multiplier)", Float) = 1

        [Header(Rect Surface OS)]
        _PlaneAxisU ("U Axis: 0=X, 1=Y, 2=Z", Float) = 0
        _PlaneAxisV ("V Axis: 0=X, 1=Y, 2=Z", Float) = 1
        _PlaneAxisN ("Normal Axis: 0=X, 1=Y, 2=Z", Float) = 2
        _PlaneHalfU ("Plane Half Size U", Float) = 0.5
        _PlaneHalfV ("Plane Half Size V", Float) = 0.5
        _PlaneCenterOS ("Plane Center (object-space)", Vector) = (0,0,0,0)
        _PlaneOffset ("Plane Offset Along Normal", Float) = 0
        _FrontOnly ("Front Only (0/1)", Float) = 0
        _Curvature ("Curvature Amount", Range(0,1)) = 0

        [Header(Appearance)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalRenderPipeline" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DecalTex);
            SAMPLER(sampler_DecalTex);

            float4 _DecalRect;
            float _DecalRotation;
            float _MirrorU;
            float _MirrorV;
            float _StretchU;
            float _StretchV;
            float _PlaneAxisU;
            float _PlaneAxisV;
            float _PlaneAxisN;
            float _PlaneHalfU;
            float _PlaneHalfV;
            float4 _PlaneCenterOS;
            float _PlaneOffset;
            float _FrontOnly;
            float _Curvature;
            float4 _BaseColor;
            float _AlphaClip;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                float3 normalOS    : TEXCOORD1;
            };

            float3 AxisVector(float axis)
            {
                if (axis < 0.5) return float3(1, 0, 0);
                if (axis < 1.5) return float3(0, 1, 0);
                return float3(0, 0, 1);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS = IN.normalOS;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float3 pos = IN.positionOS;
                float3 normalOS = normalize(IN.normalOS);
                
                // Получаем оси
                float3 uAxis = AxisVector(_PlaneAxisU);
                float3 vAxis = AxisVector(_PlaneAxisV);
                float3 nAxis = AxisVector(_PlaneAxisN);
                
                // Позиция относительно центра плоскости
                float3 localPos = pos - _PlaneCenterOS.xyz;
                
                // Координаты на плоскости
                float uCoord = dot(localPos, uAxis);
                float vCoord = dot(localPos, vAxis);
                float nCoord = dot(localPos, nAxis) - _PlaneOffset;
                
                // Применяем растяжение к границам плоскости
                float stretchedHalfU = _PlaneHalfU / max(_StretchU, 0.01);
                float stretchedHalfV = _PlaneHalfV / max(_StretchV, 0.01);
                
                // Проверка попадания в объем плоскости с учетом растяжения
                if (abs(uCoord) > stretchedHalfU) return _BaseColor;
                if (abs(vCoord) > stretchedHalfV) return _BaseColor;
                if (abs(nCoord) > 0.1) return _BaseColor;
                
                // Проверка фронтальности
                if (_FrontOnly > 0.5)
                {
                    float facing = dot(normalOS, nAxis);
                    if (facing < 0.1) return _BaseColor;
                }
                
                // UV координаты на плоскости (0-1) с учетом растяжения
                float2 planeUV = float2(
                    (uCoord / stretchedHalfU) * 0.5 + 0.5,
                    (vCoord / stretchedHalfV) * 0.5 + 0.5
                );
                
                // Координаты декали
                float2 decalCenter = _DecalRect.xy;
                float2 decalHalf = _DecalRect.zw;
                
                // Поворот
                float rad = _DecalRotation * 0.0174532925;
                float c = cos(rad);
                float s = sin(rad);
                float2 toCenter = planeUV - decalCenter;
                float2 rotatedUV = float2(
                    toCenter.x * c + toCenter.y * s,
                    -toCenter.x * s + toCenter.y * c
                );
                
                // Отзеркаливание
                if (_MirrorU > 0.5) rotatedUV.x = -rotatedUV.x;
                if (_MirrorV > 0.5) rotatedUV.y = -rotatedUV.y;
                
                // Проверка попадания в декаль
                if (abs(rotatedUV.x) > decalHalf.x) return _BaseColor;
                if (abs(rotatedUV.y) > decalHalf.y) return _BaseColor;
                
                // Финальные UV текстуры
                float2 finalUV = float2(
                    (rotatedUV.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)),
                    (rotatedUV.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001))
                );
                
                float4 decalCol = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, finalUV);
                float4 col = decalCol * _BaseColor;
                
                if (col.a <= _AlphaClip) discard;
                
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}