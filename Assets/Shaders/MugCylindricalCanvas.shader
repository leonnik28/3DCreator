Shader "Universal Render Pipeline/MugCylindricalCanvas"
{
    Properties
    {
        _CanvasTex ("Canvas Texture (RenderTexture)", 2D) = "white" {}
        _CanvasTiling ("Canvas Tiling (U,V)", Vector) = (1,1,0,0)
        _CanvasOffset ("Canvas Offset (U,V)", Vector) = (0,0,0,0)

        _MugCenterWS ("Mug Center (World)", Vector) = (0,0,0,0)
        _MugRadius   ("Mug Radius", Float) = 0.5
        _MugHeight   ("Mug Height", Float) = 1.0

        _BaseColor   ("Base Color", Color) = (1,1,1,1)
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

            TEXTURE2D(_CanvasTex);
            SAMPLER(sampler_CanvasTex);

            float4 _CanvasTiling;      // xy = tiling
            float4 _CanvasOffset;      // xy = offset

            float4 _MugCenterWS;
            float  _MugRadius;
            float  _MugHeight;

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
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = normalize(TransformObjectToWorldNormal(IN.normalOS));

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.positionWS  = positionWS;
                OUT.normalWS    = normalWS;

                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float3 worldPos    = IN.positionWS;
                float3 worldNormal = normalize(IN.normalWS);

                float3 center = _MugCenterWS.xyz;

                // Вектор от центра кружки до точки на поверхности
                float3 fromCenter = worldPos - center;

                // Убираем компоненту по высоте для вычисления угла вокруг оси Y
                float2 horizontal = float2(fromCenter.x, fromCenter.z);
                float  horLen     = length(horizontal);

                // Если точка ближе к центру, чем радиус — считаем, что внутри, отбрасываем
                if (horLen < _MugRadius * 0.99)
                    discard;

                // Высота вдоль оси Y относительно центра
                float y = fromCenter.y;

                // Обрезка по высоте кружки
                float halfH = _MugHeight * 0.5;
                if (y < -halfH || y > halfH)
                    discard;

                // Рисуем только внешнюю сторону
                float3 expectedOut = normalize(float3(horizontal.x, 0, horizontal.y));
                float   facing     = dot(worldNormal, expectedOut);
                if (facing < 0.0)
                    discard;

                // Цилиндрическое UV: U = угол вокруг оси, V = высота
                float angle = atan2(horizontal.x, horizontal.y);            // -PI..PI
float u     = (angle / (2.0 * 3.14159265)) + 0.5;     // 0..1
                float v     = saturate((y + halfH) / (2.0 * halfH));  // 0..1

                float2 uv = float2(u, v);
                uv = uv * _CanvasTiling.xy + _CanvasOffset.xy;

                // Если выходим за рамки полотна — не рисуем
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    discard;

                float4 canvasCol = SAMPLE_TEXTURE2D(_CanvasTex, sampler_CanvasTex, uv);
                float4 col       = canvasCol * _BaseColor;

                if (col.a <= _AlphaClip)
                    discard;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}