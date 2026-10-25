Shader "StarryStudy/PaintingHandoff"
{
 Properties { _MainTex("Opening painting",2D)="white" {} _Reveal("Reveal",Range(0,1))=0 _Push("Gentle push",Float)=0 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _Reveal,_Push;
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);float4 h=frac(sin(float4(dot(a,float2(127.1,311.7)),dot(a+float2(1,0),float2(127.1,311.7)),dot(a+float2(0,1),float2(127.1,311.7)),dot(a+1,float2(127.1,311.7))))*43758.5453);return lerp(lerp(h.x,h.y,f.x),lerp(h.z,h.w,f.x),f.y);}
   half4 frag(V i):SV_Target {
    float2 uv=(i.uv-.5)/(1+_Push)+.5;half3 painting=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
    float2 q=uv-float2(.51,.57);float spiral=sin(atan2(q.y,q.x)*2+length(q)*12);
    float threshold=.48+.10*spiral+.10*(noise(uv*18)-.5)+.06*(noise(uv*67)-.5);
    float alpha=1-smoothstep(threshold-.27,threshold+.27,_Reveal);
    return half4(painting,alpha);
   }
   ENDHLSL
  }
 }
}
