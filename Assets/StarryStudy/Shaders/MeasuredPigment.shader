Shader "StarryStudy/MeasuredPigment"
{
 Properties { _MotionEnabled("Brush advection",Float)=0 _FlowRate("Flow rate",Float)=1 _FlowGain("Flow amount",Float)=1 }
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
 struct A {float4 p:POSITION;float3 n:NORMAL;half4 c:COLOR;float2 uv:TEXCOORD0;float4 flow:TEXCOORD2;float4 curve:TEXCOORD3;};
 struct V {float4 p:SV_POSITION;float3 n:TEXCOORD0;half4 c:COLOR;float2 uv:TEXCOORD1;};
 float _BrushFlowTime,_MotionEnabled,_FlowRate,_FlowGain;
 V vert(A a){
 V o;float3 p=a.p.xyz;
 // Bounded, continuous brush motion: no lifetime reset or visibility dissolve.
 float phase=a.curve.w*6.2831853,time=_BrushFlowTime*.18*_FlowRate;
 float amplitude=a.flow.w*_MotionEnabled*.18*_FlowGain;
 float d=sin(phase+time)-sin(phase),bend=(cos(phase+time)-cos(phase))*.35;
 p+=a.flow.xyz*amplitude*d+a.curve.xyz*amplitude*bend;
 o.p=TransformObjectToHClip(p);o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;o.uv=a.uv;return o;
 }
 half4 frag(V i):SV_Target {
 float light=saturate(dot(normalize(i.n),normalize(float3(-.3,.6,-.7))));float ridge=sin(i.uv.x*55+i.uv.y*8)*.012;
 return half4(SRGBToLinear(saturate(i.c.rgb*(.99+.055*light+ridge))),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Simple Lit/DepthOnly"
 }
}
