Shader "Universal Render Pipeline/PillowRectDecalProjection"
{
    Properties
    {
        _DecalTex ("Decal Texture", 2D) = "white" {}
        _DecalRect ("Decal Rect (centerU, centerV, halfW, halfH)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _DecalRotation ("Decal Rotation (degrees)", Float) = 0
        _MirrorU ("Mirror U (0/1)", Float) = 0
        _MirrorV ("Mirror V (0/1)", Float) = 0
        _CanvasFlipX ("Canvas Flip X (0/1)", Float) = 0
        _PlaneAxisU ("U Axis: 0=X, 1=Y, 2=Z", Float) = 0
        _PlaneAxisV ("V Axis: 0=X, 1=Y, 2=Z", Float) = 1
        _PlaneAxisN ("Normal Axis: 0=X, 1=Y, 2=Z", Float) = 2
        _PlaneHalfU ("Plane Half Size U", Float) = 0.5
        _PlaneHalfV ("Plane Half Size V", Float) = 0.5
        _PlaneCenterOS ("Plane Center (object-space)", Vector) = (0,0,0,0)
        _PlaneOffset ("Plane Offset Along Normal", Float) = 0
        _FrontOnly ("Front Only (0/1)", Float) = 1
        _Curvature ("Curvature Amount", Range(0,1)) = 0.25
        _NoPrintCenterU ("No-Print Center U (0..1)", Range(0,1)) = 0
        _NoPrintHalfU ("No-Print Half Width U", Range(0,0.5)) = 0
        _NoPrintAtEdges ("No-Print At Canvas Edges (0/1)", Float) = 1
        _NoPrintCenterV ("No-Print Center V (0..1)", Range(0,1)) = 0.5
        _NoPrintHalfV ("No-Print Half Width V", Range(0,0.5)) = 0
        _NoPrintAtEdgesV ("No-Print At Canvas Edges V (0/1)", Float) = 1
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.001
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalRenderPipeline" }
        Cull Back ZWrite On ZTest LEqual
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DecalTex); SAMPLER(sampler_DecalTex);
            float4 _DecalRect;
            float _DecalRotation;
            float _MirrorU;
            float _MirrorV;
            float _CanvasFlipX;
            float _PlaneAxisU;
            float _PlaneAxisV;
            float _PlaneAxisN;
            float _PlaneHalfU;
            float _PlaneHalfV;
            float4 _PlaneCenterOS;
            float _PlaneOffset;
            float _FrontOnly;
            float _Curvature;
            float _NoPrintCenterU;
            float _NoPrintHalfU;
            float _NoPrintAtEdges;
            float _NoPrintCenterV;
            float _NoPrintHalfV;
            float _NoPrintAtEdgesV;
            float4 _BaseColor;
            float _AlphaClip;

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 positionOS:TEXCOORD0; float3 normalOS:TEXCOORD1; };

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
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS = IN.normalOS;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionOS;
                float3 normalOS = normalize(IN.normalOS);
                float3 center = _PlaneCenterOS.xyz;

                float uLocal = AxisValue(pos, _PlaneAxisU) - AxisValue(center, _PlaneAxisU);
                float vLocal = AxisValue(pos, _PlaneAxisV) - AxisValue(center, _PlaneAxisV);
                float nLocal = AxisValue(pos, _PlaneAxisN);

                float uNorm = uLocal / max(_PlaneHalfU, 0.001);
                float vNorm = vLocal / max(_PlaneHalfV, 0.001);

                float2 canvasUV = float2(0.5 - uNorm * 0.5, vNorm * 0.5 + 0.5);
                if (_CanvasFlipX > 0.5)
                    canvasUV.x = 1.0 - canvasUV.x;
                if (canvasUV.x < 0.0 || canvasUV.x > 1.0 || canvasUV.y < 0.0 || canvasUV.y > 1.0)
                    return _BaseColor;

                float u = canvasUV.x;
                float halfW = clamp(_NoPrintHalfU, 0.0, 0.5);
                if (halfW > 0.0)
                {
                    if (_NoPrintAtEdges > 0.5)
                    {
                        if (u < halfW || u > (1.0 - halfW))
                            return _BaseColor;
                    }
                    else
                    {
                        if (abs(u - _NoPrintCenterU) < halfW)
                            return _BaseColor;
                    }
                }

                float v = canvasUV.y;
                float halfHV = clamp(_NoPrintHalfV, 0.0, 0.5);
                if (halfHV > 0.0)
                {
                    if (_NoPrintAtEdgesV > 0.5)
                    {
                        if (v < halfHV || v > (1.0 - halfHV))
                            return _BaseColor;
                    }
                    else
                    {
                        if (abs(v - _NoPrintCenterV) < halfHV)
                            return _BaseColor;
                    }
                }

                float3 nAxis = AxisVector(_PlaneAxisN);
                float3 uAxis = AxisVector(_PlaneAxisU);
                float3 vAxis = AxisVector(_PlaneAxisV);
                float3 curvedOut = normalize(nAxis + _Curvature * (uNorm * uAxis + vNorm * vAxis * 0.35));

                if (_FrontOnly > 0.5 && dot(normalOS, curvedOut) <= 0.0)
                    return _BaseColor;

                float depthAllowance = max(_Curvature * 0.08, 0.015);
                if (abs(nLocal - _PlaneOffset) > depthAllowance)
                    return _BaseColor;

                float2 decalCenter = _DecalRect.xy;
                float2 decalHalf = _DecalRect.zw;
                float rad = _DecalRotation * 0.017453293;
                float c = cos(rad);
                float s = sin(rad);
                float2 toPoint = canvasUV - decalCenter;
                float2 local = float2(toPoint.x * c + toPoint.y * s, -toPoint.x * s + toPoint.y * c);

                if (abs(local.x) > decalHalf.x || abs(local.y) > decalHalf.y)
                    return _BaseColor;

                float2 mirroredLocal = local;
                if (_MirrorU > 0.5) mirroredLocal.x = -mirroredLocal.x;
                if (_MirrorV > 0.5) mirroredLocal.y = -mirroredLocal.y;

                float2 decalUV = float2(
                    (mirroredLocal.x + decalHalf.x) / (2.0 * max(decalHalf.x, 0.001)),
                    (mirroredLocal.y + decalHalf.y) / (2.0 * max(decalHalf.y, 0.001))
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
