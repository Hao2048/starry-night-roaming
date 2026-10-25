#if UNITY_EDITOR
using System;
using System.IO;
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarryStudy
{
 public static class SculptedStarrySky
 {
  const string AssetRoot="Assets/StarryStudy/SkyModels";
  const string ScenePath="Assets/StarryStudy/StarryNightPreview.unity";
  static System.Random random;
  static Material cloudMaterial,lightMaterial;
  static int meshId;
  static float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
  static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
  static Vector3 P(float x,float y,float z)=>new(x,y,z);
  static Color Pigment(float value,bool warm=false)
  {
   string[] colors=warm?new[]{"A28B48","D6B65B","F2D583","F5E4A1"}:new[]{"284C79","3D7699","6FA9B5","AEC8B7"};
   float t=Mathf.Clamp01(value)*(colors.Length-1);int k=Mathf.Min(colors.Length-2,(int)t);return Color.Lerp(C(colors[k]),C(colors[k+1]),t-k);
  }
  static Transform Group(string name,Transform parent,Vector3 center,Quaternion rotation)
  {
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=center;go.transform.localRotation=rotation;return go.transform;
  }
  static Material MakeMaterial(string name,float emission)
  {
   string path=AssetRoot+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(material==null){material=new Material(Shader.Find("StarryStudy/SkySculpture"));AssetDatabase.CreateAsset(material,path);}
   material.shader=Shader.Find("StarryStudy/SkySculpture");material.SetFloat("_Emission",emission);material.SetFloat("_Relief",.8f);EditorUtility.SetDirty(material);return material;
  }
  static GameObject Save(PaintMesh mesh,string label,Transform parent,Material material)
  {
   if(mesh.LongestRibbonLength>1.4f)throw new InvalidOperationException("A continuous long ribbon was generated: "+label);
   var go=mesh.Create(label,parent,material);var marker=go.AddComponent<RibbonGeometry>();marker.Role=label;marker.StrokeCount=mesh.RibbonCount;marker.LongestStrokeLength=mesh.LongestRibbonLength;var filter=go.GetComponent<MeshFilter>();
   if(filter==null)throw new InvalidOperationException("Empty sky model: "+label);
   var generated=filter.sharedMesh;generated.hideFlags=HideFlags.None;
   foreach(var v in generated.vertices)if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))throw new InvalidOperationException("Invalid sky sculpture vertex: "+label);
   string path=AssetRoot+"/SkyMesh-"+(meshId++).ToString("D3")+".asset";
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(existing==null)AssetDatabase.CreateAsset(generated,path);
   else {OverwriteMesh(generated,existing);UnityEngine.Object.DestroyImmediate(generated);filter.sharedMesh=existing;}
   return go;
  }
  public static void OverwriteMesh(Mesh source,Mesh target)
  {
   // CopySerialized leaves stale render buffers when an asset changes topology. Reassign buffers.
   target.Clear(false);target.indexFormat=source.indexFormat;target.vertices=source.vertices;target.colors32=source.colors32;
   target.uv=source.uv;target.uv2=source.uv2;var flowDirections=new System.Collections.Generic.List<Vector4>();var flowCurves=new System.Collections.Generic.List<Vector4>();source.GetUVs(2,flowDirections);source.GetUVs(3,flowCurves);if(flowDirections.Count>0){target.SetUVs(2,flowDirections);target.SetUVs(3,flowCurves);}target.triangles=source.triangles;target.normals=source.normals;
   target.bounds=source.bounds;target.UploadMeshData(false);EditorUtility.SetDirty(target);
  }
  static Vector3 Composition(float u,float v,float depth)
  {
   float half=Mathf.Tan(53*Mathf.Deg2Rad/2);
   return StarryStudyBuilder.Hero+StarryStudyBuilder.HeroRotation*P((u-.5f)*1.255f*2*half*depth,(v-.5f)*2*half*depth,depth);
  }
  [MenuItem("Starry Study/Replace sky with 3D sculptures")]
  public static void Replace()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before replacing sky models.");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
   var world=GameObject.Find("Starry Night - sculpted world");if(world==null)throw new InvalidOperationException("The study world was not found.");
   string[] oldNames={"Curved blue pigment underpainting","Broad curved sky strokes","Fine impasto sky strokes","Night hemisphere","Sculpted sky - full surrounding composition"};
   foreach(var name in oldNames){var child=world.transform.Find(name);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);}
   Build(world.transform);
   EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();
   Debug.Log("SCULPTED_SKY_READY: old sampled sky removed; "+meshId+" texture-free 3D meshes saved.");
  }
  public static void Build(Transform world)
  {
   Directory.CreateDirectory(AssetRoot);AssetDatabase.Refresh();random=new System.Random(188909);meshId=0;
   cloudMaterial=MakeMaterial("Blue cloud pigment",.08f);lightMaterial=MakeMaterial("Star and moon pigment",.18f);
   var sky=Group("Sculpted sky - full surrounding composition",world,Vector3.zero,Quaternion.identity);
   // Every visible cloud is an independent closed sweep with substantial depth, not a sky panel.
   BackgroundBands(sky);
   FlowingBrushVolume(sky);
   // Swirls are expressed by the short-stroke direction field, without a separate continuous sculpture.
   var positions=new[]{new Vector2(.11f,.94f),new Vector2(.24f,.96f),new Vector2(.35f,.94f),new Vector2(.42f,.91f),new Vector2(.61f,.90f),new Vector2(.71f,.76f),new Vector2(.34f,.67f),new Vector2(.36f,.47f),new Vector2(.13f,.51f),new Vector2(.045f,.55f)};
   for(int k=0;k<positions.Length;k++){
    var p=positions[k];float d=R(120,180);Star("Front star "+(k+1),sky,Composition(p.x,p.y,d),k==0?1.45f:R(.65f,1.1f),k==0?6.5f:R(3.1f,5.6f));
   }
   for(int k=0;k<32;k++){
    float az=(65+k*9.7f)*Mathf.Deg2Rad;var center=P(Mathf.Sin(az)*R(145,205),R(42,150),10+Mathf.Cos(az)*R(145,205));
    Star("Surrounding star "+(k+1),sky,center,R(.35f,.85f),R(2.1f,4.1f));
   }
   Moon(sky,Composition(.91f,.83f,158),11.5f);
   AssetDatabase.SaveAssets();
  }
  static Vector3[] Curl(float rx,float ry,float depth,float turns)
  {
   const int count=150;var points=new Vector3[count];
   for(int j=0;j<count;j++){
    float t=j/(float)(count-1),a=-3.4f+t*turns*Mathf.PI*2,r=Mathf.Exp(-t*1.65f);
    points[j]=P(Mathf.Cos(a)*rx*r,Mathf.Sin(a)*ry*r,Mathf.Sin(a*.8f)*depth+Mathf.Cos(a*1.7f)*depth*.22f);
   }
   return points;
  }
  static void BackgroundBands(Transform sky)
  {
   for(int sector=0;sector<12;sector++){
    float yaw=sector*30;var root=Group("Deep blue flowing bands "+sector,sky,P(Mathf.Sin(yaw*Mathf.Deg2Rad)*225,0,Mathf.Cos(yaw*Mathf.Deg2Rad)*225+10),Quaternion.Euler(0,yaw,0));
    var bands=new PaintMesh();
    for(int band=0;band<44;band++){
     int count=66;var points=new Vector3[count];var widths=new float[count];var colors=new Color[count];
     float y=12+band*3.8f,phase=sector*.8f+band*.19f,amplitude=R(8,14),bandWidth=R(1.2f,2.4f);
     for(int j=0;j<count;j++){
      float t=j/(float)(count-1);points[j]=P((t-.5f)*151,y+Mathf.Sin(t*6.5f+phase)*amplitude,Mathf.Sin(t*4+phase)*5+Mathf.Sin(band*.7f)*9);
      widths[j]=.007f+bandWidth*(.88f+.12f*Mathf.Sin(j*.7f+band))*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.4f);
      colors[j]=Color.Lerp(C("19345D"),C("315F83"),.35f+.32f*Mathf.Sin(t*9+phase)+.12f*Mathf.Sin(band*1.9f));
     }
     bands.ShortBands(points,widths,colors,.38f,8);
    }
    Save(bands,"Closed layered background paint ribbons",root,cloudMaterial);
   }
  }
  static Vector3 Current(Vector3 point)
  {
   var flow=P(1,.18f*Mathf.Sin(point.x*.04f),.12f*Mathf.Sin(point.y*.08f));
   foreach(var center in new[]{P(0,60,0),P(36,40,0)}){
    float dx=point.x-center.x,dy=(point.y-center.y)*1.25f,r=Mathf.Sqrt(dx*dx+dy*dy);
    float power=Mathf.Exp(-r*r/(center.x==0?2100:850))*3.3f;
    flow+=P(-dy,dx,.1f*r*Mathf.Sin(point.x*.06f))*power/Mathf.Max(8,r);
   }
   return flow.normalized;
  }
  static Color CurrentPigment(Vector3 point)
  {
   float dx=point.x,dy=(point.y-60)*1.25f,r=Mathf.Sqrt(dx*dx+dy*dy),angle=Mathf.Atan2(dy,dx);
   float strength=Mathf.Exp(-r*r/2600),vortex=.5f+.5f*Mathf.Sin(r*.27f-angle*2);
   float wave=.5f+.5f*Mathf.Sin(point.y*.23f+Mathf.Sin(point.x*.025f)*3.2f);
   float value=Mathf.Lerp(wave*.48f,vortex*.9f,strength)+R(-.09f,.12f);
   var color=Color.Lerp(C("193D68"),C("609BAD"),Mathf.Clamp01(value));
   if(value>.5f)color=Color.Lerp(color,C("B6C8AC"),Mathf.InverseLerp(.5f,1,value)*.58f);
   return color;
  }
  static void FlowingBrushVolume(Transform sky)
  {
   // Analytic 3D currents, with 90 m of depth. There is no image, image coordinate grid or sky sheet.
   for(int layer=0;layer<5;layer++){
    var root=Group("Spatial brush current layer "+layer,sky,Vector3.zero,Quaternion.identity);var mesh=new PaintMesh();
    for(int dab=0;dab<8200;dab++){
     float x=R(-150,150),y=R(18,168),edge=x*x/(155*155)+(y-91)*(y-91)/(83*83);
     if(edge>.92f+Mathf.PerlinNoise(x*.035f,y*.035f)*.14f)continue;
     var center=P(x,y,112+layer*19+R(-8,8)+Mathf.Sin(x*.018f)*9+Mathf.Cos(y*.034f)*8);
     float length=R(.8f,2.7f),width=R(.1f,.29f)*Mathf.Clamp01((1.12f-edge)*3);var direction=Current(center);
     var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];var color=CurrentPigment(center);
     for(int j=0;j<4;j++){
      float t=j/3f;var p=center+direction*((t-.5f)*length);points[j]=p+Current(p)*Mathf.Sin(t*Mathf.PI)*.13f;
      widths[j]=.003f+width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.45f);colors[j]=color*(.96f+.04f*Mathf.Sin(t*Mathf.PI));
     }
     mesh.ShortBands(points,widths,colors,.33f,4);
    }
    Save(mesh,"Closed short brush ribbons in spatial flow",root,cloudMaterial);
   }
  }
  static void Cloud(string name,Transform sky,Vector3 center,Vector3 euler,float rx,float ry,float depth,float width,float turns)
  {
   var root=Group(name,sky,center,Quaternion.Euler(euler));var points=Curl(rx,ry,depth,turns);var widths=new float[points.Length];
   for(int j=0;j<points.Length;j++){float t=j/(float)(points.Length-1);widths[j]=.008f+width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.38f)*(1-.35f*t);}
   Body(root,points,widths,false,.62f);
  }
  static void Wave(string name,Transform sky,Vector3 center,Vector3 euler,float span,float rise,float depth,float width)
  {
   var root=Group(name,sky,center,Quaternion.Euler(euler));var points=new Vector3[140];var widths=new float[points.Length];
   for(int j=0;j<points.Length;j++){
    float t=j/(float)(points.Length-1);points[j]=P((t-.5f)*span,Mathf.Sin(t*7-1)*rise+Mathf.Sin(t*12)*rise*.18f,Mathf.Sin(t*5)*depth);
    widths[j]=.008f+width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.5f);
   }
   Body(root,points,widths,false,.58f);
  }
  static void Frames(Vector3[] points,out Vector3[] side,out Vector3[] normal)
  {
   side=new Vector3[points.Length];normal=new Vector3[points.Length];var previous=Vector3.forward;
   for(int j=0;j<points.Length;j++){
    var tangent=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(j-1,0)]).normalized;
    side[j]=j==0?Vector3.Cross(tangent,Vector3.forward).normalized:Quaternion.FromToRotation(previous,tangent)*side[j-1];
    if(side[j].sqrMagnitude<.1f)side[j]=Vector3.Cross(tangent,Vector3.up).normalized;
    normal[j]=Vector3.Cross(tangent,side[j]).normalized;previous=tangent;
   }
  }
  static void Body(Transform root,Vector3[] points,float[] widths,bool warm,float depthRatio)
  {
   Frames(points,out var side,out var normal);var material=warm?lightMaterial:cloudMaterial;
   var body=new PaintMesh();
   // The silhouette itself is built from overlapping broad ribbons. There is no smooth core.
   for(int strip=0;strip<12;strip++){
    float angle=strip*2*Mathf.PI/12;var path=new Vector3[points.Length];var size=new float[points.Length];var pigment=new Color[points.Length];
    for(int j=0;j<points.Length;j++){
     float turn=angle+Mathf.Sin(j*.045f+strip)*.045f;
     path[j]=points[j]+(side[j]*Mathf.Cos(turn)+normal[j]*(Mathf.Sin(turn)*depthRatio))*widths[j]*.88f;
     size[j]=.003f+widths[j]*(.23f+.05f*Mathf.Sin(j*.071f+strip));
     pigment[j]=Pigment((warm?.63f:.39f)+Mathf.Sin(strip*1.9f)*.17f+Mathf.Sin(j*.055f)*.13f,warm);
    }
    var outward=(side[points.Length/2]*Mathf.Cos(angle)+normal[points.Length/2]*Mathf.Sin(angle)).normalized;
    body.ShortBands(path,size,pigment,.24f,8,outward);
   }
   Save(body,"Broad ribbons composing the whole cloud volume",root,material);
   var ridges=new PaintMesh();
   for(int line=0;line<11;line++){
    float a=line*2*Mathf.PI/11;var path=new Vector3[points.Length];var radii=new float[points.Length];var pigment=new Color[points.Length];
    for(int j=0;j<path.Length;j++){
     float twist=.045f*Mathf.Sin(j*.12f+line),angle=a+twist;
     path[j]=points[j]+(side[j]*Mathf.Cos(angle)+normal[j]*(Mathf.Sin(angle)*depthRatio))*widths[j]*1.015f;
     radii[j]=Mathf.Min(widths[j]*.3f,R(.24f,.54f))*(.65f+.35f*Mathf.Sin(j*.08f+line));pigment[j]=Pigment(.18f+.6f*(Mathf.Sin(a)*.5f+.5f)+Mathf.Sin(j*.075f+line)*.1f,warm);
    }
    ridges.ShortBands(path,radii,pigment,.22f,8);
   }
   Save(ridges,"Raised longitudinal pigment ridges",root,material);
   var marks=new PaintMesh();
   for(int dab=0;dab<190;dab++){
    int begin=random.Next(2,points.Length-12),count=random.Next(4,11);float a=R(0,Mathf.PI*2);var path=new Vector3[count];var radii=new float[count];var pigment=new Color[count];
    float width=R(.13f,.38f),tone=R(.22f,.94f);
    for(int j=0;j<count;j++){
     int i=begin+j;path[j]=points[i]+(side[i]*Mathf.Cos(a)+normal[i]*(Mathf.Sin(a)*depthRatio))*widths[i]*1.035f;
     radii[j]=.003f+Mathf.Min(width,widths[i]*.18f)*Mathf.Sin(j*Mathf.PI/(count-1));pigment[j]=Pigment(tone,warm);
    }
    marks.ShortBands(path,radii,pigment,.4f,6);
   }
   Save(marks,"Broken impasto brush deposits",root,material);
  }
  static void Star(string name,Transform sky,Vector3 center,float radius,float haloRadius)
  {
   var root=Group(name,sky,center,Quaternion.Euler(R(-22,22),R(-25,25),R(-70,70)));var core=new PaintMesh();
   for(int strip=0;strip<14;strip++){
    float angle=strip*2*Mathf.PI/14;var corePath=new Vector3[22];var coreWidths=new float[22];var coreColors=new Color[22];
    for(int j=0;j<22;j++){
     float t=j/21f,r=radius*Mathf.Sin(t*Mathf.PI),a=angle+.25f*Mathf.Sin(t*Mathf.PI);
     corePath[j]=P(Mathf.Cos(a)*r,-radius*Mathf.Cos(t*Mathf.PI),Mathf.Sin(a)*r);
     coreWidths[j]=.002f+Mathf.Max(0,r)*.3f;coreColors[j]=Pigment(.65f+.3f*Mathf.Sin(t*Mathf.PI)+.05f*Mathf.Sin(strip),true);
    }
    core.ShortBands(corePath,coreWidths,coreColors,.28f,6,P(Mathf.Cos(angle),0,Mathf.Sin(angle)));
   }
   Save(core,"Luminous star composed of interwoven ribbons",root,lightMaterial);
   var rings=new PaintMesh();
   for(int ring=0;ring<7;ring++){
    int count=85;var points=new Vector3[count];var widths=new float[count];var colors=new Color[count];float rr=Mathf.Lerp(radius*1.3f,haloRadius,ring/6f);
    for(int j=0;j<count;j++){
     float t=j/(float)(count-1),a=t*2*Mathf.PI+ring*.6f,r=rr*(1+.035f*Mathf.Sin(a*5+ring));
     points[j]=P(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(a*2+ring*.8f)*rr*.18f+Mathf.Sin(a)*rr*.1f);
     widths[j]=.008f+rr*R(.06f,.11f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.28f);
     colors[j]=ring<3?Pigment(.53f+R(0,.32f),true):Color.Lerp(C("6B9EAA"),C("C4CB8B"),R(.1f,.9f));
    }
    rings.ShortBands(points,widths,colors,.75f,7);
   }
   Save(rings,"Spatial curling star halo",root,lightMaterial);
   HaloDabs(root,radius,haloRadius,false);
  }
  static void Moon(Transform sky,Vector3 center,float radius)
  {
   var root=Group("Sculpted crescent moon",sky,center,Quaternion.Euler(-8,-14,-22));var points=new Vector3[110];var widths=new float[110];
   for(int j=0;j<points.Length;j++){
    float t=j/(float)(points.Length-1),a=Mathf.Lerp(.35f*Mathf.PI,1.65f*Mathf.PI,t);
    points[j]=P(Mathf.Cos(a)*radius*.72f,Mathf.Sin(a)*radius*.72f,Mathf.Sin(t*Mathf.PI)*radius*.06f);
    widths[j]=.004f+radius*.32f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.72f);
   }
   Body(root,points,widths,true,.6f);
   var halo=new PaintMesh();
   for(int ring=0;ring<7;ring++){
    var path=new Vector3[100];var w=new float[path.Length];var colors=new Color[path.Length];float rr=radius*(1.05f+ring*.085f);
    for(int j=0;j<path.Length;j++){
     float t=j/(float)(path.Length-1),a=t*Mathf.PI*2;path[j]=P(Mathf.Cos(a)*rr,Mathf.Sin(a)*rr,Mathf.Sin(a*2+ring)*1.4f);
     w[j]=.006f+R(.08f,.19f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.3f);colors[j]=Color.Lerp(C("85B5AC"),C("D8D294"),R(.1f,.9f));
    }
    halo.ShortBands(path,w,colors,.75f,7);
   }
   Save(halo,"Three-dimensional moon aura",root,lightMaterial);
   HaloDabs(root,radius*.7f,radius*1.6f,true);
  }
  static void HaloDabs(Transform root,float inner,float outer,bool moon)
  {
   var mesh=new PaintMesh();int count=moon?850:220;
   for(int dab=0;dab<count;dab++){
    float angle=R(0,Mathf.PI*2),r=R(inner,outer),span=R(.07f,.24f),z=R(-outer*.22f,outer*.22f);
    var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];
    var c=Color.Lerp(Pigment(R(.45f,.95f),true),C("69A4AA"),Mathf.InverseLerp(inner,outer,r)*.8f);
    for(int j=0;j<4;j++){float t=j/3f,a=angle+span*(t-.5f);points[j]=P(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z+Mathf.Sin(a)*r*.1f);widths[j]=.003f+R(.055f,.18f)*Mathf.Sin(t*Mathf.PI);colors[j]=c;}
    mesh.ShortBands(points,widths,colors,.4f,5);
   }
   Save(mesh,"Scattered three-dimensional halo brush ribbons",root,lightMaterial);
  }
 }
}
#endif
