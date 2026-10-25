#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarryStudy
{
 // Reference measurements are consumed only in the Editor. The player contains closed, colored meshes.
 public static class ReferenceStrokeReconstruction
 {
  const string Root="Assets/StarryStudy/FittedMeshes";
  static readonly Vector2[] Cypress={new(.198f,.055f),new(.209f,.079f),new(.209f,.13f),new(.200f,.18f),new(.207f,.24f),new(.219f,.28f),new(.226f,.40f),new(.244f,.51f),new(.253f,.64f),new(.269f,.69f),new(.280f,.57f),new(.299f,.66f),new(.324f,.705f),new(.360f,.74f),new(.379f,.80f),new(.375f,.85f),new(.440f,.92f),new(.458f,.97f),new(.445f,1.03f),new(.180f,1.03f),new(.140f,.96f),new(.119f,.88f),new(.124f,.82f),new(.148f,.73f),new(.160f,.64f),new(.144f,.59f),new(.150f,.54f),new(.159f,.49f),new(.156f,.42f),new(.147f,.32f),new(.157f,.315f),new(.173f,.42f),new(.182f,.32f),new(.185f,.23f),new(.185f,.17f),new(.190f,.11f)};
  // Separate roof/wall volumes keep the village from becoming a single relief surface.
  static readonly Vector4[] Houses={new(.490f,.799f,.579f,.885f),new(.427f,.814f,.490f,.876f),new(.574f,.835f,.640f,.887f),new(.649f,.825f,.734f,.899f),new(.762f,.869f,.832f,.939f),new(.831f,.895f,.916f,.970f),new(.610f,.898f,.722f,1.025f),new(.673f,.948f,.776f,1.025f),new(.488f,.907f,.611f,.961f),new(.479f,.960f,.600f,1.025f),new(.886f,.871f,.958f,.922f)};
  static Transform root;
  static Material material,flowMaterial;
  static int meshID,total;
  static float longest;
  const int BucketCount=480;
  const float WorldScale=2.6f;
  static float Hash(float x)=>Mathf.Repeat(Mathf.Sin(x*127.1f+17.71f)*43758.5453f,1);
  static PaintMesh[] batches;
  static int[] counts;
  static bool Inside(Vector2 p,Vector2[] polygon){bool result=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){var a=polygon[i];var b=polygon[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)result=!result;}return result;}
  static float Mountain(float u){float[] x={0,.16f,.28f,.40f,.48f,.56f,.62f,.71f,.78f,.85f,.90f,.95f,1};float[] y={.765f,.73f,.755f,.72f,.69f,.71f,.65f,.635f,.62f,.597f,.55f,.515f,.52f};for(int i=1;i<x.Length;i++)if(u<=x[i])return Mathf.Lerp(y[i-1],y[i],Mathf.InverseLerp(x[i-1],x[i],u));return y[y.Length-1];}
  static readonly Vector3[] SourceGroves={new(.61f,.775f,.028f),new(.67f,.76f,.032f),new(.73f,.745f,.032f),new(.79f,.735f,.033f),new(.85f,.735f,.035f),new(.91f,.755f,.038f),new(.97f,.79f,.041f),new(.67f,.82f,.026f),new(.77f,.82f,.027f),new(.85f,.845f,.037f),new(.94f,.875f,.043f),new(.71f,.94f,.035f)};
  static int GroveAt(float u,float v){for(int i=0;i<SourceGroves.Length;i++){var g=SourceGroves[i];float x=(u-g.x)*1.255f/g.z,y=(v-g.y)/g.z;if(x*x+y*y<1)return i;}return -1;}
  static int Region(float u,float v){if(Inside(new Vector2(u,v),Cypress))return 2;if(u>.554f&&u<.580f&&v>.614f&&v<.826f)return 4;for(int i=0;i<Houses.Length;i++){var h=Houses[i];if(u>h.x&&u<h.z&&v>h.y&&v<h.w)return 5+i;}if(v<Mountain(u))return 0;return v<.785f?1:3;}
  static void TreeBounds(float v,out float l,out float r){l=1;r=0;for(int i=0,j=Cypress.Length-1;i<Cypress.Length;j=i++){var a=Cypress[i];var b=Cypress[j];if((a.y>v)!=(b.y>v)){float x=Mathf.Lerp(a.x,b.x,(v-a.y)/(b.y-a.y));l=Mathf.Min(l,x);r=Mathf.Max(r,x);}}if(l>r){l=.195f;r=.205f;}}
  static float Depth(float u,float v,int region,bool back=false)
  {
   if(region==0)return StarrySemanticSky.At(u,v).Depth;
   if(region==2){TreeBounds(v,out float l,out float r);float q=Mathf.Clamp((u-(l+r)/2)/Mathf.Max(.004f,(r-l)/2),-1,1);float radius=Mathf.Lerp(.1f,3.9f,Mathf.Clamp01(v));return 35.7f+(back?1:-1)*radius*Mathf.Sqrt(Mathf.Max(0,1-q*q));}
   if(region==1){float t=Mathf.InverseLerp(Mountain(u),.785f,v);float d=t<.23f?155-t/.23f*28+10*Mathf.Sin(u*9):t<.62f?124-(t-.23f)/.39f*26+8*Mathf.Sin(u*7):95-(t-.62f)/.38f*23+7*Mathf.Sin(u*5);return d+(back?10+Mathf.Sin(t*Mathf.PI)*8:0); }
   if(region==4)return 48+(back?1.4f:0)+Mathf.Abs(u-.566f)*60;
   if(region>=20&&region<20+SourceGroves.Length){var grove=SourceGroves[region-20];float x=(u-grove.x)*1.255f/grove.z,y=(v-grove.y)/grove.z;float section=Mathf.Sqrt(Mathf.Max(0,1-x*x-y*y));float center=11.5f/Mathf.Max(.12f,-(.139173f+(.5f-grove.y-grove.z)*.987459f));return center+(back?1:-1)*grove.z*center*.85f*section;}
   if(region>=5&&region<5+Houses.Length){var h=Houses[region-5];float q=Mathf.InverseLerp(h.y,h.w,v);float ridge=h.y+(h.w-h.y)*.38f;float roof=Mathf.Clamp01((ridge-v)/(ridge-h.y));float baseDepth=11.5f/Mathf.Max(.11f,-(.139173f+(.5f-h.w)*.987459f));float buildingDepth=Mathf.Clamp((h.z-h.x)*1.255f*baseDepth*.9f,2.2f,8);return baseDepth+roof*3+Mathf.Abs(u-(h.x+h.z)/2)*12+(back?buildingDepth:0);}
   // A real receding terrain, following the reference's compressed perspective.
   return 11.5f/Mathf.Max(.14f,-(.139173f+(.5f-v)*.987459f))+(back?3:0)+Mathf.Sin(u*28+v*9)*.35f;
  }
  static Vector3 Surface(float u,float v,int region,bool back=false){float d=Depth(u,v,region,back),front=Depth(u,v,region,false);float xy=back&&region==2?front:d;return StarryStudyBuilder.Hero+StarryStudyBuilder.HeroRotation*new Vector3((u-.5f)*1.255f*.997163f*xy,(.5f-v)*.997163f*xy,d);}
  static void Save(int region)
  {
   var mesh=batches[region];if(mesh==null||mesh.VertexCount==0)return;
   var go=mesh.Create("Measured short pigment / region "+region+" / "+meshID,root,mesh.HasFlow?flowMaterial:material);var marker=go.AddComponent<RibbonGeometry>();marker.Role=region>=20&&region<20+SourceGroves.Length?"Reference grove "+(region-20):region>=320?"Extended landscape "+region:region>=200?StarrySemanticSky.Name(region-200):"Reference fitted volume "+region;marker.StrokeCount=mesh.RibbonCount;marker.LongestStrokeLength=mesh.LongestRibbonLength;
   var filter=go.GetComponent<MeshFilter>();var m=filter.sharedMesh;m.hideFlags=HideFlags.None;string path=Root+"/Fitted-"+(meshID++).ToString("D3")+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(previous==null)AssetDatabase.CreateAsset(m,path);else{SculptedStarrySky.OverwriteMesh(m,previous);UnityEngine.Object.DestroyImmediate(m);filter.sharedMesh=previous;}
   total+=mesh.RibbonCount;longest=Mathf.Max(longest,mesh.LongestRibbonLength);batches[region]=new PaintMesh();counts[region]=0;
  }
  static void Stroke(float u,float v,float angle,float length,float width,Color c,int region,bool back=false,float offset=0)
  {
   var semantic=region==0?StarrySemanticSky.At(u,v):default;
   float seed=u*4711+v*3917+region*19+(back?91:0);
   if(region==0&&Hash(seed+73)>semantic.Density)return;
   if(region==0&&semantic.Feature>0){var reference=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));if(Vector2.Dot(reference,semantic.Direction)<0)reference=-reference;var coherent=Vector2.Lerp(reference,semantic.Direction,.16f).normalized;angle=Mathf.Atan2(coherent.y,coherent.x);}
   var center=Surface(u,v,region,back);float d=Depth(u,v,region,back),scale=d*.997163f;
   length=Mathf.Min(length,1.28f/scale);
   var tangent=new Vector2(Mathf.Cos(angle)/1.255f,Mathf.Sin(angle));
   var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];
   var du=Surface(u+.0002f,v,region,back)-center;var dv=Surface(u,v+.0002f,region,back)-center;var normal=Vector3.Cross(du,dv).normalized;
   if(normal.sqrMagnitude<.1f)normal=Vector3.back;
   for(int j=0;j<4;j++){float t=j/3f;float delta=(t-.5f)*length;var p=Surface(u+tangent.x*delta,v+tangent.y*delta,region,back);points[j]=p+normal*(offset+Mathf.Sin(t*Mathf.PI)*width*scale*.12f);widths[j]=width*scale*(j==0||j==3?.14f:1);colors[j]=c;}
   // Extreme boundary derivatives must not create elongated geometry.
   float arc=0;for(int j=1;j<4;j++)arc+=Vector3.Distance(points[j-1],points[j]);if(arc>1.35f){float s=1.28f/arc;for(int j=0;j<4;j++)points[j]=center+(points[j]-center)*s;}
   // Object-specific cross sections: no shared uniform depth noise across the sky.
   float spread=region==0?semantic.HalfDepth:region==1?.55f:region==2?.45f:region==3?.22f:.16f;
   float depthOffset=(Hash(seed)-.5f)*spread*2;
   float objectScale=WorldScale*(region!=0&&region!=2?StarryWorldLayout.LandScale:1);
   float factor=objectScale*Mathf.Max(.6f,(d+depthOffset)/d);
   var pivot=StarryStudyBuilder.Hero+(center-StarryStudyBuilder.Hero)*factor;
   var tilt=Quaternion.AngleAxis((Hash(seed+3)-.5f)*12,(points[3]-points[0]).normalized);
   for(int j=0;j<4;j++){var scaled=StarryStudyBuilder.Hero+(points[j]-StarryStudyBuilder.Hero)*factor;points[j]=pivot+tilt*(scaled-pivot);widths[j]*=factor;}
   int bucket=region==0?200+semantic.Feature:region==1?50+Mathf.Clamp(Mathf.FloorToInt(u*4)+1,0,5):region==3?60+Mathf.Clamp(Mathf.FloorToInt(u*4)+1,0,5):region;
   if(region==0&&semantic.Feature>=1&&semantic.Feature<=3){var direction=(points[3]-points[0]).normalized;var curve=(points[0]+points[3]-points[1]-points[2]).normalized*.25f;batches[bucket].SetRibbonFlow(direction,curve,WorldScale*(.6f+semantic.HalfDepth*.04f),semantic.Progress*4);}
   batches[bucket].SculptedBand(points,widths,colors,.22f,4,normal);if(++counts[bucket]>=6500)Save(bucket);
  }
  static void CompleteHouseVolumes()
  {
   var reference=new Texture2D(2,2,TextureFormat.RGB24,false);
   ImageConversion.LoadImage(reference,File.ReadAllBytes("Assets/StarryStudy/Editor/Reference/StarryNight.jpg"));
   var random=new System.Random(911);
   for(int house=0;house<Houses.Length;house++){
    var h=Houses[house];int region=house+5;
    for(int edge=0;edge<4;edge++)for(int j=0;j<30;j++)for(int row=0;row<9;row++){
     float t=(j+(float)random.NextDouble())/30,across=(row+(float)random.NextDouble())/9;
     float u=edge<2?(edge==0?h.x:h.z):Mathf.Lerp(h.x,h.z,t),v=edge<2?Mathf.Lerp(h.y,h.w,t):(edge==2?h.y:h.w);
     var front=Surface(u,v,region);var back=Surface(u,v,region,true);var center=Vector3.Lerp(front,back,across)+new Vector3((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*.35f;
     var tangent=edge<2?(Surface(u,v+.001f,region)-front).normalized:(Surface(u+.001f,v,region)-front).normalized;
     var normal=Vector3.Cross(tangent,back-front).normalized;float len=.20f+(float)random.NextDouble()*.23f;
     var color=reference.GetPixelBilinear(Mathf.Lerp((h.x+h.z)/2,u,.92f),1-Mathf.Lerp((h.y+h.w)/2,v,.92f))*.91f;
     var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];
     for(int k=0;k<4;k++){points[k]=StarryStudyBuilder.Hero+(center+tangent*((k/3f-.5f)*len)-StarryStudyBuilder.Hero)*(WorldScale*StarryWorldLayout.LandScale);widths[k]=(k==0||k==3?.012f:.09f)*(WorldScale*StarryWorldLayout.LandScale);colors[k]=color;}
     batches[17].SculptedBand(points,widths,colors,.24f,4,normal);if(++counts[17]>=6500)Save(17);
    }
   }
   UnityEngine.Object.DestroyImmediate(reference);
  }
  static Color NightColor(Vector3 n)
  {
   float az=Mathf.Atan2(n.x,n.z),el=Mathf.Asin(Mathf.Clamp(n.y,-1,1));
   float flow=.5f+.5f*Mathf.Sin(el*22+(Mathf.Sin(az*3)*3+Mathf.Sin(az*7+el*5))*Mathf.Cos(el));
   var c=Color.Lerp(new Color(.09f,.18f,.35f),new Color(.32f,.49f,.59f),flow);
   if(flow>.78f)c=Color.Lerp(c,new Color(.67f,.73f,.56f),(flow-.78f)*2);
   return c;
  }
  static void LooseMark(Vector3 center,Vector3 tangent,Vector3 normal,float length,float width,Color color,int bucket,float salt)
  {
   var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];
   var bend=Vector3.Cross(tangent,normal).normalized;
   for(int j=0;j<4;j++){float t=j/3f;points[j]=center+tangent*((t-.5f)*length)+bend*Mathf.Sin(t*Mathf.PI)*length*.08f;widths[j]=(j==0||j==3?.1f:1)*width;colors[j]=color;}
   batches[bucket].SculptedBand(points,widths,colors,.3f,4,normal);if(++counts[bucket]>=6500)Save(bucket);
  }
  static void BuildSurroundingVolume()
  {
   var random=new System.Random(188912);float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());var hero=StarryStudyBuilder.Hero;
   // A broad 3D atmosphere. Each mark has its own radius, tilt and size; there is no shell surface.
   for(int i=0;i<210000;i++){
    float az=R(-Mathf.PI,Mathf.PI),el=Mathf.Asin(R(-.78f,1));var n=new Vector3(Mathf.Sin(az)*Mathf.Cos(el),Mathf.Sin(el),Mathf.Cos(az)*Mathf.Cos(el));
    float radius=R(270,570);var localDirection=Quaternion.Inverse(StarryStudyBuilder.HeroRotation)*n;if(localDirection.z>0&&Mathf.Abs(localDirection.x/localDirection.z)<.69f&&Mathf.Abs(localDirection.y/localDirection.z)<.57f)radius=R(780,1100);var center=hero+n*radius;float current=Mathf.Sin(az*3+el*5)*.7f+Mathf.Sin(el*9)*.25f;
    var east=new Vector3(Mathf.Cos(az),0,-Mathf.Sin(az));var north=Vector3.Cross(n,east).normalized;var tangent=(east*Mathf.Cos(current)+north*Mathf.Sin(current)+n*R(-.22f,.22f)).normalized;
    int bucket=72+Mathf.Clamp((int)((az+Mathf.PI)/(Mathf.PI*2)*16),0,15)*3+Mathf.Clamp((int)((n.y+.8f)/1.8f*3),0,2);
    LooseMark(center,tangent,n,R(.7f,2.7f),R(.13f,.40f),NightColor(n)*R(.82f,1.12f),bucket,i);
   }
   // Stars are clouds of short dabs at different radii, distributed over the entire sky.
   for(int star=0;star<32;star++){
    float az=star*2.399963f+.7f,el=R(.12f,1.15f);var n=new Vector3(Mathf.Sin(az)*Mathf.Cos(el),Mathf.Sin(el),Mathf.Cos(az)*Mathf.Cos(el));
    // Keep the original front constellation legible.
    var local=Quaternion.Inverse(StarryStudyBuilder.HeroRotation)*n;if(local.z>.7f&&Mathf.Abs(local.x/local.z)<.7f&&local.y/local.z>0&&local.y/local.z<.6f)continue;
    var east=Vector3.Cross(n,Vector3.up).normalized;var up=Vector3.Cross(east,n).normalized;float radius=R(310,490),size=R(5,13);
    for(int j=0;j<480;j++){
     float a=R(0,Mathf.PI*2),r=Mathf.Sqrt(R(0,1))*size;var p=hero+n*(radius+R(-13,13))+east*(Mathf.Cos(a)*r)+up*(Mathf.Sin(a)*r);
     var tangent=(-east*Mathf.Sin(a)+up*Mathf.Cos(a)+n*R(-.25f,.25f)).normalized;var c=Color.Lerp(new Color(.41f,.58f,.61f),new Color(.95f,.81f,.33f),1-r/size);
     LooseMark(p,tangent,n,R(.6f,2.2f),R(.13f,.33f),c,122+star%8,j);
    }
   }
   BuildExtendedLandscape(random);
  }
  static void PanelMarks(Vector3 origin,Vector3 u,Vector3 v,Vector3 normal,Color color,int bucket,System.Random random,bool roof=false)
  {
   float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());float w=u.magnitude,h=v.magnitude;
   for(float y=.15f;y<h;y+=1.15f)for(float x=.2f;x<w;x+=1.35f){
    var point=origin+u*((x+R(-.18f,.18f))/w)+v*((y+R(-.14f,.14f))/h)+normal*R(-.07f,.07f);
    var direction=(roof?u.normalized*.55f+v.normalized*.84f:v.normalized*.94f+u.normalized*.28f).normalized;
    var c=color*R(.77f,1.13f);LooseMark(point,direction,normal,R(1.1f,2.6f),R(.22f,.40f),c,bucket,x+y);
   }
  }
  static void ExtendedHouse(Vector3 position,Quaternion rotation,float w,float h,float d,int bucket,System.Random random)
  {
   Vector3 V(float x,float y,float z)=>position+rotation*new Vector3(x,y,z);Vector3 D(float x,float y,float z)=>rotation*new Vector3(x,y,z);
   float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());float rise=h*R(.36f,.62f);
   var wall=Color.Lerp(new Color(.20f,.32f,.37f),new Color(.48f,.55f,.49f),R(0,1));var roof=Color.Lerp(new Color(.14f,.23f,.34f),new Color(.37f,.40f,.34f),R(0,1));
   PanelMarks(V(-w/2,0,-d/2),D(w,0,0),Vector3.up*h,D(0,0,-1),wall,bucket,random);
   PanelMarks(V(w/2,0,d/2),D(-w,0,0),Vector3.up*h,D(0,0,1),wall*.84f,bucket,random);
   PanelMarks(V(-w/2,0,d/2),D(0,0,-d),Vector3.up*h,D(-1,0,0),wall*.87f,bucket,random);
   PanelMarks(V(w/2,0,-d/2),D(0,0,d),Vector3.up*h,D(1,0,0),wall*.93f,bucket,random);
   var slope=D(w/2,rise,0);PanelMarks(V(-w/2,h,-d/2),slope,D(0,0,d),D(-.6f,.8f,0),roof,bucket+1,random,true);
   PanelMarks(V(w/2,h,d/2),D(-w/2,rise,0),D(0,0,-d),D(.6f,.8f,0),roof*.88f,bucket+1,random,true);
   for(int end=-1;end<=1;end+=2)for(float x=-w/2+.25f;x<w/2;x+=1.1f)for(float y=.2f;y<rise*(1-Mathf.Abs(x)/(w/2));y+=.9f)LooseMark(V(x,h+y,end*d/2),Vector3.up,D(0,0,end),1.3f,.29f,wall*R(.86f,1.06f),bucket,x+y);
   // Small windows, doors and roof accents remain separate short brush meshes.
   for(int side=-1;side<=1;side+=2)for(float x=-w*.32f;x<w*.4f;x+=w*.3f)for(int row=0;row<2;row++){
    float y=h*(row==0?.33f:.72f);var c=random.Next(3)==0?new Color(.88f,.76f,.30f):new Color(.055f,.12f,.19f);
    for(int line=0;line<4;line++)LooseMark(V(x,y+line*.25f,side*(d/2+.12f)),D(1,0,0),D(0,0,side),.85f,.14f,c,bucket+2,line);
   }
   for(float y=.2f;y<h*.57f;y+=.4f)LooseMark(V(0,y,-d/2-.14f),Vector3.up,D(0,0,-1),.7f,.34f,new Color(.07f,.13f,.19f),bucket+2,y);
  }
  static void BuildExtendedLandscape(System.Random random)
  {
   float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());var hero=StarryStudyBuilder.Hero;
   // Radial sectors and distance bands allow far ground to be culled independently.
   for(int i=0;i<250000;i++){
    float az=R(-Mathf.PI,Mathf.PI),r=Mathf.Sqrt(R(0,1))*StarryWorldLayout.GroundRadius,x=Mathf.Sin(az)*r,z=Mathf.Cos(az)*r;
    if(z>8&&Mathf.Abs(x/z)<.75f&&r<610)continue;
    float y=StarryWorldLayout.GroundHeight(hero.x+x,hero.z+z)+R(-.65f,.65f);var point=new Vector3(hero.x+x,y,hero.z+z);
    float lod=Mathf.Lerp(1,3.2f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(120,950,r)));float turn=.35f*Mathf.Sin(x*.012f+z*.009f);
    var direction=new Vector3(Mathf.Cos(turn),R(-.08f,.08f),Mathf.Sin(turn));var c=Color.Lerp(new Color(.10f,.22f,.29f),new Color(.38f,.46f,.32f),R(0,1));
    int sector=Mathf.Clamp((int)((az+Mathf.PI)/(Mathf.PI*2)*16),0,15),ring=r<280?0:r<650?1:2;
    LooseMark(point,direction,Vector3.up,R(.65f,2.1f)*lod,R(.15f,.40f)*lod,c,320+ring*16+sector,i);
   }
   // Whole-world mountain silhouette rather than one small island beneath the painting.
   for(int i=0;i<95000;i++){
    float az=R(-Mathf.PI,Mathf.PI),r=R(1120,1340),height=160+Mathf.Sin(az*5)*45+Mathf.Sin(az*11)*24,t=R(0,1);
    var point=hero+new Vector3(Mathf.Sin(az)*r,Mathf.Lerp(-45,height,t)+R(-2,2),Mathf.Cos(az)*r);
    var tangent=new Vector3(Mathf.Cos(az),Mathf.Cos(az*5)*.25f,-Mathf.Sin(az));var c=Color.Lerp(new Color(.12f,.19f,.35f),new Color(.29f,.41f,.47f),R(0,1));
    LooseMark(point,tangent,new Vector3(Mathf.Sin(az),0,Mathf.Cos(az)),R(2,6),R(.4f,1.4f),c,380+Mathf.Clamp((int)((az+Mathf.PI)/(Mathf.PI*2)*16),0,15),i);
   }
   for(int district=0;district<StarryWorldLayout.Districts.Length;district++){
    var anchor=StarryWorldLayout.Districts[district];var center=new Vector3(anchor.x,0,anchor.y);float radius=center.magnitude,az=Mathf.Atan2(center.x,center.z);
    for(int house=0;house<10;house++){
     int col=house%5,row=house/5;var offset=new Vector3((col-2)*R(51,62),0,(row-.5f)*R(75,92));var p=hero+center+Quaternion.Euler(0,az*Mathf.Rad2Deg+28,0)*offset;p.y=StarryWorldLayout.GroundHeight(p.x,p.z);
     ExtendedHouse(p,Quaternion.Euler(0,az*Mathf.Rad2Deg+R(-18,18),0),R(25,43),R(16,27),R(23,38),400+district*3,random);
    }
    for(int tree=0;tree<22;tree++){
     float tx=(tree%11-5)*20+R(-3,3),tz=(tree<11?-150:150)+R(-12,12);var p=hero+center+Quaternion.Euler(0,az*Mathf.Rad2Deg+28,0)*new Vector3(tx,0,tz);p.y=StarryWorldLayout.GroundHeight(p.x,p.z);float h=R(10,22),w=R(4,7);
     for(int dab=0;dab<320;dab++){
      float yaw=R(0,Mathf.PI*2),t=R(0,1),rr=Mathf.Sqrt(R(0,1))*w*Mathf.Sin(t*Mathf.PI);var q=p+new Vector3(Mathf.Cos(yaw)*rr,t*h,Mathf.Sin(yaw)*rr);var tangent=new Vector3(-Mathf.Sin(yaw)*.3f,1,Mathf.Cos(yaw)*.3f).normalized;
      LooseMark(q,tangent,new Vector3(Mathf.Cos(yaw),.2f,Mathf.Sin(yaw)),R(.8f,2.1f),R(.20f,.38f),Color.Lerp(new Color(.07f,.20f,.20f),new Color(.27f,.39f,.28f),R(0,1)),440+district,dab);
     }
    }
    for(int dab=0;dab<1700;dab++){
     float t=R(0,1);float curve=Mathf.Sin(t*Mathf.PI)*35;var p=hero+Vector3.Lerp(new Vector3(60,0,340),center,t)+new Vector3(Mathf.Cos(az)*(curve+R(-4,4)),0,-Mathf.Sin(az)*(curve+R(-4,4)));p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+.13f;
     if(p.z>hero.z&&Mathf.Abs(p.x/(p.z-hero.z+.01f))<.75f&&new Vector2(p.x-hero.x,p.z-hero.z).magnitude<610)continue;
     LooseMark(p,new Vector3(Mathf.Sin(az),0,Mathf.Cos(az)),Vector3.up,R(.8f,2.4f),R(.15f,.36f),Color.Lerp(new Color(.21f,.31f,.32f),new Color(.49f,.49f,.34f),R(0,1)),460+district,dab);
    }
   }
  }
  [MenuItem("Starry Study/Fit short mesh painting to reference")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before fitting scene.");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/StarryStudy/StarryNightPreview.unity")EditorSceneManager.OpenScene("Assets/StarryStudy/StarryNightPreview.unity");
   var old=GameObject.Find("Reference fitted short-stroke world");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
   var legacy=GameObject.Find("Starry Night - sculpted world");if(legacy!=null)UnityEngine.Object.DestroyImmediate(legacy);
   root=new GameObject("Reference fitted short-stroke world").transform;Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   string mp=Root+"/Measured pigment.mat";material=AssetDatabase.LoadAssetAtPath<Material>(mp);if(material==null){material=new Material(Shader.Find("StarryStudy/MeasuredPigment"));AssetDatabase.CreateAsset(material,mp);}material.shader=Shader.Find("StarryStudy/MeasuredPigment");material.SetFloat("_MotionEnabled",0);
   string fp=Root+"/Flowing pigment.mat";flowMaterial=AssetDatabase.LoadAssetAtPath<Material>(fp);if(flowMaterial==null){flowMaterial=new Material(material);AssetDatabase.CreateAsset(flowMaterial,fp);}flowMaterial.shader=material.shader;flowMaterial.SetFloat("_MotionEnabled",1);EditorUtility.SetDirty(material);EditorUtility.SetDirty(flowMaterial);
   batches=new PaintMesh[BucketCount];counts=new int[BucketCount];for(int i=0;i<BucketCount;i++)batches[i]=new PaintMesh();meshID=total=0;longest=0;
   using(var reader=new BinaryReader(File.OpenRead("Assets/StarryStudy/Editor/Reference/BrushPlan.bytes"))){int count=reader.ReadInt32();for(int i=0;i<count;i++){
    float u=reader.ReadSingle(),v=reader.ReadSingle(),angle=reader.ReadSingle(),len=reader.ReadSingle(),width=reader.ReadSingle();var c=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());var skyColor=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());float skyAngle=reader.ReadSingle();var landColor=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());float landAngle=reader.ReadSingle(),regionHint=reader.ReadSingle();int region=regionHint== -3?3:regionHint== -2?1:regionHint<0?0:regionHint>0?2:Region(u,v);
    if((region==1||region==3)&&c.g>c.r*.85f&&c.g>c.b*.68f){int grove=GroveAt(u,v);if(grove>=0)region=20+grove;}
    if(regionHint<0)continue; // Replaced by a complete surrounding world, without rectangular extensions.
    float edge=Mathf.Min(u,1-u);if(region==0)edge=Mathf.Min(edge,Mathf.Min(v,1-v));
    float coverage=Mathf.SmoothStep(0,1,Mathf.Clamp01((edge+.005f)/.105f));
    if(Hash(i*.37f+13)>coverage)continue;
    float fringe=1-Mathf.Clamp01(edge/.14f);
    // Density feathers at the edge; positions continue to follow the measured brush field.
    Stroke(u,v,angle,len,width*.82f,c,region,false,0);
    if(region!=0)Stroke(u,v,angle,len,width*.62f,c*.88f,region,true);
    // Continue the sky behind the foreground instead of leaving a cut-out hole after movement.
    if(region!=0)Stroke(u,v,skyAngle,len,width*.85f,skyColor,0,false,.02f);
    if((region==2||region>=4)&&v>Mountain(u))Stroke(u,v,landAngle,len,width*.85f,landColor,3,true);
   }}
   CompleteHouseVolumes();
   BuildSurroundingVolume();
   for(int i=0;i<BucketCount;i++)Save(i);
   var controller=UnityEngine.Object.FindAnyObjectByType<StarryStudyController>();controller.HeroPosition=StarryStudyBuilder.Hero;controller.HeroEuler=StarryStudyBuilder.HeroRotation.eulerAngles;controller.ResetView();controller.View.rect=new Rect(0,0,1,1);controller.View.fieldOfView=controller.UseCinematicOpening&&controller.Cinematic!=null?controller.OpeningFieldOfView:53;controller.View.farClipPlane=3000;controller.View.clearFlags=CameraClearFlags.Skybox;controller.View.allowHDR=false;var cameraData=controller.View.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=false;cameraData.requiresDepthTexture=false;cameraData.requiresColorTexture=false;
   var skyboxPath=Root+"/Surrounding night.mat";var skybox=AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);if(skybox==null){skybox=new Material(Shader.Find("StarryStudy/SurroundingNight"));AssetDatabase.CreateAsset(skybox,skyboxPath);}skybox.shader=Shader.Find("StarryStudy/SurroundingNight");RenderSettings.skybox=skybox;RenderSettings.fog=false;
   StarryForegroundGround.Rebuild(false);
   StarryWorldHierarchy.Organize(false);
   PainterlyTownBuilder.Rebuild();
   StarryCinematicBuilder.Ensure();
   StarryOpeningBuilder.Configure(false);
   StarryInterfaceBuilder.Configure();
   EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);AssetDatabase.SaveAssets();
   Debug.Log($"FITTED_MESH_READY strokes={total} meshes={meshID} longest={longest:F3}; no runtime image or canvas");
  }
 }
}
#endif
