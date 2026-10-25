Shader "StarryStudy/SkySculpture"
{
 Properties { _Emission("Pigment luminance",Float)=0 _Relief("Bristle relief",Range(0,1))=.65 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Cull Back
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 struct A { float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;float2 uv:TEXCOORD0; };
 struct V { float4 p:SV_POSITION;float3 n:TEXCOORD0;float4 c:COLOR;float2 uv:TEXCOORD1;float3 local:TEXCOORD2; };
 CBUFFER_START(UnityPerMaterial)
 float _Emission,_Relief;
 CBUFFER_END
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.uv=a.uv;o.local=a.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(17.71,81.13,43.31)))*43758.5453);}
 float4 frag(V i):SV_Target {
 float3 n=normalize(i.n);float lambert=saturate(dot(n,normalize(float3(-.35,.65,-.7))));
 float shade=.84+.16*smoothstep(0,1,lambert);
 float groove=sin(i.uv.x*100+sin(i.uv.y*46)*1.5);
 float grain=hash(floor(i.local*48))-.5;
 float3 c=saturate(i.c.rgb*(shade+groove*.032*_Relief)+grain*.018);
 c=lerp(c*float3(.78,.9,1.08),c,lambert*.7+.3);
 return float4(SRGBToLinear(c)*(1+_Emission),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Simple Lit/DepthOnly"
 }
}
