Shader "StarryStudy/SurroundingNight"
{
 SubShader {
 Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 struct A { float4 p:POSITION; };struct V { float4 p:SV_POSITION;float3 ray:TEXCOORD0; };
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.ray=i.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(17.71,83.13,41.31)))*43758.5453);}
 float swirl(float3 n,float3 axis){float3 e=normalize(cross(float3(0,1,0),axis)),u=cross(axis,e);float r=acos(clamp(dot(n,axis),-1,1)),a=atan2(dot(n,u),dot(n,e));return sin(r*42-a*2)*exp(-r*r/.22);}
 half4 frag(V i):SV_Target {
 float3 n=normalize(i.ray);float az=atan2(n.x,n.z),el=asin(clamp(n.y,-1,1));
 // Periodic angular currents are continuous at the back seam and at the poles.
 float flow=.5+.5*sin(el*22+(sin(az*3)*3+sin(az*7+el*5))*cos(el));
 flow=saturate(flow+.27*swirl(n,normalize(float3(-.65,.35,-.67)))+.24*swirl(n,normalize(float3(.85,.35,.32))));
 float3 c=lerp(float3(.10,.22,.39),float3(.30,.47,.57),flow);
 c=lerp(c,float3(.60,.67,.52),saturate((flow-.78)*1.5));
 float brush=sin(dot(n,float3(83,119,47))+sin(dot(n,float3(37,71,53)))*2)*sin(dot(n,float3(137,-79,97)));
 float dab=hash(floor(n*235+sin(n.zxy*51)*1.4));
 float fine=hash(floor(n*570));
 c*=.79+dab*.40+brush*.035+fine*.035;
 for(int k=0;k<16;k++){float a=k*2.399963+.7;float h=.15+.7*frac(k*.618034);float3 s=float3(sin(a)*sqrt(1-h*h),h,cos(a)*sqrt(1-h*h));float d=max(0,1-dot(n,s));float halo=exp(-d*650);float core=exp(-d*21000);c+=float3(.24,.22,.09)*halo+float3(.75,.60,.17)*core;}
 // Pigment-blue horizon continues below the modeled terrain rather than showing a flat clear color.
 c=lerp(c,float3(.065,.12,.20),(1-smoothstep(-.65,-.1,n.y))*.65);
 return half4(SRGBToLinear(saturate(c)),1);
 }
 ENDHLSL
 }
 }
}
