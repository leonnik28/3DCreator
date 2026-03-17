Shader "Universal Render Pipeline/MugDecalProjection"
{
    Properties
    {
        [Header(Decal)]
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH) in canvas 0..1", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0

        [Header(Cylinder OS)]
        _CylRadius   ("Cylinder Radius (object-space)", Float) = 0.5
        _CylHalfH    ("Cylinder Half-Height (object-space)", Float) = 1.0
        _HeightAxis  ("Height Axis: 0=X, 1=Y, 2=Z", Float) = 1
        _OuterOnly   ("Outer Only (0/1)", Float) = 1
        _OuterMin    ("Outer Min Radius Ratio", Range(0,1)) = 0.98
        _UOffset     ("U Offset (0..1)", Range(0,1)) = 0
        _VOffset     ("V Offset (0..1)", Range(0,1)) = 0
        _VScale      ("V Scale", Float) = 1
        _FlipU       ("Flip U (0/1)", Float) = 0
        _FlipV       ("Flip V (0/1)", Float) = 0

        [Header(NoPrint Zone Handle)]
        _NoPrintCenterU ("No-Print Center U (0..1)", Range(0,1)) = 0
        _NoPrintHalfU   ("No-Print Half Width U", Range(0,0.5)) = 0
        _NoPrintAtEdges ("No-Print At Canvas Edges (0/1)", Float) = 1

        [Header(Appearance)]
        _BaseColor   ("Base Color (mug where no decal)", Color) = (1,1,1,1)
        _AlphaClip   ("Alpha Clip Threshold", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Cull Back
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

            float4 _DecalRect;  // xy = center (0..1), zw = halfSize (0..1)
            float  _DecalRotation;  // degrees

            float  _CylRadius;
            float  _CylHalfH;
            float  _HeightAxis;
            float  _OuterOnly;
            float  _OuterMin;
            float  _UOffset;
            float  _VOffset;
            float  _VScale;
            float  _FlipU;
            float  _FlipV;
            float  _NoPrintCenterU;
            float  _NoPrintHalfU;
            float  _NoPrintAtEdges;

            float4 _BaseColor;
            float  _AlphaClip;

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

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS  = IN.positionOS.xyz;
                OUT.normalOS    = IN.normalOS;

                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                // Используем object-space: работает при любом повороте и масштабе цилиндра
                float3 pos = IN.positionOS;
                float height;
                float2 radial;
                float3 normalOS = normalize(IN.normalOS);

                // Выбираем ось высоты (X/Y/Z) и 2 радиальные оси
                if (_HeightAxis < 0.5) // X
                {
                    height = pos.x;
                    radial = float2(pos.y, pos.z);
                }
                else if (_HeightAxis < 1.5) // Y
                {
                    height = pos.y;
                    radial = float2(pos.x, pos.z);
                }
                else // Z
                {
                    height = pos.z;
                    radial = float2(pos.x, pos.y);
                }

                float horLen = length(radial);

                // Внутренняя часть кружки должна оставаться базовым цветом (белой),
                // а не исчезать. Поэтому возвращаем _BaseColor вместо discard.
                // Наружная область (внешний "пояс") получает проекцию декали.
                float outerMinRatio = (_OuterOnly > 0.5) ? _OuterMin : 0.01;
                if (horLen < _CylRadius * outerMinRatio)
                    return _BaseColor;

                if (height < -_CylHalfH || height > _CylHalfH)
                    discard;

                // Наружная сторона: сравниваем нормаль с радиальным направлением
                float3 expectedOut;
                if (_HeightAxis < 0.5) // X
                    expectedOut = normalize(float3(0, radial.x, radial.y));
                else if (_HeightAxis < 1.5) // Y
                    expectedOut = normalize(float3(radial.x, 0, radial.y));
                else // Z
                    expectedOut = normalize(float3(radial.x, radial.y, 0));

                float facing = dot(normalOS, expectedOut);
                if (facing < 0.0)
                    discard;

                // Cylinder UV: U = angle 0..1, V = height 0..1
                // угол считаем по radial (x=cos, y=sin) — возможно потребуется UOffset/FlipU
                float angle = atan2(radial.x, radial.y);
                float u = (angle / (2.0 * 3.14159265)) + 0.5;
                float v = (height + _CylHalfH) / (2.0 * _CylHalfH);

                // U можно зацикливать по окружности
                u = frac(u + _UOffset);

                // V НЕ зацикливаем: всё, что вне печатной зоны, должно быть базовым цветом.
                // Scale V around center 0.5, then apply offset.
                v = (v - 0.5) / max(_VScale, 0.0001) + 0.5;
                v = v + _VOffset;

                if (_FlipU > 0.5) u = 1.0 - u;
                if (_FlipV > 0.5) v = 1.0 - v;

                // Вне 0..1 — это вне области печати на кружке
                if (v < 0.0 || v > 1.0)
                    return _BaseColor;

                // Непечатная зона возле ручки (gap по U):
                // вырезаем сегмент окружности и перераспределяем (remap) оставшуюся часть
                // полотна по всей печатной области, чтобы не было искажений.
                float halfW = clamp(_NoPrintHalfU, 0.0, 0.5);
                float gapW = clamp(halfW * 2.0, 0.0, 1.0); // 0..1
                if (gapW > 0.0)
                {
                    // Два режима:
                    // 1) _NoPrintAtEdges=1: gap лежит на краях полотна (u≈0 и u≈1) — как реальная развертка под ручку.
                    // 2) _NoPrintAtEdges=0: gap по центру _NoPrintCenterU (старое поведение).

                    if (_NoPrintAtEdges > 0.5)
                    {
                        // Сдвигаем U так, чтобы центр ручки оказался в u=0 (край полотна).
                        float uRel = frac(u - _NoPrintCenterU);

                        // Внутри краевого gap (с двух сторон) — не печатаем
                        if (uRel < halfW || uRel > (1.0 - halfW))
                            return _BaseColor;

                        // Remap оставшегося диапазона [halfW .. 1-halfW] -> [0..1]
                        u = (uRel - halfW) / max(1.0 - gapW, 0.0001);
                    }
                    else
                    {
                        // Gap по центру _NoPrintCenterU (wrap-distance в [-0.5..0.5])
                        float sU = frac(u - _NoPrintCenterU + 0.5) - 0.5;

                        if (abs(sU) < halfW)
                            return _BaseColor;

                        float sU2 = (sU > halfW) ? (sU - gapW) : sU;
                        u = (sU2 + 0.5) / max(1.0 - gapW, 0.0001);
                        u = frac(u);
                    }
                }

                float2 canvasUV = float2(u, v);

                // Decal rect in canvas space
                float2 decalCenter = _DecalRect.xy;
                float2 decalHalf   = _DecalRect.zw;

                // Rotate canvas point around decal center by -angle (inverse)
                float rad = _DecalRotation * 0.017453293; // deg to rad
                float c = cos(rad);
                float s = sin(rad);
                float2 toPoint = canvasUV - decalCenter;
                float2 local = float2(toPoint.x * c + toPoint.y * s, -toPoint.x * s + toPoint.y * c);

                // Check if inside decal rect
                bool insideDecal = abs(local.x) <= decalHalf.x && abs(local.y) <= decalHalf.y;

                if (!insideDecal)
                {
                    return _BaseColor;
                }

                // Map to decal texture UV 0..1
                float2 decalUV = float2(
                    (local.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)),
                    (local.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001))
                );

                float4 decalCol = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, decalUV);
                float4 col = decalCol * _BaseColor;

                if (col.a <= _AlphaClip)
                    discard;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
