Shader "StarryStudy/CanvasReveal"
{
 Properties { _MainTex("Painting",2D)="white"{} _Reveal("Reveal",Range(0,1))=0 _Opacity("Opacity",Range(0,1))=1 }
 SubShader {
 Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest Always
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST;float _Reveal,_Opacity;
 CBUFFER_END
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float4 frag(V i):SV_Target{
 float2 cells=floor(i.uv*float2(94,74));float rank=hash(cells);
 float appear=smoothstep(rank-.08,rank+.08,_Reveal*1.16);
 float2 f=frac(i.uv*float2(94,74));float edge=smoothstep(0,.13,min(f.x,1-f.x)+_Reveal*.2);
 float4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
 c.a=appear*edge*_Opacity;return c;
 }
 ENDHLSL
 }
 }
}
