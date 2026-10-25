Shader "StarryStudy/CinematicCover"
{
 Properties { [PerRendererData]_MainTex("Painting",2D)="white"{} _Veil("Night veil",Range(0,1))=1 _CoverTime("Time",Float)=0 }
 SubShader {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;fixed4 c:COLOR;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;fixed4 c:COLOR;};
   sampler2D _MainTex;float _Veil,_CoverTime;
   V vert(A a){V o;o.p=UnityObjectToClipPos(a.p);o.uv=a.uv;o.c=a.c;return o;}
   fixed4 frag(V i):SV_Target{
    float2 drift=float2(sin(_CoverTime*.065),cos(_CoverTime*.052))*.0018*_Veil;
    float zoom=1+.035*_Veil;float2 uv=(i.uv-.5)/zoom+.5+drift;
    float3 painting=tex2D(_MainTex,uv).rgb;
    float bottom=1-smoothstep(.04,.68,i.uv.y);float vignette=saturate(length((i.uv-.5)*float2(1.0,.72))*1.5);
    float2 focus=(i.uv-float2(.5,.45))*float2(2.4,3.0);float center=exp(-dot(focus,focus));
    float shade=saturate(.18+bottom*.62+vignette*.20+center*.20)*_Veil;
    float3 result=lerp(painting,float3(.008,.018,.035),shade);
    return fixed4(result*i.c.rgb,i.c.a);
   }
   ENDHLSL
  }
 }
}
