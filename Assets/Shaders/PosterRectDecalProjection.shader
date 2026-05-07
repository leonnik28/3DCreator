Shader "Universal Render Pipeline/PosterRectDecalLit"
{
    Properties
    {
        [Header(Decal Settings)]
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0
        _MirrorU ("Mirror U (0/1)", Float) = 0
        _MirrorV ("Mirror V (0/1)", Float) = 0

        [Header(Projection Plane)]
        _PlaneAxisU ("U Axis: 0=X, 1=Y, 2=Z", Float) = 0
        _PlaneAxisV ("V Axis: 0=X, 1=Y, 2=Z", Float) = 1
        _PlaneAxisN ("Normal Axis: 0=X, 1=Y, 2=Z", Float) = 2
        _PlaneHalfU ("Plane Half Size U", Float) = 0.5
        _PlaneHalfV ("Plane Half Size V", Float) = 0.5
        _PlaneCenterOS ("Plane Center (object-space)", Vector) = (0,0,0,0)
        _PlaneOffset ("Plane Offset Along Normal", Float) = 0
        _FrontOnly ("Front Only (0/1)", Float) = 0
        _Curvature ("Curvature Amount", Range(0,1)) = 0

        [Header(Material Appearance)]
        _BaseColor ("Base Color (Tint)", Color) = (1,1,1,1)
        _SurfaceColor ("Surface Color", Color) = (1,1,1,1)
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalRenderPipeline" }
        LOD 300

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

            // Важен порядок подключения библиотек
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
                float _MirrorU;
                float _MirrorV;
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
                float4 _SurfaceColor;
                float _AlphaClip;
                half _Smoothness;
                half _Metallic;
            CBUFFER_END

            TEXTURE2D(_DecalTex); SAMPLER(sampler_DecalTex);

            float AxisValue(float3 v, float axis)
            {
                if (axis < 0.5) return v.x;
                if (axis < 1.5) return v.y;
                return v.z;
            }

            float3 AxisVector(float axis)
            {
                if (axis < 0.5) return float3(1,0,0);
                if (axis < 1.5) return float3(0,1,0);
                return float3(0,0,1);
            }

            Varyings vert(Attributes IN)
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

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Расчет проекции декали
                float3 pos = IN.positionOS;
                float3 normalOS = TransformWorldToObjectNormal(IN.normalWS);
                float3 center = _PlaneCenterOS.xyz;

                float uLocal = AxisValue(pos, _PlaneAxisU) - AxisValue(center, _PlaneAxisU);
                float vLocal = AxisValue(pos, _PlaneAxisV) - AxisValue(center, _PlaneAxisV);
                float nLocal = AxisValue(pos, _PlaneAxisN);

                float uNorm = uLocal / max(_PlaneHalfU, 0.001);
                float vNorm = vLocal / max(_PlaneHalfV, 0.001);
                float2 canvasUV = float2(uNorm * 0.5 + 0.5, vNorm * 0.5 + 0.5);

                half4 finalAlbedo = _SurfaceColor;
                bool isDecal = true;

                if (canvasUV.x < 0.0 || canvasUV.x > 1.0 || canvasUV.y < 0.0 || canvasUV.y > 1.0) isDecal = false;

                float3 nAxis = AxisVector(_PlaneAxisN);
                float3 uAxis = AxisVector(_PlaneAxisU);
                float3 vAxis = AxisVector(_PlaneAxisV);
                float3 curvedOut = normalize(nAxis + _Curvature * (uNorm * uAxis + vNorm * vAxis * 0.35));

                if (_FrontOnly > 0.5 && dot(normalOS, curvedOut) <= 0.0) isDecal = false;

                float depthAllowance = max(_Curvature * 0.05, 0.01);
                if (abs(nLocal - _PlaneOffset) > depthAllowance) isDecal = false;

                if (isDecal)
                {
                    float2 decalCenter = _DecalRect.xy;
                    float2 decalHalf = _DecalRect.zw;
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
                        float2 mirroredLocal = local;
                        if (_MirrorU > 0.5) mirroredLocal.x = -mirroredLocal.x;
                        if (_MirrorV > 0.5) mirroredLocal.y = -mirroredLocal.y;

                        float2 finalUV = float2(
                            (mirroredLocal.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)),
                            (mirroredLocal.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001))
                        );

                        half4 decalCol = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, finalUV) * _BaseColor;
                        if (decalCol.a <= _AlphaClip) discard;
                        finalAlbedo = decalCol;
                    }
                }

                // 2. Настройка данных для PBR освещения
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactor;
                
                // Простейший расчет GI (освещенность в тенях)
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(float2(0,0), float2(0,0), inputData.normalWS);
                #else
                    inputData.bakedGI = SampleSH(inputData.normalWS);
                #endif

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0); // Используем металлик
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = finalAlbedo.a;
                surfaceData.emission = 0.0;
                surfaceData.normalTS = half3(0, 0, 1);

                // 3. Вызов финального освещения (UniversalFragmentPBR более стабилен)
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                
                // 4. Применяем туман
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }

        // Проход для теней (ShadowCaster)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}