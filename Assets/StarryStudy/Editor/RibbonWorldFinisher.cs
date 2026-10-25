#if UNITY_EDITOR
using System;
using System.IO;
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarryStudy
{
 public static class RibbonWorldFinisher
 {
  const string Root="Assets/StarryStudy/RibbonModels";
  static System.Random random;
  static Material material,windowMaterial;
  static int meshId;
  static float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
  static Vector3 P(float x,float y,float z)=>new(x,y,z);
  static Color C(string h){ColorUtility.TryParseHtmlString("#"+h,out var c);return c;}
  static Color Pick(string[] palette)=>C(palette[random.Next(palette.Length)]);
  static readonly string[] Earth={"243F57","35586B","4C7181","607F80","7A8D7B"};
  static readonly string[] Hills={"203D68","2F527A","3C6991","527F9C","71949C","5E778A"};
  static readonly string[] Tree={"12342F","1B453B","2E5D48","3E7058","526F51","6C6041","315B4C"};
  static GameObject Save(PaintMesh paint,string name,Transform parent,Material pigment=null)
  {
   if(paint.LongestRibbonLength>1.4f)throw new InvalidOperationException("A continuous long ribbon was generated: "+name);
   var go=paint.Create(name,parent,pigment==null?material:pigment);var filter=go.GetComponent<MeshFilter>();if(filter==null)return go;
   var marker=go.AddComponent<RibbonGeometry>();marker.Role=name;marker.StrokeCount=paint.RibbonCount;marker.LongestStrokeLength=paint.LongestRibbonLength;
   var mesh=filter.sharedMesh;mesh.hideFlags=HideFlags.None;string path=Root+"/RibbonMesh-"+(meshId++).ToString("D3")+".asset";
   var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old==null)AssetDatabase.CreateAsset(mesh,path);else {SculptedStarrySky.OverwriteMesh(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);filter.sharedMesh=old;}
   return go;
  }
  static void Band(PaintMesh mesh,Vector3[] points,float width,Color color,float thickness=.18f,Vector3 normalHint=default,int sides=6)
  {
   var widths=new float[points.Length];var colors=new Color[points.Length];
   for(int j=0;j<points.Length;j++){
    float t=j/(float)(points.Length-1);widths[j]=.002f+width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.36f)*(1+.07f*Mathf.Sin(j*2.8f));
    colors[j]=color*(.96f+.04f*Mathf.Sin(j*.9f));
   }
   mesh.ShortBands(points,widths,colors,thickness,sides,normalHint);
  }
  static void Line(PaintMesh mesh,Vector3 a,Vector3 b,float width,Color color,Vector3 normal,float arch=.012f)
  {
   var points=new Vector3[5];for(int j=0;j<5;j++){float t=j/4f;points[j]=Vector3.Lerp(a,b,t)+normal*(Mathf.Sin(t*Mathf.PI)*arch);}
   Band(mesh,points,width,color,.22f,normal);
  }
  static Material GetMaterial(string name,float emission)
  {
   var result=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
   if(result==null){result=new Material(Shader.Find("StarryStudy/SkySculpture"));AssetDatabase.CreateAsset(result,Root+"/"+name+".mat");}
   result.shader=Shader.Find("StarryStudy/SkySculpture");result.SetFloat("_Emission",emission);result.SetFloat("_Relief",.8f);EditorUtility.SetDirty(result);return result;
  }
  [MenuItem("Starry Study/Apply unified 3D paint ribbons")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
   var world=GameObject.Find("Starry Night - sculpted world");if(world==null)throw new InvalidOperationException("Starry Night world is not open.");
   ApplyTo(world.transform);EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();
   Debug.Log("PURE_RIBBON_WORLD_READY: "+meshId+" visible ribbon meshes; reference surfaces hidden.");
  }
  public static void ApplyTo(Transform world)
  {
   random=new System.Random(188913);meshId=0;Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   material=GetMaterial("Directional paint ribbons",.05f);windowMaterial=GetMaterial("Warm window ribbons",.85f);
   var previous=world.Find("Land and ridge paint ribbons");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   var land=new GameObject("Land and ridge paint ribbons").transform;land.SetParent(world,false);
   GroundBands(land);RidgeBands(land);RoadBands(land);SpatialPigments(land);
   foreach(var tr in world.GetComponentsInChildren<Transform>()){
    if(tr==null)continue;
    if(tr.name.StartsWith("Village house ")||tr.name=="Church with modeled spire")HouseBands(tr);
    if(tr.name=="Foreground cypress - branching sculpture")CypressBands(tr);
    if(tr.name.StartsWith("Painted orchard "))OrchardBands(tr);
   }
   foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>())renderer.enabled=renderer.GetComponent<RibbonGeometry>()!=null;
   AssetDatabase.SaveAssets();
  }
  static Vector3 GroundNormal(float x,float z)=>P(StarryStudyBuilder.Ground(x-.1f,z)-StarryStudyBuilder.Ground(x+.1f,z),.2f,StarryStudyBuilder.Ground(x,z-.1f)-StarryStudyBuilder.Ground(x,z+.1f)).normalized;
  static void GroundBands(Transform parent)
  {
   var large=new PaintMesh();var medium=new PaintMesh();var fine=new PaintMesh();
   for(float z=-55;z<104;z+=1.35f)for(float x=-80;x<81;x+=5.4f){
    float xx=x+R(-1.4f,1.4f),zz=z+R(-.2f,.2f),len=R(6.1f,8.5f);var points=new Vector3[7];
    for(int k=0;k<7;k++){float px=xx+len*k/6,pz=zz+Mathf.Sin(k*.45f+z*.08f)*.3f;points[k]=P(px,StarryStudyBuilder.Ground(px,pz)+.02f,pz);}
    Band(large,points,R(.9f,1.2f),Pick(Earth)*R(.68f,.85f),.15f,GroundNormal(xx,zz));
   }
   for(int j=0;j<6900;j++){
    float x=R(-79,83),z=R(-54,104),len=R(1.3f,4.2f),a=Mathf.Sin(x*.09f+z*.1f)*.4f;var points=new Vector3[6];
    for(int k=0;k<6;k++){float xx=x+Mathf.Cos(a)*len*k/5,zz=z+Mathf.Sin(a)*len*k/5;points[k]=P(xx,StarryStudyBuilder.Ground(xx,zz)+R(.10f,.15f),zz);}
    Band(medium,points,R(.17f,.43f),Pick(Earth),.18f,GroundNormal(x,z));
   }
   for(int j=0;j<6500;j++){
    float x=R(-75,79),z=R(-48,98),len=R(.35f,1.15f);var points=new Vector3[4];
    for(int k=0;k<4;k++){float xx=x+len*k/3,zz=z+.13f*k/3;points[k]=P(xx,StarryStudyBuilder.Ground(xx,zz)+.18f,zz);}
    Band(fine,points,R(.035f,.12f),Pick(Earth)*R(.9f,1.2f),.24f,GroundNormal(x,z),4);
   }
   Save(large,"Large ribbons composing the ground",parent);Save(medium,"Medium directional ground ribbons",parent);Save(fine,"Fine scattered ground pigments",parent);
  }
  static float Hill(float x,float z,int layer)
  {
   float ridge=5+layer*2.4f+3*Mathf.Sin(x*.056f+layer)+1.8f*Mathf.Sin(x*.125f+1.2f)+1.1f*Mathf.Sin(x*.21f+layer)+13*Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,65,x));
   float center=66+layer*17;return StarryStudyBuilder.Ground(x,z)+ridge*Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(z-center)/17),.65f);
  }
  static Vector3 HillNormal(float x,float z,int layer)=>P(Hill(x-.1f,z,layer)-Hill(x+.1f,z,layer),.2f,Hill(x,z-.1f,layer)-Hill(x,z+.1f,layer)).normalized;
  static void RidgeBands(Transform parent)
  {
   for(int layer=0;layer<3;layer++){
    var large=new PaintMesh();var medium=new PaintMesh();var fine=new PaintMesh();float begin=49+layer*17;
    for(float z=begin+.15f;z<begin+33;z+=1.2f)for(float x=-180;x<180;x+=7.8f){
     float px=x+R(-1,1),pz=z+R(-.15f,.15f),len=R(8.5f,12);var points=new Vector3[8];
     for(int k=0;k<8;k++){float xx=px+len*k/7,zz=pz+.12f*Mathf.Sin(k*.7f);points[k]=P(xx,Hill(xx,zz,layer)+.025f,zz);}
     Band(large,points,R(.83f,1.15f),Pick(Hills)*R(.68f,.84f),.13f,HillNormal(px,pz,layer));
    }
    for(int j=0;j<2700;j++){
     float x=R(-178,178),z=R(begin+.4f,begin+31.5f),len=R(1.6f,5.6f);var points=new Vector3[7];
     for(int k=0;k<7;k++){float xx=x+len*k/6,zz=z+.15f*k/6;points[k]=P(xx,Hill(xx,zz,layer)+.14f,zz);}
     Band(medium,points,R(.2f,.54f),Pick(Hills),.19f,HillNormal(x,z,layer));
    }
    for(int j=0;j<1500;j++){
     float x=R(-176,176),z=R(begin+1,begin+30),len=R(.5f,1.8f);var points=new Vector3[4];
     for(int k=0;k<4;k++){float xx=x+len*k/3;points[k]=P(xx,Hill(xx,z,layer)+.21f,z);}
     Band(fine,points,R(.045f,.14f),Pick(Hills)*R(.95f,1.15f),.22f,HillNormal(x,z,layer),4);
    }
    Save(large,"Large ribbons composing ridge "+layer,parent);Save(medium,"Middle contour ribbons "+layer,parent);Save(fine,"Small ridge pigment strokes "+layer,parent);
   }
  }
  static void RoadBands(Transform parent)
  {
   var mesh=new PaintMesh();var path=new Vector3[160];
   for(int j=0;j<path.Length;j++){float z=-28+j*.5f,x=7+Mathf.Sin(z*.055f)*3;path[j]=P(x,StarryStudyBuilder.Ground(x,z)+.2f,z);}
   Band(mesh,path,.82f,C("526C7F"),.14f,Vector3.up);
   for(int j=0;j<340;j++){
    float z=R(-28,51),x=7+Mathf.Sin(z*.055f)*3+R(-.64f,.64f);var a=P(x,StarryStudyBuilder.Ground(x,z)+.29f,z);var b=P(x+R(.1f,.38f),StarryStudyBuilder.Ground(x,z+.3f)+.29f,z+.3f);
    Line(mesh,a,b,R(.04f,.12f),j%4==0?C("9BA99E"):C("6E8996"),Vector3.up);
   }
   Save(mesh,"Village lane composed of paint ribbons",parent);
  }
  static void SpatialPigments(Transform parent)
  {
   var mesh=new PaintMesh();
   for(int j=0;j<850;j++){
    float x=R(-45,45),z=R(-24,64),height=R(3,22);var a=P(x,StarryStudyBuilder.Ground(x,z)+height,z);
    var direction=P(1,.2f*Mathf.Sin(x*.06f+z*.08f),.2f).normalized;float length=R(.22f,1.2f);
    Line(mesh,a,a+direction*length,R(.025f,.075f),j%17==0?C("B4AD76"):Pick(Earth)*R(.75f,1.1f),Vector3.forward,.035f);
   }
   Save(mesh,"Small spatial paint fragments around the town",parent);
  }
  static void Panel(PaintMesh mesh,Vector3 origin,Vector3 axisU,Vector3 axisV,float width,float height,Vector3 normal,Color baseColor)
  {
   // Broad vertical ribbons establish the form. Shorter horizontal ribbons build broken paint.
   for(float u=.12f;u<width+.12f;u+=.42f){
    var path=new Vector3[7];for(int k=0;k<7;k++){float t=k/6f;path[k]=origin+axisU*(u+Mathf.Sin(t*5+u)*.025f)+axisV*(t*height)+normal*(.015f+Mathf.Sin(t*Mathf.PI)*.025f);}
    Band(mesh,path,R(.29f,.37f),baseColor*R(.73f,.92f),.16f,normal);
   }
   for(float v=.07f;v<height+.03f;v+=R(.14f,.23f)){
    float u=-R(.03f,.18f);
    while(u<width){
     float len=Mathf.Min(R(.65f,1.5f),width-u+.05f);if(len<.08f)break;var path=new Vector3[5];
     for(int k=0;k<5;k++){float t=k/4f;path[k]=origin+axisU*(u+len*t)+axisV*(v+Mathf.Sin(t*4+v)*.022f)+normal*(.045f+Mathf.Sin(t*Mathf.PI)*R(.015f,.035f));}
     var color=Color.Lerp(baseColor,random.Next(4)==0?C("8FA7A5"):C("314C67"),R(.08f,.28f));
     Band(mesh,path,R(.11f,.16f),color,.17f,normal);u+=len*R(.77f,.91f);
    }
   }
  }
  static void BoxSides(PaintMesh mesh,Vector3 center,Vector3 size,Color color)
  {
   var min=center-size*.5f;
   Panel(mesh,min,Vector3.right,Vector3.up,size.x,size.y,Vector3.back,color);
   Panel(mesh,min+P(size.x,0,size.z),Vector3.left,Vector3.up,size.x,size.y,Vector3.forward,color);
   Panel(mesh,min+P(0,0,size.z),Vector3.back,Vector3.up,size.z,size.y,Vector3.left,color*.83f);
   Panel(mesh,min+P(size.x,0,0),Vector3.forward,Vector3.up,size.z,size.y,Vector3.right,color*.94f);
  }
  static void HouseBands(Transform house)
  {
   foreach(var name in new[]{"Surface paint ribbons","Window ribbon light"}){var old=house.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
   var source=house.Find("Walls, roof, frames and chimney");if(source==null)return;
   var original=source.GetComponent<MeshFilter>().sharedMesh;var verts=original.vertices;var bounds=new Bounds(verts[0],Vector3.zero);for(int j=1;j<24;j++)bounds.Encapsulate(verts[j]);
   float w=bounds.size.x,h=bounds.size.y,depth=bounds.size.z,ridge=h+w*.36f;bool church=house.name.Contains("Church");
   var paint=new PaintMesh();var light=new PaintMesh();
   Color wall=church?C("718C96"):Color.Lerp(C("4A6D84"),C("758D92"),R(0,.85f));Color roof=Color.Lerp(C("2D4D6C"),C("6C6B66"),R(0,.7f));
   BoxSides(paint,P(0,h*.5f,0),P(w,h,depth),wall);
   var leftSlope=P(w/2+.2f,ridge-h+.2f,0);var rightSlope=P(-w/2-.2f,ridge-h+.2f,0);
   Panel(paint,P(-w/2-.2f,h-.1f,-depth/2-.22f),Vector3.forward,leftSlope.normalized,depth+.44f,leftSlope.magnitude,P(-leftSlope.y,leftSlope.x,0).normalized,roof);
   Panel(paint,P(w/2+.2f,h-.1f,depth/2+.22f),Vector3.back,rightSlope.normalized,depth+.44f,rightSlope.magnitude,P(rightSlope.y,-rightSlope.x,0).normalized,roof*.85f);
   foreach(int side in new[]{-1,1})for(float y=h+.03f;y<ridge;y+=.16f){
    float half=w*.5f*(1-(y-h)/(ridge-h))+.02f;Line(paint,P(-half,y,side*(depth/2+.015f)),P(half,y,side*(depth/2+.015f)),.13f,wall*R(.86f,1.04f),Vector3.forward*side);
   }
   Line(paint,P(0,ridge+.13f,-depth/2-.3f),P(0,ridge+.13f,depth/2+.3f),.07f,C("536F84"),Vector3.up);
   // The door, frame and glass are all short closed ribbons, without any rendered quads.
   for(int j=0;j<6;j++)Line(paint,P(-.15f+j*.10f,.03f,-depth/2-.09f),P(-.15f+j*.10f,1.38f,-depth/2-.09f),.065f,C("1B3046"),Vector3.back);
   var windowSource=house.Find("Warm small windows");if(windowSource!=null){
    var wm=windowSource.GetComponent<MeshFilter>().sharedMesh;var p=wm.vertices;
    for(int j=0;j+3<p.Length;j+=4){
     var center=(p[j]+p[j+2])*.5f;float ww=(p[j+3]-p[j]).magnitude,wh=(p[j+1]-p[j]).magnitude;float z=center.z-.035f;
     foreach(int sign in new[]{-1,1}){
      Line(paint,P(center.x-ww*.65f,center.y+sign*wh*.62f,z),P(center.x+ww*.65f,center.y+sign*wh*.62f,z),.055f,C("172B3E"),Vector3.back);
      Line(paint,P(center.x+sign*ww*.65f,center.y-wh*.62f,z),P(center.x+sign*ww*.65f,center.y+wh*.62f,z),.045f,C("172B3E"),Vector3.back);
     }
     for(int row=0;row<4;row++){float y=center.y-wh*.41f+row*wh*.27f;Line(light,P(center.x-ww*.49f,y,z-.025f),P(center.x+ww*.49f,y,z-.025f),wh*.18f,row%2==0?C("E4C370"):C("BDA968"),Vector3.back);}
     Line(paint,P(center.x,center.y-wh*.6f,z-.04f),P(center.x,center.y+wh*.6f,z-.04f),.018f,C("314758"),Vector3.back);
    }
   }
   if(church){
    BoxSides(paint,P(0,h+1.1f,-1.3f),P(1.55f,3.1f,1.7f),C("6A8495"));
    for(int j=0;j<24;j++){
     float a=j*Mathf.PI*2/24;var path=new Vector3[20];var sizes=new float[20];var colors=new Color[20];
     for(int k=0;k<20;k++){float t=k/19f,r=1.05f*Mathf.Pow(1-t,.9f);path[k]=P(Mathf.Cos(a)*r+.14f*t,h+2.65f+t*7.75f,-1.3f+Mathf.Sin(a)*r);sizes[k]=.003f+.22f*Mathf.Pow(1-t,.8f);colors[k]=Color.Lerp(C("365A79"),C("89A3AC"),(j%5)/5f);}
     paint.ShortBands(path,sizes,colors,.24f,6,P(Mathf.Cos(a),0,Mathf.Sin(a)));
    }
    Line(paint,P(.14f,h+10.35f,-1.3f),P(.14f,h+11.5f,-1.3f),.04f,C("A4B5AB"),Vector3.back);
    Line(paint,P(-.14f,h+11.1f,-1.3f),P(.42f,h+11.1f,-1.3f),.035f,C("A4B5AB"),Vector3.back);
   }else {
    string suffix=house.name.Substring("Village house ".Length);if(int.TryParse(suffix,out int id)&&id%3==0)BoxSides(paint,P(w*.25f,ridge-.15f,depth*.2f),P(.4f,1,.45f),C("566D7B"));
   }
   Save(paint,"Surface paint ribbons",house);Save(light,"Window ribbon light",house,windowMaterial);
  }
  static void CypressBands(Transform tree)
  {
   var old=tree.Find("Vertical sculpted paint ribbons");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var source=tree.Find("Flame-shaped modeled branches");if(source==null)return;
   var mesh=source.GetComponent<MeshFilter>().sharedMesh;var verts=mesh.vertices;var normals=mesh.normals;var paint=new PaintMesh();
   const int rows=35,columns=16,perBranch=rows*columns*4;int branches=verts.Length/perBranch;
   for(int branch=0;branch<branches;branch++)for(int col=0;col<columns;col++){
    var large=new Vector3[rows+1];var width=new float[rows+1];var colors=new Color[rows+1];var shade=Pick(Tree)*R(.65f,.88f);
    for(int row=0;row<rows;row++){
     int n=branch*perBranch+row*columns*4+col*4;large[row]=(verts[n]+verts[n+3])*.5f+normals[n]*.025f;width[row]=(verts[n]-verts[n+3]).magnitude*.64f+.003f;colors[row]=shade;
     if(row==rows-1){large[row+1]=(verts[n+1]+verts[n+2])*.5f+normals[n]*.025f;width[row+1]=.003f;colors[row+1]=shade;}
    }
    int first=branch*perBranch+col*4;paint.ShortBands(large,width,colors,.19f,6,normals[first]);
    for(int start=0;start<rows-3;start+=random.Next(4,8)){
     int count=Mathf.Min(random.Next(6,14),rows-start);var points=new Vector3[count];
     for(int k=0;k<count;k++){int n=branch*perBranch+(start+k)*columns*4+col*4;points[k]=(verts[n]+verts[n+3])*.5f+normals[n]*.085f;}
     Band(paint,points,R(.07f,.18f)*(1-start/(float)rows*.6f),Pick(Tree),.23f,normals[first]);
    }
   }
   Save(paint,"Vertical sculpted paint ribbons",tree);
  }
  static void OrchardBands(Transform tree)
  {
   var old=tree.Find("Curled foliage ribbons");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var filter=tree.GetComponent<MeshFilter>();if(filter==null)return;var bounds=filter.sharedMesh.bounds;var center=bounds.center;var radii=bounds.extents;var mesh=new PaintMesh();
   string[] palette={"193D40","2F5A54","46766A","689387"};
   for(int row=0;row<22;row++){
    float latitude=Mathf.Lerp(-1.5f,1.5f,row/21f),phase=R(0,6.28f);var points=new Vector3[42];
    for(int k=0;k<points.Length;k++){float t=k/(float)(points.Length-1),a=phase+t*Mathf.PI*2,b=latitude+Mathf.Sin(a*2+row)*.035f;points[k]=center+P(Mathf.Cos(a)*Mathf.Cos(b)*radii.x,Mathf.Sin(b)*radii.y,Mathf.Sin(a)*Mathf.Cos(b)*radii.z);}
    Band(mesh,points,Mathf.Lerp(.08f,.24f,Mathf.Cos(latitude)),Pick(palette)*R(.7f,1),.25f,(points[0]-center).normalized);
   }
   for(int j=0;j<80;j++){
    float a=R(0,6.28f),b=R(-1.4f,1.4f);var points=new Vector3[5];
    for(int k=0;k<5;k++){float aa=a+k*.1f,bb=b+Mathf.Sin(aa*2)*.04f;points[k]=center+P(Mathf.Cos(aa)*Mathf.Cos(bb)*radii.x*1.04f,Mathf.Sin(bb)*radii.y*1.04f,Mathf.Sin(aa)*Mathf.Cos(bb)*radii.z*1.04f);}
    Band(mesh,points,R(.05f,.15f),Pick(palette),.25f,(points[0]-center).normalized,4);
   }
   Save(mesh,"Curled foliage ribbons",tree);
  }
 }
}
#endif
