Shader "Universal Render Pipeline/MugDecalLit"
{
    Properties
    {
        [Header(Decal)]
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0

        [Header(Cylinder Projection)]
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
        _BaseColor   ("Decal Tint Color", Color) = (1,1,1,1)
        _SurfaceColor ("Surface Color (Mug Material)", Color) = (1,1,1,1)
        _AlphaClip   ("Alpha Clip Threshold", Range(0,1)) = 0.5
        _Smoothness  ("Smoothness", Range(0,1)) = 0.8
        _Metallic    ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
            "RenderPipeline"="UniversalRenderPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                half fogFactor    : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DecalRect;
                float _DecalRotation;
                float _CylRadius; float _CylHalfH; float _HeightAxis;
                float _OuterOnly; float _OuterMin;
                float _UOffset; float _VOffset; float _VScale;
                float _FlipU; float _FlipV;
                float _NoPrintCenterU; float _NoPrintHalfU; float _NoPrintAtEdges;
                float4 _BaseColor; float4 _SurfaceColor;
                float _AlphaClip;
                half _Smoothness; half _Metallic;
            CBUFFER_END

            TEXTURE2D(_DecalTex); SAMPLER(sampler_DecalTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    OUT.shadowCoord = GetShadowCoord(vertexInput);
                #else
                    OUT.shadowCoord = float4(0, 0, 0, 0);
                #endif

                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // --- 1. ЦИЛИНДРИЧЕСКАЯ ПРОЕКЦИЯ ---
                float3 pos = IN.positionOS;
                float height;
                float2 radial;
                float3 normalOS = normalize(TransformWorldToObjectNormal(IN.normalWS));

                if (_HeightAxis < 0.5) { height = pos.x; radial = float2(pos.y, pos.z); }
                else if (_HeightAxis < 1.5) { height = pos.y; radial = float2(pos.x, pos.z); }
                else { height = pos.z; radial = float2(pos.x, pos.y); }

                float horLen = length(radial);
                half4 finalAlbedo = _SurfaceColor;
                bool isDecal = true;

                // Проверка радиуса (внутренняя/внешняя часть)
                float outerMinRatio = (_OuterOnly > 0.5) ? _OuterMin : 0.01;
                if (horLen < _CylRadius * outerMinRatio) isDecal = false;

                // Проверка высоты
                if (height < -_CylHalfH || height > _CylHalfH) discard;

                // Проверка направления нормали (Facing)
                float3 expectedOut;
                if (_HeightAxis < 0.5) expectedOut = normalize(float3(0, radial.x, radial.y));
                else if (_HeightAxis < 1.5) expectedOut = normalize(float3(radial.x, 0, radial.y));
                else expectedOut = normalize(float3(radial.x, radial.y, 0));

                if (isDecal && dot(normalOS, expectedOut) < 0.0) isDecal = false;

                if (isDecal)
                {
                    // Cylinder UV
                    float angle = atan2(radial.x, radial.y);
                    float u = (angle / (2.0 * 3.14159265)) + 0.5;
                    float v = (height + _CylHalfH) / (2.0 * _CylHalfH);

                    u = frac(u + _UOffset);
                    v = (v - 0.5) / max(_VScale, 0.0001) + 0.5 + _VOffset;

                    if (_FlipU > 0.5) u = 1.0 - u;
                    if (_FlipV > 0.5) v = 1.0 - v;

                    if (v < 0.0 || v > 1.0) isDecal = false;

                    // No-Print Zone (Handle gap)
                    if (isDecal)
                    {
                        float halfW = clamp(_NoPrintHalfU, 0.0, 0.5);
                        float gapW = clamp(halfW * 2.0, 0.0, 1.0);
                        if (gapW > 0.0)
                        {
                            if (_NoPrintAtEdges > 0.5)
                            {
                                float uRel = frac(u - _NoPrintCenterU);
                                if (uRel < halfW || uRel > (1.0 - halfW)) isDecal = false;
                                else u = (uRel - halfW) / max(1.0 - gapW, 0.0001);
                            }
                            else
                            {
                                float sU = frac(u - _NoPrintCenterU + 0.5) - 0.5;
                                if (abs(sU) < halfW) isDecal = false;
                                else {
                                    float sU2 = (sU > halfW) ? (sU - gapW) : sU;
                                    u = frac((sU2 + 0.5) / max(1.0 - gapW, 0.0001));
                                }
                            }
                        }
                    }

                    // Decal Rect & Rotation
                    if (isDecal)
                    {
                        float2 canvasUV = float2(u, v);
                        float2 decalCenter = _DecalRect.xy;
                        float2 decalHalf  = _DecalRect.zw;
                        float rad = _DecalRotation * 0.017453293;
                        float c = cos(rad); float s = sin(rad);
                        float2 toPoint = canvasUV - decalCenter;
                        float2 local = float2(toPoint.x * c + toPoint.y * s, -toPoint.x * s + toPoint.y * c);

                        if (abs(local.x) > decalHalf.x || abs(local.y) > decalHalf.y)
                        {
                            isDecal = false;
                        }
                        else
                        {
                            float2 decalUV = float2((local.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)),
                                                    (local.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001)));
                            half4 decalCol = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, decalUV) * _BaseColor;
                            if (decalCol.a <= _AlphaClip) isDecal = false;
                            else finalAlbedo = decalCol;
                        }
                    }
                }

                // --- 2. ОСВЕЩЕНИЕ (PBR) ---
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}