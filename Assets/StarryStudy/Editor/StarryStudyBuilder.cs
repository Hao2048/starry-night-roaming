#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StarryStudy
{
 public static class StarryStudyBuilder
 {
  const string Root="Assets/StarryStudy/Generated";
  static System.Random rng;
  static Transform world;
  static Material pigment,glow;
  static Camera camera;
  static int assetId;
  public static readonly Vector3 Hero=new(0,13,-35);
  public static readonly Quaternion HeroRotation=Quaternion.Euler(-8,0,0);
  static float R(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
  static Color C(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
  static Color Shift(Color c,float v=.14f)=>Color.Lerp(c,rng.Next(2)==0?C("9EAFA0"):C("101F41"),R(0,v));
  static Vector3 P(float x,float y,float z)=>new(x,y,z);
  public static float Ground(float x,float z)=>1.5f+2.8f*Mathf.Exp(-Mathf.Pow((z+17)/15,2))+.45f*Mathf.Sin(x*.14f+z*.09f)+.28f*Mathf.Sin(z*.21f);
  static GameObject Commit(PaintMesh mesh,string name,Material mat,bool collider=false,Transform parent=null)
  {
   var go=mesh.Create(name,parent==null?world:parent,mat,collider);
   var filter=go.GetComponent<MeshFilter>();if(filter!=null){var m=filter.sharedMesh;m.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(m,Root+"/Mesh"+(assetId++)+".asset");}
   return go;
  }
  static Material Material(string name,float unlit,float emission=0)
  {
   var m=new Material(Shader.Find("StarryStudy/Pigment")){name=name};m.SetFloat("_Unlit",unlit);m.SetFloat("_Emission",emission);m.SetFloat("_Bristles",unlit>.9f?.6f:.18f);
   AssetDatabase.CreateAsset(m,Root+"/"+name+".mat");return m;
  }
  [MenuItem("Starry Study/Build single painting preview")]
  public static void Build()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before rebuilding.");
   rng=new System.Random(188906);assetId=0;
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   // Delete only generated asset contents on a deliberate study rebuild.
   foreach(var file in Directory.GetFiles(Root))if(!file.EndsWith(".meta")&&!file.EndsWith(".unity"))AssetDatabase.DeleteAsset(file);
   pigment=Material("Sculpted pigment",.68f);glow=Material("Warm windows",1,.3f);
   var camObj=new GameObject("Original composition camera");camera=camObj.AddComponent<Camera>();camera.tag="MainCamera";
   camera.fieldOfView=53;camera.nearClipPlane=.12f;camera.farClipPlane=500;camera.allowHDR=true;
   camera.backgroundColor=C("142C52");camera.clearFlags=CameraClearFlags.SolidColor;
   camera.transform.SetPositionAndRotation(Hero,HeroRotation);camObj.AddComponent<AudioListener>();
   camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   world=new GameObject("Starry Night - sculpted world").transform;
   RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=C("8B9CB8");
   var sun=new GameObject("Cool moonlight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=C("BCD4E8");sun.intensity=.65f;sun.transform.rotation=Quaternion.Euler(32,-28,0);
   var volume=new GameObject("Pigment atmosphere").AddComponent<Volume>();volume.isGlobal=true;
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Atmosphere.asset");volume.sharedProfile=profile;
   var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1);bloom.intensity.Override(.08f);AssetDatabase.AddObjectToAsset(bloom,profile);
   BuildSky();BuildTerrain();BuildMountains();BuildTown();BuildCypress();RibbonWorldFinisher.ApplyTo(world);
   var controller=new GameObject("Starry Night preview").AddComponent<StarryStudyController>();controller.View=camera;controller.HeroPosition=Hero;controller.HeroEuler=HeroRotation.eulerAngles;
   
   EditorSceneManager.SaveScene(scene,"Assets/StarryStudy/StarryNightPreview.unity");AssetDatabase.SaveAssets();
   if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.AlignViewToObject(camera.transform);
   ReferenceStrokeReconstruction.Apply();
   Debug.Log("STARRY_STUDY_READY: "+assetId+" saved geometry meshes; standalone composition scene ready.");
  }
  static void BuildSky()
  {
   SculptedStarrySky.Build(world);
  }
  static void BuildTerrain()
  {
   var mesh=new PaintMesh();const int n=135;const float step=1.2f;
   for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++){
    float x=-81+ix*step,z=-58+iz*step;var c=Color.Lerp(C("243F54"),C("4C6666"),Mathf.PerlinNoise(x*.06f,z*.09f));
    mesh.Quad(P(x,Ground(x,z),z),P(x,Ground(x,z+step),z+step),P(x+step,Ground(x+step,z+step),z+step),P(x+step,Ground(x+step,z),z),c);
   }
   Commit(mesh,"Rolling ground - walkable mesh",pigment,true);
   var dabs=new PaintMesh();
   for(int j=0;j<11500;j++){
    float x=R(-70,70),z=R(-40,97),a=.3f*Mathf.Sin(x*.1f+z*.13f);float len=R(.25f,1.1f);var p=P(x,Ground(x,z)+.04f,z);var q=p+P(Mathf.Cos(a)*len,0,Mathf.Sin(a)*len);q.y=Ground(q.x,q.z)+.045f;
    Color c=Shift(j%5==0?C("79845E"):j%3==0?C("305876"):C("4F6A69"),.24f);dabs.Stroke(p,q,R(.045f,.13f),c,Vector3.up,.025f);
   }
   Commit(dabs,"Contour-following ground pigment",pigment);
   var lane=new PaintMesh();
   for(int j=0;j<260;j++){
    float z=-28+j*.31f,x=7+Mathf.Sin(z*.055f)*3,w=.9f;float z1=z+.32f,x1=7+Mathf.Sin(z1*.055f)*3;
    lane.Quad(P(x-w,Ground(x-w,z)+.06f,z),P(x1-w,Ground(x1-w,z1)+.06f,z1),P(x1+w,Ground(x1+w,z1)+.06f,z1),P(x+w,Ground(x+w,z)+.06f,z),Shift(C("657D88"),.12f));
   }
   Commit(lane,"Silver-blue village lane",pigment);
  }
  static float Ridge(float x,int layer)=>5+layer*2.4f+3*Mathf.Sin(x*.056f+layer)+1.8f*Mathf.Sin(x*.125f+1.2f)+1.1f*Mathf.Sin(x*.21f+layer)+13*Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,65,x));
  static float Hill(float x,float z,int layer){float center=66+layer*17;return Ground(x,z)+Ridge(x,layer)*Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(z-center)/17),.65f);}
  static void BuildMountains()
  {
   string[] palette={"293F70","365380","506A91"};
   for(int layer=2;layer>=0;layer--){
    var mountain=new PaintMesh();float z0=49+layer*17;var color=C(palette[layer]);
    for(int k=0;k<32;k++)for(int j=0;j<300;j++){
     float x=-180+j*1.2f,z=z0+k;mountain.Quad(P(x,Hill(x,z,layer),z),P(x,Hill(x,z+1,layer),z+1),P(x+1.2f,Hill(x+1.2f,z+1,layer),z+1),P(x+1.2f,Hill(x+1.2f,z,layer),z),Color.Lerp(color,C("203653"),.12f*Mathf.Sin(x*.13f+z*.2f)+.12f));
    }
    Commit(mountain,"Sculpted ridge "+layer,pigment);
    var marks=new PaintMesh();
    for(int j=0;j<3400;j++){
     float x=R(-93,93),z=R(z0+1,z0+17),len=R(1.4f,4.4f);var points=new Vector3[7];
     for(int k=0;k<7;k++){float xx=x+len*k/6,zz=z+.2f*k/6;points[k]=P(xx,Hill(xx,zz,layer)+.15f,zz);}
     marks.Tube(points,R(.055f,.125f),R(.025f,.07f),Shift(j%5==0?C("788B83"):j%3==0?C("1D3A6A"):C("436C91"),.18f),5);
    }
    Commit(marks,"Ridge contour paint "+layer,pigment);
   }
  }
  static void BuildTown()
  {
   int id=0;
   // Staggered neighborhoods: varied footprints, no regular grid visible from the composition camera.
   for(int row=0;row<7;row++)for(int j=0;j<12;j++){
    float x=-26+j*4.8f+R(-1.2f,1.2f),z=-3+row*7.2f+R(-1.3f,1.3f);
    if(Mathf.Abs(x-(7+Mathf.Sin(z*.055f)*3))<2.4f||x<-10&&z<13||Mathf.Abs(x-1)<4&&Mathf.Abs(z-23)<7)continue;
    House(x+5,z+10,R(2.5f,4.1f),R(1.5f,2.8f),R(2.8f,4.6f),R(-28,28),id++);
   }
   House(4.4f,23,4.4f,5.1f,7.5f,-4,id++,true);
   for(int j=0;j<85;j++){
    float x=R(-60,65),z=R(35,65);if(z<45&&Mathf.Abs(x)<27)continue;
    var mesh=new PaintMesh();float y=Ground(x,z);var c=Shift(C("294D4D"));mesh.Ellipsoid(P(x,y+1.3f,z),P(R(1,2.2f),R(1.4f,2.8f),R(1,2)),c,12,9);
    for(int k=0;k<45;k++){
     float a=R(0,6.28f),b=R(-.8f,.8f);var p=P(x+Mathf.Cos(a)*1.5f,y+1.7f+b,z+Mathf.Sin(a)*1.3f);
     mesh.Stroke(p,p+P(R(.15f,.45f),R(.05f,.3f),0),.055f,Shift(C("50797A"),.3f),Vector3.back,.025f);
    }
    Commit(mesh,"Painted orchard "+j,pigment);
   }
  }
  static void House(float x,float z,float w,float h,float depth,float yaw,int id,bool church=false)
  {
   var holder=new GameObject(church?"Church with modeled spire":"Village house "+id).transform;holder.SetParent(world,false);holder.position=P(x,Ground(x,z),z);holder.rotation=Quaternion.Euler(0,yaw,0);
   var shell=new PaintMesh();var marks=new PaintMesh();var windows=new PaintMesh();var wall=Shift(id%4==0?C("708A91"):id%3==0?C("415D78"):C("597387"),.25f);var roof=Shift(id%3==0?C("69615B"):C("294866"),.2f);
   shell.Box(P(0,h*.5f,0),P(w,h,depth),wall);
   float ridge=h+w*.36f;
   shell.Tri(P(-w/2,h,-depth/2),P(0,ridge,-depth/2),P(w/2,h,-depth/2),Color.Lerp(wall,C("C1BCA0"),.15f));
   shell.Tri(P(w/2,h,depth/2),P(0,ridge,depth/2),P(-w/2,h,depth/2),wall);
   for(int side=-1;side<=1;side+=2){
    shell.Quad(P(side*(w/2+.2f),h-.1f,-depth/2-.22f),P(side*(w/2+.2f),h-.1f,depth/2+.22f),P(0,ridge+.1f,depth/2+.22f),P(0,ridge+.1f,-depth/2-.22f),roof);
    for(int k=0;k<55;k++){
     float a=R(.03f,.95f),zz=R(-depth*.5f,depth*.5f);var p=P(side*(w/2+.2f)*(1-a),Mathf.Lerp(h-.1f,ridge+.1f,a)+.035f,zz);var q=p+P(-side*R(.1f,.32f),R(.06f,.19f),.06f);
     marks.Stroke(p,q,R(.035f,.07f),Shift(k%3==0?C("839396"):k%4==0?C("172E51"):roof,.3f),new Vector3(side,.8f,0),.025f);
    }
   }
   for(int k=0;k<45;k++){
    float xx=R(-w*.48f,w*.48f),yy=R(.2f,h-.1f);marks.Stroke(P(xx,yy,-depth/2-.026f),P(xx+R(.12f,.45f),yy+R(-.03f,.04f),-depth/2-.03f),R(.035f,.07f),Shift(k%4==0?C("324C6B"):k%3==0?C("91A49C"):wall,.24f),Vector3.back,.015f);
   }
   for(int floor=0;floor<(h>3?2:1);floor++)for(int k=0;k<2;k++){
    float xx=(k==0?-.26f:.26f)*w,yy=.9f+floor*1.4f;float ww=church?.42f:.32f;
    shell.Box(P(xx,yy,-depth/2-.035f),P(ww+.13f,.7f,.08f),C("1C3047"));
    windows.Quad(P(xx-ww/2,yy-.23f,-depth/2-.085f),P(xx-ww/2,yy+.23f,-depth/2-.085f),P(xx+ww/2,yy+.23f,-depth/2-.085f),P(xx+ww/2,yy-.23f,-depth/2-.085f),id%4==0?C("E5C75F"):C("B7B796"));
    shell.Box(P(xx,yy,-depth/2-.095f),P(.035f,.53f,.025f),C("35414B"));
   }
   shell.Box(P(.12f,.69f,-depth/2-.04f),P(.53f,1.38f,.08f),C("24364C"));
   var ink=C("263A52");
   foreach(float zz in new[]{-depth/2-.08f,depth/2+.08f}){
    shell.Tube(new[]{P(-w*.54f,h,zz),P(-w*.25f,Mathf.Lerp(h,ridge,.5f)+.05f,zz),P(0,ridge+.09f,zz),P(w*.27f,Mathf.Lerp(h,ridge,.5f),zz),P(w*.54f,h,zz)},.04f,.035f,ink,5);
   }
   foreach(float xx in new[]{-w/2,w/2})shell.Tube(new[]{P(xx,.02f,-depth/2-.03f),P(xx+.025f,h*.5f,-depth/2-.04f),P(xx,h,-depth/2-.03f)},.028f,.025f,ink,5);
   if(church){
    shell.Box(P(0,h+1.1f,-1.3f),P(1.55f,3.1f,1.7f),C("85999F"));
    shell.Tube(new[]{P(0,h+2.65f,-1.3f),P(.06f,h+5,-1.3f),P(.14f,h+10.4f,-1.3f)},1.05f,.015f,C("47637C"),4);
    shell.Box(P(.14f,h+10.9f,-1.3f),P(.07f,1.15f,.07f),C("9AA7A2"));shell.Box(P(.14f,h+11.1f,-1.3f),P(.55f,.07f,.07f),C("9AA7A2"));
   }else if(id%3==0)shell.Box(P(w*.25f,ridge-.15f,depth*.2f),P(.4f,1,.45f),C("606F7D"));
   Commit(shell,"Walls, roof, frames and chimney",pigment,true,holder);Commit(marks,"Raised roof and wall paint",pigment,false,holder);Commit(windows,"Warm small windows",glow,false,holder);
  }
  static void BuildCypress()
  {
   var tree=new GameObject("Foreground cypress - branching sculpture").transform;tree.SetParent(world,false);tree.position=P(-8.2f,Ground(-8.2f,-11),-11);
   var body=new PaintMesh();var strokes=new PaintMesh();
   for(int branch=0;branch<25;branch++){
    float height=branch==0?24.5f:branch>20?R(5,11):R(4,18.7f),angle=R(0,6.28f),radius=branch==0?2.1f:R(.45f,1.05f),spread=branch==0?0:R(.3f,3.4f);
    Vector3 Center(float t)=>P((branch>20?3+Mathf.Sin(t*4)*2.3f:Mathf.Cos(angle)*spread*(1-t*.45f))+Mathf.Sin(t*7+branch)*.47f*t,t*height,Mathf.Sin(angle)*spread*(1-t*.45f)+Mathf.Sin(t*4)*.16f);
    float Width(float t)=>radius*Mathf.Pow(1-t,.65f)*(.8f+.18f*Mathf.Sin(t*15+branch));
    for(int j=0;j<35;j++)for(int k=0;k<16;k++){
     float t=j/35f,t1=(j+1)/35f,a=k*Mathf.PI/8,b=(k+1)*Mathf.PI/8;var p=Center(t);var q=Center(t1);float w=Width(t),w1=Width(t1);
     body.Quad(p+P(Mathf.Cos(a)*w,0,Mathf.Sin(a)*w),q+P(Mathf.Cos(a)*w1,0,Mathf.Sin(a)*w1),q+P(Mathf.Cos(b)*w1,0,Mathf.Sin(b)*w1),p+P(Mathf.Cos(b)*w,0,Mathf.Sin(b)*w),Color.Lerp(k%4==0?C("343E33"):k%3==0?C("173D39"):C("18362F"),C("233C37"),.15f+.1f*Mathf.Sin(t*8)));
    }
    for(int k=0;k<300;k++){
     float t=R(.02f,.93f),t1=Mathf.Min(.99f,t+R(.025f,.13f)),a=R(0,6.28f);var n=P(Mathf.Cos(a),0,Mathf.Sin(a));
     var p=Center(t)+n*(Width(t)+.045f);var q=Center(t1)+n*(Width(t1)+.045f);
     strokes.Stroke(p,q,R(.025f,.065f),Shift(k%7==0?C("7C674C"):k%4==0?C("507B64"):k%3==0?C("102D30"):C("2C574B"),.2f),n,.03f);
    }
   }
   Commit(body,"Flame-shaped modeled branches",pigment,true,tree);Commit(strokes,"Vertical cypress pigment ridges",pigment,false,tree);
  }
  public static void BuildPlayer()
  {
   PlayerSettings.defaultScreenWidth=1255;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.productName="星空漫游";EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/StarryStudy/StarryNightPreview.unity",true)};PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
   Directory.CreateDirectory("Builds/StarryNight");
   var report=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/StarryStudy/StarryNightPreview.unity"},locationPathName="Builds/StarryNight/StarryNight.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception(report.summary.result.ToString());
  }
 }
}
#endif
