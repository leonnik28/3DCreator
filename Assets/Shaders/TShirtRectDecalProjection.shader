Shader "Universal Render Pipeline/TShirtRectDecalLit"
{
    Properties
    {
        [Header(Decal Settings)]
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0

        [Header(Projection Plane)]
        _PlaneAxisU ("U Axis: 0=X, 1=Y, 2=Z", Float) = 0
        _PlaneAxisV ("V Axis: 0=X, 1=Y, 2=Z", Float) = 1
        _PlaneAxisN ("Normal Axis: 0=X, 1=Y, 2=Z", Float) = 2
        _PlaneHalfU ("Plane Half Size U", Float) = 0.5
        _PlaneHalfV ("Plane Half Size V", Float) = 0.5
        _PlaneCenterOS ("Plane Center (object-space)", Vector) = (0,0,0,0)
        _PlaneOffset ("Plane Offset Along Normal", Float) = 0
        _PlaneNormalSign ("Plane Normal Sign", Float) = 1
        _FrontOnly ("Front Only (0/1)", Float) = 1
        _Curvature ("Curvature Amount", Range(0,1)) = 0.05

        [Header(No Print Zones)]
        _NoPrintCenterU ("No-Print Center U (0..1)", Range(0,1)) = 0
        _NoPrintHalfU ("No-Print Half Width U", Range(0,0.5)) = 0
        _NoPrintAtEdges ("No-Print At Canvas Edges (0/1)", Float) = 1
        _NoPrintCenterV ("No-Print Center V (0..1)", Range(0,1)) = 0.5
        _NoPrintHalfV ("No-Print Half Width V", Range(0,0.5)) = 0
        _NoPrintAtEdgesV ("No-Print At Canvas Edges V (0/1)", Float) = 1

        [Header(Material Appearance)]
        _BaseColor ("Decal Tint Color", Color) = (1,1,1,1)
        _SurfaceColor ("Surface Color (Fabric)", Color) = (1,1,1,1)
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.2
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
                float _PlaneAxisU; float _PlaneAxisV; float _PlaneAxisN;
                float _PlaneHalfU; float _PlaneHalfV;
                float4 _PlaneCenterOS;
                float _PlaneOffset; float _PlaneNormalSign; float _FrontOnly;
                float _Curvature;
                float _NoPrintCenterU; float _NoPrintHalfU; float _NoPrintAtEdges;
                float _NoPrintCenterV; float _NoPrintHalfV; float _NoPrintAtEdgesV;
                float4 _BaseColor; float4 _SurfaceColor;
                float _AlphaClip;
                half _Smoothness; half _Metallic;
            CBUFFER_END

            TEXTURE2D(_DecalTex); SAMPLER(sampler_DecalTex);

            float AxisValue(float3 v, float axis) { if(axis<0.5)return v.x; if(axis<1.5)return v.y; return v.z; }
            float3 AxisVector(float axis) { if(axis<0.5)return float3(1,0,0); if(axis<1.5)return float3(0,1,0); return float3(0,0,1); }

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
                // --- ЛОГИКА ПРОЕКЦИИ ---
                float3 pos = IN.positionOS;
                float3 normalOS = normalize(TransformWorldToObjectNormal(IN.normalWS));
                float3 center = _PlaneCenterOS.xyz;

                float uLocal = AxisValue(pos, _PlaneAxisU) - AxisValue(center, _PlaneAxisU);
                float vLocal = AxisValue(pos, _PlaneAxisV) - AxisValue(center, _PlaneAxisV);
                float nLocal = AxisValue(pos, _PlaneAxisN);

                float uNorm = uLocal / max(_PlaneHalfU, 0.001);
                float vNorm = vLocal / max(_PlaneHalfV, 0.001);
                
                // canvasUV из вашего кода
                float2 canvasUV = float2(0.5 - uNorm * 0.5, vNorm * 0.5 + 0.5);

                half4 finalAlbedo = _SurfaceColor;
                bool isDecal = true;

                // Границы канваса
                if(canvasUV.x < 0.0 || canvasUV.x > 1.0 || canvasUV.y < 0.0 || canvasUV.y > 1.0) isDecal = false;

                // Зоны No-Print (U)
                float halfW = clamp(_NoPrintHalfU, 0.0, 0.5);
                if(isDecal && halfW > 0.0) {
                    if(_NoPrintAtEdges > 0.5) {
                        if(canvasUV.x < halfW || canvasUV.x > (1.0 - halfW)) isDecal = false;
                    } else {
                        if(abs(canvasUV.x - _NoPrintCenterU) < halfW) isDecal = false;
                    }
                }

                // Зоны No-Print (V)
                float halfHV = clamp(_NoPrintHalfV, 0.0, 0.5);
                if(isDecal && halfHV > 0.0) {
                    if(_NoPrintAtEdgesV > 0.5) {
                        if(canvasUV.y < halfHV || canvasUV.y > (1.0 - halfHV)) isDecal = false;
                    } else {
                        if(abs(canvasUV.y - _NoPrintCenterV) < halfHV) isDecal = false;
                    }
                }

                // Кривизна и направление
                float planeNormalSign = abs(_PlaneNormalSign) > 0.001 ? sign(_PlaneNormalSign) : 1.0;
                float3 nAxis = AxisVector(_PlaneAxisN) * planeNormalSign;
                float3 uAxis = AxisVector(_PlaneAxisU);
                float3 vAxis = AxisVector(_PlaneAxisV);
                float3 curvedOut = normalize(nAxis + _Curvature * (uNorm * uAxis + vNorm * vAxis * 0.35));

                if(isDecal && _FrontOnly > 0.5 && dot(normalOS, curvedOut) <= 0.0) isDecal = false;

                // Глубина проекции
                float depthAllowance = max(_Curvature * 0.05, 0.01);
                float signedDepth = (nLocal - _PlaneOffset) * planeNormalSign;
                if(isDecal && (signedDepth > 0.002 || signedDepth < -depthAllowance)) isDecal = false;

                // Рендеринг текстуры
                if(isDecal)
                {
                    float2 decalCenter = _DecalRect.xy;
                    float2 decalHalf = _DecalRect.zw;
                    float rad = _DecalRotation * 0.017453293;
                    float c = cos(rad); float s = sin(rad);
                    float2 toPoint = canvasUV - decalCenter;
                    float2 localPos = float2(toPoint.x * c + toPoint.y * s, -toPoint.x * s + toPoint.y * c);

                    if(abs(localPos.x) > decalHalf.x || abs(localPos.y) > decalHalf.y) 
                    {
                        isDecal = false;
                    }
                    else 
                    {
                        float2 decalUV = float2((localPos.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)), 
                                                (localPos.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001)));
                        half4 decalCol = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, decalUV) * _BaseColor;
                        
                        if(decalCol.a <= _AlphaClip) 
                            isDecal = false; // Если прозрачно, рисуем SurfaceColor
                        else
                            finalAlbedo = decalCol;
                    }
                }

                // --- ЛОГИКА ОСВЕЩЕНИЯ ---
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactor;
                
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(float2(0,0), float2(0,0), inputData.normalWS);
                #else
                    inputData.bakedGI = SampleSH(inputData.normalWS);
                #endif

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0; // Мы в Opaque очереди

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}