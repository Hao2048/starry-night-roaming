Shader "StarryStudy/Pigment"
{
 Properties { _Unlit("Unlit",Range(0,1))=0 _Emission("Emission",Float)=0 _Bristles("Bristles",Float)=1 _Tint("Pigment tint",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Cull Off
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 struct A { float4 p:POSITION; float3 n:NORMAL; float4 c:COLOR; float2 uv:TEXCOORD0; };
 struct V { float4 p:SV_POSITION; float3 n:TEXCOORD0; float4 c:COLOR; float2 uv:TEXCOORD1; float3 w:TEXCOORD2; };
 CBUFFER_START(UnityPerMaterial)
 float _Unlit,_Emission,_Bristles;float4 _Tint;
 CBUFFER_END
 V vert(A a){ V o;o.p=TransformObjectToHClip(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.uv=a.uv;o.w=TransformObjectToWorld(a.p.xyz);return o; }
 float4 frag(V i):SV_Target {
 float3 n=normalize(i.n);float l=abs(dot(n,normalize(float3(-.4,.7,-.5))));
 float shade=lerp(.75+ .25*l,1,_Unlit);
 float ridges=sin(i.uv.x*105+sin(i.uv.y*11)*1.4)*.018*_Bristles;
 float grain=frac(sin(dot(floor(i.w*85),float3(13.17,77.13,41.9)))*43758.5)-.5;
 float3 c=saturate(i.c.rgb*_Tint.rgb*(shade+ridges)+grain*.012);
 return float4(SRGBToLinear(c)*(1+_Emission),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Simple Lit/DepthOnly"
 }
}
