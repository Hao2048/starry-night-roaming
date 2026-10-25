#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StarryStudy
{
 public static class PainterlyTownBuilder
 {
  const string AssetsRoot="Assets/StarryStudy/PainterlyTownMeshes";
  static Transform root;
  static Material paint;
  static System.Random random;
  static int meshId,total;
  static float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
  static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
  static GameObject Save(PaintMesh mesh,string name,Transform parent)
  {
   var go=mesh.Create(name,parent,paint);var mark=go.AddComponent<RibbonGeometry>();mark.Role="Town "+name;mark.StrokeCount=mesh.RibbonCount;mark.LongestStrokeLength=mesh.LongestRibbonLength;total+=mesh.RibbonCount;
   var filter=go.GetComponent<MeshFilter>();var generated=filter.sharedMesh;generated.hideFlags=HideFlags.None;string path=AssetsRoot+"/Town-"+(meshId++).ToString("D3")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(old==null)AssetDatabase.CreateAsset(generated,path);else{SculptedStarrySky.OverwriteMesh(generated,old);UnityEngine.Object.DestroyImmediate(generated);filter.sharedMesh=old;}
   return go;
  }
  static void Mark(PaintMesh mesh,Vector3 center,Vector3 direction,Vector3 normal,float length,float width,Color color,float phase,float energy=.7f)
  {
   direction=direction.normalized;normal=normal.normalized;var bend=Vector3.Cross(normal,direction).normalized;float curve=R(-.08f,.08f);
   var points=new Vector3[4];var widths=new float[4];var colors=new Color[4];
   for(int j=0;j<4;j++){float t=j/3f;points[j]=center+direction*((t-.5f)*length)+bend*(Mathf.Sin(t*Mathf.PI)*length*curve);widths[j]=width*(j==0||j==3?.10f:1);colors[j]=color;}
   if(energy>0)mesh.SetRibbonFlow((direction+normal*.12f).normalized,bend*.17f+normal*.14f,energy,phase);mesh.SculptedBand(points,widths,colors,.28f,4,normal);
  }
  static void Panel(PaintMesh mesh,Vector3 origin,Vector3 u,Vector3 v,Vector3 normal,Color baseColor,int seed,bool roof=false)
  {
   float w=u.magnitude,h=v.magnitude;var east=u.normalized;var north=v.normalized;
   // Flowing, jittered packets replace parallel vertical fence-like rows.
   for(float y=.25f;y<h;y+=1.0f)for(float x=.25f;x<w;x+=1.12f){
    float px=Mathf.Clamp(x+R(-.40f,.40f),0,w),py=Mathf.Clamp(y+R(-.35f,.35f),0,h),a=px/w,b=py/h;
    float stream=.5f+.5f*Mathf.Sin(b*7+Mathf.Sin(a*5+seed)*1.7f),edge=Mathf.Min(Mathf.Min(a,1-a),Mathf.Min(b,1-b));
    float density=Mathf.Lerp(.72f,.97f,stream);if(edge<.07f)density=.98f;if(R(0,1)>density)continue;
    float angle=(roof?.48f:.12f)+.48f*Mathf.Sin(a*4.5f+b*2.3f+seed*.7f);var direction=(east*Mathf.Cos(angle)+north*Mathf.Sin(angle)).normalized;
    float scatter=Mathf.Lerp(.12f,.65f,1-stream);var p=origin+east*px+north*py+normal*R(-scatter,scatter);
    var color=Color.Lerp(baseColor*.75f,baseColor*1.22f,.25f+stream*.6f)*R(.93f,1.06f);
    Mark(mesh,p,direction,normal,R(1.15f,2.5f),R(.23f,.46f),color,a*2+b*.65f+seed*.13f,roof?1.1f:.7f);
    // A few loose edge dabs leave the silhouette as a brush cloud, without erasing the building.
    if(edge<.09f&&R(0,1)<.18f)Mark(mesh,p+normal*R(1.0f,2.5f)+east*R(-.7f,.7f),direction,normal,R(.8f,1.7f),R(.15f,.29f),color*.9f,a*2+b,1.5f);
   }
  }
  static void Edge(PaintMesh mesh,Vector3 a,Vector3 b,Vector3 normal,Color color,int seed)
  {
   float distance=Vector3.Distance(a,b);var dir=(b-a).normalized;
   for(float t=.3f;t<distance;t+=1.35f)Mark(mesh,Vector3.Lerp(a,b,t/distance)+normal*R(-.18f,.18f),dir,normal,R(1.1f,1.8f),R(.18f,.29f),color*R(.9f,1.1f),t/distance+seed*.17f,.5f);
  }
  static StarryTownBuilding House(string name,Vector2 site,float yaw,float w,float h,float d,bool church=false,float towerHeight=35,float spireHeight=23)
  {
   var group=new GameObject(name).transform;group.SetParent(root,false);var position=new Vector3(site.x,StarryWorldLayout.GroundHeight(site.x,site.y),site.y);var q=Quaternion.Euler(0,yaw,0);group.position=position;
   Vector3 P(float x,float y,float z)=>q*new Vector3(x,y,z);Vector3 D(float x,float y,float z)=>q*new Vector3(x,y,z);
   var body=new PaintMesh();var roof=new PaintMesh();var accents=new PaintMesh();var footing=new PaintMesh();int seed=meshId+3;float rise=h*(church?.50f:R(.36f,.56f));
   Color walls=church?C("819389"):Color.Lerp(C("36535D"),C("8B9580"),R(.15f,.85f)),tiles=Color.Lerp(C("203B57"),C("786B55"),R(.1f,.9f));
   Panel(body,P(-w/2,0,-d/2),D(w,0,0),Vector3.up*h,D(0,0,-1),walls,seed);
   Panel(body,P(w/2,0,d/2),D(-w,0,0),Vector3.up*h,D(0,0,1),walls*.87f,seed+1);
   Panel(body,P(-w/2,0,d/2),D(0,0,-d),Vector3.up*h,D(-1,0,0),walls*.88f,seed+2);
   Panel(body,P(w/2,0,-d/2),D(0,0,d),Vector3.up*h,D(1,0,0),walls*.95f,seed+3);
   Panel(roof,P(-w/2,h,-d/2),D(w/2,rise,0),D(0,0,d),D(-.6f,.8f,0),tiles,seed,true);
   Panel(roof,P(w/2,h,d/2),D(-w/2,rise,0),D(0,0,-d),D(.6f,.8f,0),tiles*.9f,seed+2,true);
   for(int end=-1;end<=1;end+=2)for(float x=-w/2+.4f;x<w/2;x+=1.0f)for(float y=.4f;y<rise*(1-Mathf.Abs(x)/(w/2));y+=.9f)Mark(body,P(x,h+y,end*d/2+end*R(-.3f,.3f)),D(1,.25f,0),D(0,0,end),R(1.0f,2.0f),R(.22f,.39f),walls*R(.9f,1.1f),x/w+y/rise,.7f);
   // Sparse pigment inside the volume prevents a hollow paper-shell reading from oblique views.
   for(int i=0;i<280;i++){var p=P(R(-w*.45f,w*.45f),R(1,h),R(-d*.45f,d*.45f));Mark(body,p,D(1,R(-.3f,.3f),R(-.2f,.2f)),D(0,0,-1),R(.9f,1.8f),R(.17f,.32f),walls*.8f,i*.037f,.5f);}
   for(int side=-1;side<=1;side+=2){Edge(roof,P(-w/2,h,side*d/2),P(0,h+rise,side*d/2),D(0,0,side),C("82998F"),seed);Edge(roof,P(w/2,h,side*d/2),P(0,h+rise,side*d/2),D(0,0,side),C("82998F"),seed+1);}
   Edge(roof,P(0,h+rise,-d/2),P(0,h+rise,d/2),Vector3.up,C("90A394"),seed+2);
   for(int corner=0;corner<4;corner++){float cx=(corner%2==0?-1:1)*w/2,cz=(corner<2?-1:1)*d/2;Edge(body,P(cx,0,cz),P(cx,h,cz),D(0,0,corner<2?-1:1),C("29485A"),seed+corner);}
   int floors=h>20?3:2;
   for(int side=-1;side<=1;side+=2)for(int row=0;row<floors;row++)for(float x=-w*.32f;x<w*.40f;x+=w*.31f){float y=h*(.25f+row*.55f/Mathf.Max(1,floors-1));var color=R(0,1)<.46f?C("DDB85D"):C("132F45");for(int line=0;line<5;line++)Mark(accents,P(x,y+line*.32f,side*(d/2+.35f)),D(1,.10f,0),D(0,0,side),1.45f,.20f,color,x/w+row*.23f,.24f);}
   for(float y=.3f;y<Mathf.Min(5,h*.4f);y+=.6f)Mark(accents,P(0,y,-d/2-.4f),Vector3.up,D(0,0,-1),.9f,.65f,C("1C3643"),y*.15f,.18f);
   for(int side=-1;side<=1;side+=2)for(int window=0;window<3;window++){
    float z=(window-1)*d*.28f;var color=church?C("C5B078"):(R(0,1)<.45f?C("DDB85D"):C("183C50"));int lines=church?8:5;
    for(int line=0;line<lines;line++)Mark(accents,P(side*(w/2+.35f),h*.35f+line*(church?.50f:.32f),z),D(0,.08f,1),D(side,0,0),church?1.8f:1.45f,.2f,color,z/d+line*.1f,.2f);
   }
   if(!church&&seed%2==0){float cw=2.2f,ch=4.4f,cx=w*.22f,cz=d*.17f,cy=h+rise*.72f;var o=P(cx-cw/2,cy,cz-cw/2);var brick=C("66776C");Panel(body,o,D(cw,0,0),Vector3.up*ch,D(0,0,-1),brick,seed);Panel(body,o+D(cw,0,0),D(0,0,cw),Vector3.up*ch,D(1,0,0),brick*.9f,seed+1);Panel(body,o+D(cw,0,cw),D(-cw,0,0),Vector3.up*ch,D(0,0,1),brick*.8f,seed+2);Panel(body,o+D(0,0,cw),D(0,0,-cw),Vector3.up*ch,D(-1,0,0),brick*.9f,seed+3);}
   float top=h+rise;
   if(church){
    float tower=towerHeight,size=8;var o=P(-size/2,0,-d*.27f-size/2);Panel(body,o,D(size,0,0),Vector3.up*tower,D(0,0,-1),walls*.95f,seed+4);Panel(body,o+D(size,0,0),D(0,0,size),Vector3.up*tower,D(1,0,0),walls*.85f,seed+5);Panel(body,o+D(size,0,size),D(-size,0,0),Vector3.up*tower,D(0,0,1),walls*.8f,seed+6);Panel(body,o+D(0,0,size),D(0,0,-size),Vector3.up*tower,D(-1,0,0),walls*.89f,seed+7);
    for(int i=0;i<1600;i++){float t=R(0,1),a=R(0,Mathf.PI*2),radius=4.9f*Mathf.Pow(1-t,1.15f),depth=R(.72f,1.03f);var p=P(Mathf.Cos(a)*radius*depth,tower+t*spireHeight,-d*.27f+Mathf.Sin(a)*radius*depth);var dir=D(-Mathf.Sin(a)*.2f,1,Mathf.Cos(a)*.2f).normalized;Mark(roof,p,dir,D(Mathf.Cos(a),.2f,Mathf.Sin(a)),R(.9f,1.9f),R(.16f,.34f),tiles*R(.9f,1.15f),t*3+a*.08f,.7f);}
    for(int j=0;j<28;j++){float a=j/28f*Mathf.PI*2;var p=P(Mathf.Cos(a)*2.0f,26+Mathf.Sin(a)*2.0f,-d*.27f-size/2-.4f);Mark(accents,p,D(-Mathf.Sin(a),Mathf.Cos(a),0),D(0,0,-1),.6f,.17f,C("C9AF6B"),j/28f,.14f);}
    for(float y=.5f;y<5;y+=.45f)Mark(accents,P(0,y,-d/2-.55f),D(1,.1f,0),D(0,0,-1),2.2f,.23f,C("B2A271"),y*.1f,.16f);
    top=tower+spireHeight;
   }
   for(float z=-d/2-3;z<d/2+3;z+=1.15f)for(float x=-w/2-3;x<w/2+3;x+=1.15f){var local=P(x+R(-.3f,.3f),.06f,z+R(-.3f,.3f));var world=position+local;float ground=StarryWorldLayout.GroundHeight(world.x,world.z);local.y=Mathf.Max(.06f,ground-position.y+.08f);Mark(footing,local,D(1,.02f,.25f),Vector3.up,R(1.5f,2.8f),R(.32f,.55f),Color.Lerp(C("183A45"),C("466456"),R(0,1)),x*.04f+z*.03f,.25f);}
   Save(footing,name+" / gathered ground pigment",group);
   Save(body,name+" / pigment walls",group);Save(roof,name+" / roof currents",group);Save(accents,name+" / warm accents",group);
   var building=group.gameObject.AddComponent<StarryTownBuilding>();building.IsChurch=church;var renderers=group.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);bounds.Expand(3);building.WorldBounds=bounds;return building;
  }
  static void Tree(Vector2 site,float height,float width,string name,Transform parent=null)
  {
   var mesh=new PaintMesh();var basePoint=new Vector3(site.x,StarryWorldLayout.GroundHeight(site.x,site.y),site.y);
   for(int i=0;i<420;i++){float t=R(0,1),a=R(0,Mathf.PI*2),r=Mathf.Sqrt(R(.1f,1))*width*Mathf.Pow(Mathf.Sin(t*Mathf.PI),.65f);var p=new Vector3(Mathf.Cos(a)*r,t*height,Mathf.Sin(a)*r);var dir=new Vector3(-Mathf.Sin(a)*.55f,1,Mathf.Cos(a)*.55f).normalized;var normal=new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a));var c=Color.Lerp(C("173C43"),C("628073"),R(0,1));Mark(mesh,p,dir,normal,R(.9f,2.0f),R(.22f,.42f),c,t*3+a*.07f,.9f);}
   var go=Save(mesh,name,parent!=null?parent:root);go.transform.position=basePoint;
  }
  static Vector3 RoadPoint(float t)
  {
   Vector3 a=new(-38,0,68),b=new(-115,0,145),c=new(-45,0,230),d=new(20,0,325);float s=1-t;return a*s*s*s+b*3*s*s*t+c*3*s*t*t+d*t*t*t;
  }
  static void LinkedRoadAndSquare()
  {
   var road=new PaintMesh();var shoulders=new PaintMesh();
   for(int i=0;i<13500;i++){float t=R(0,1);var p=RoadPoint(t);var tangent=(RoadPoint(Mathf.Min(1,t+.005f))-RoadPoint(Mathf.Max(0,t-.005f))).normalized;var side=Vector3.Cross(Vector3.up,tangent);p+=side*R(-6,6);p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+R(.05f,.35f);var c=Color.Lerp(C("315B68"),C("9A9D6C"),R(0,1));Mark(road,p,tangent,Vector3.up,R(1.2f,2.8f),R(.22f,.48f),c,t*4,.85f);
    if(i%3==0){p+=side*R(-8,8);p.y+=R(.1f,1.4f);Mark(shoulders,p,(tangent+side*.3f).normalized,Vector3.up,R(.8f,1.8f),R(.18f,.36f),Color.Lerp(C("315B50"),C("718B62"),R(0,1)),t*4,1.2f);}}
   for(int i=0;i<2700;i++){float a=R(0,Mathf.PI*2),r=Mathf.Sqrt(R(0,1))*28;var p=new Vector3(20+Mathf.Cos(a)*r,0,325+Mathf.Sin(a)*r*.72f);p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+.15f;var tangent=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a));Mark(road,p,tangent,Vector3.up,R(1,2.4f),R(.2f,.43f),Color.Lerp(C("34566C"),C("7C8B7C"),R(0,1)),a/Mathf.PI, .65f);}
   Save(road,"Cypress path to village square",root);Save(shoulders,"Loose green strokes linking roots and gardens",root);
  }
  static void BranchingLanes()
  {
   var footprints=new List<Bounds>();foreach(var building in root.GetComponentsInChildren<StarryTownBuilding>())footprints.Add(building.GetWorldBounds());
   for(int district=0;district<StarryWorldLayout.Districts.Length;district++){
    var relative=StarryWorldLayout.Districts[district];var end=new Vector3(relative.x+StarryWorldLayout.Origin.x,0,relative.y+StarryWorldLayout.Origin.z);
    var start=end.z<100?RoadPoint(.28f):new Vector3(20,0,325);var delta=end-start;var perpendicular=new Vector3(-delta.z,0,delta.x).normalized;
    Vector3 c1,c2;
    if(end.z<100){if(end.x>100){c1=new Vector3(40,0,160);c2=new Vector3(180,0,-70);}else if(end.x< -100){c1=new Vector3(-160,0,140);c2=new Vector3(-260,0,-80);}else{c1=new Vector3(-130,0,100);c2=new Vector3(-80,0,-320);}}
    else if(end.x< -200){c1=new Vector3(-80,0,335);c2=new Vector3(-180,0,Mathf.Min(490,end.z+80));}
    else if(end.x>200){c1=new Vector3(160,0,335);c2=new Vector3(end.z>500?320:250,0,end.z>500?480:375);}
    else{c1=new Vector3(-20,0,405);c2=new Vector3(-45,0,510);}
    Vector3 Point(float t){float s=1-t;return start*s*s*s+c1*3*s*s*t+c2*3*s*t*t+end*t*t*t;}
    var mesh=new PaintMesh();
    for(int i=0;i<2600;i++){float t=R(0,1);var p=Point(t);var tangent=(Point(Mathf.Min(1,t+.005f))-Point(Mathf.Max(0,t-.005f))).normalized;var side=Vector3.Cross(Vector3.up,tangent);p+=side*R(-3.2f,3.2f);bool blocked=false;foreach(var bounds in footprints)if(p.x>bounds.min.x&&p.x<bounds.max.x&&p.z>bounds.min.z&&p.z<bounds.max.z){blocked=true;break;}if(blocked)continue;p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+.15f;Mark(mesh,p,tangent,Vector3.up,R(1.1f,2.5f),R(.20f,.40f),Color.Lerp(C("33596A"),C("7F8F72"),R(0,1)),t*5+district*.17f,.6f);}
    Save(mesh,"Connected quarter lane "+(district+1),root);
   }
  }
  public static void RebuildConnections()
  {
   var world=GameObject.Find("Reference fitted short-stroke world");root=world.transform.Find("城镇与柏树相连的笔触山谷");
   foreach(var marker in root.GetComponentsInChildren<RibbonGeometry>())if(marker.Role.StartsWith("Town Connected quarter lane"))UnityEngine.Object.DestroyImmediate(marker.gameObject);
   paint=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Flowing town pigment.mat");random=new System.Random(189024);meshId=900;total=0;BranchingLanes();EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();
  }
  static void ValleyGround()
  {
   var mesh=new PaintMesh();
   for(int i=0;i<45000;i++){float x=R(-230,300),z=R(130,530);var point=new Vector3(x,StarryWorldLayout.GroundHeight(x,z)+R(.03f,.22f),z);float turn=.3f*Mathf.Sin(x*.017f+z*.008f);var direction=new Vector3(Mathf.Sin(turn),0,Mathf.Cos(turn));Mark(mesh,point,direction,Vector3.up,R(1.6f,3.0f),R(.35f,.65f),Color.Lerp(C("183C49"),C("416457"),R(0,1)),z*.008f+x*.003f,.35f);}
   Save(mesh,"Valley ground currents holding the village",root);
  }
  static void MiddleLandscape()
  {
   for(int sector=0;sector<6;sector++){
    var mesh=new PaintMesh();for(int i=0;i<50000;i++){float x=R(-1000,1000),z=R(520,1120);if((int)((x+1000)/2000*6)!=sector)continue;float y=StarryWorldLayout.GroundHeight(x,z);var p=new Vector3(x,y+R(-.3f,.3f),z);float dx=(StarryWorldLayout.GroundHeight(x+2,z)-y)/2,dz=(StarryWorldLayout.GroundHeight(x,z+2)-y)/2;var tangent=new Vector3(1,dx,.25f*Mathf.Sin(x*.014f+z*.01f)).normalized;var normal=new Vector3(-dx,1,-dz).normalized;Mark(mesh,p,tangent,normal,R(2.0f,4.8f),R(.4f,.85f),Color.Lerp(C("203E65"),C("56828B"),R(0,1)),(x+z)*.002f,.7f);}
    Save(mesh,"Blue ridge contour packets "+sector,root);
   }
  }
  // Layout anchors are measured in the reference camera frame, not sampled image colours.
  // Intersecting the actual terrain gives each row its own world depth and walkable footing.
  static Vector3 PaintingGround(float u,float v)
  {
   var rotation=Quaternion.Euler(-8,0,0);float tan=Mathf.Tan(53*Mathf.Deg2Rad/2);
   var ray=(rotation*new Vector3((u-.5f)*2*tan*1.255f,(.5f-v)*2*tan,1)).normalized;
   var origin=StarryWorldLayout.Origin;float previous=0;
   for(float t=2;t<=1800;t+=2){var p=origin+ray*t;if(p.y<=StarryWorldLayout.GroundHeight(p.x,p.z)){float lo=previous,hi=t;for(int j=0;j<16;j++){float mid=(lo+hi)*.5f;var q=origin+ray*mid;if(q.y>StarryWorldLayout.GroundHeight(q.x,q.z))lo=mid;else hi=mid;}return origin+ray*((lo+hi)*.5f);}previous=t;}
   throw new InvalidOperationException("Reference village anchor misses terrain.");
  }
  static void PaintedHouse(string name,float u,float v,float width,float height,float yaw,bool church=false)
  {
   var p=PaintingGround(u,v);float depth=(Quaternion.Inverse(Quaternion.Euler(-8,0,0))*(p-StarryWorldLayout.Origin)).z;
   float span=2*depth*Mathf.Tan(53*Mathf.Deg2Rad/2);float w=width*span*1.255f;
   // Walls, roofs and spire are separate real volumes, scaled to their painted silhouette.
   float h=height*span/1.45f;
   House(name,new Vector2(p.x,p.z),yaw,w,h,w*R(.72f,1.12f),church,church?span*.15f:35,church?span*.13f:23);
  }
  static void ReferenceVillage()
  {
   PaintedHouse("Church - reference steeple and nave",.568f,.925f,.047f,.070f,-8,true);
   // Back roofs form a low, irregular skyline beneath the painted hillside.
   float[] back={.46f,.51f,.65f,.70f,.75f,.80f,.85f,.90f,.95f,.99f};
   for(int i=0;i<back.Length;i++)PaintedHouse("Reference back lane / home "+i,back[i],.870f+R(-.008f,.012f),R(.037f,.052f),R(.026f,.041f),R(-35,30));
   float[] middle={.46f,.51f,.64f,.70f,.76f,.82f,.88f,.94f,.99f};
   for(int i=0;i<middle.Length;i++)PaintedHouse("Reference middle lane / home "+i,middle[i],.939f+R(-.008f,.014f),R(.043f,.060f),R(.043f,.062f),R(-32,26));
   float[] front={.43f,.49f,.55f,.62f,.69f,.76f,.84f,.92f,.99f};
   for(int i=0;i<front.Length;i++)PaintedHouse("Reference foreground roofs / home "+i,front[i],1.014f+R(-.008f,.018f),R(.056f,.077f),R(.055f,.078f),R(-32,30));
   PaintedHouse("West garden home",.45f,.908f,.039f,.039f,18);
   PaintedHouse("East garden home",.96f,.902f,.045f,.045f,-20);
   PaintedHouse("Church side home",.62f,.897f,.033f,.032f,-14);
   PaintedHouse("Church rear home",.60f,.866f,.032f,.030f,10);
   // Tree rows frame the roofs rather than competing with the dominant foreground cypress.
   for(int i=0;i<48;i++){float u=Mathf.Lerp(.46f,1.04f,i/47f);var p=PaintingGround(u,.824f+R(-.009f,.012f));Tree(new Vector2(p.x,p.z),R(7,13),R(3.0f,5.3f),"Reference hillside orchard "+i);}
   for(int i=0;i<22;i++){var p=PaintingGround(R(.67f,1.04f),R(.89f,.99f));Tree(new Vector2(p.x,p.z),R(5,10),R(2.5f,4),"Reference garden canopy "+i);}
   // Keep the rest of the explorable world alive outside the reference view.
   for(int quarter=0;quarter<5;quarter++){
    var anchor=new Vector2(quarter%2==0?-530:560,-260+(quarter/2)*160);
    for(int i=0;i<5;i++)House("Outer quarter "+quarter+" / home "+i,anchor+new Vector2((i%2)*60,(i/2)*58),R(-25,25),R(20,29),R(13,20),R(20,28));
    for(int i=0;i<8;i++)Tree(anchor+new Vector2(R(-45,115),R(-30,180)),R(8,14),R(3,5),"Outer orchard "+quarter+" / "+i);
   }
  }
  static Vector3 ReferencePoint(float u,float v,float depth)
  {
   float t=Mathf.Tan(53*Mathf.Deg2Rad/2);return StarryWorldLayout.Origin+Quaternion.Euler(-8,0,0)*new Vector3((u-.5f)*2*t*1.255f*depth,(.5f-v)*2*t*depth,depth);
  }
  static float RidgeLine(float u,int layer)
  {
   float[] xs={-.2f,0,.16f,.28f,.40f,.48f,.56f,.62f,.71f,.78f,.85f,.90f,.95f,1,1.2f};
   float[] ys={.78f,.765f,.73f,.755f,.72f,.69f,.71f,.65f,.635f,.62f,.597f,.55f,.515f,.52f,.56f};
   int k=0;while(k<xs.Length-2&&u>xs[k+1])k++;float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(xs[k],xs[k+1],u));return Mathf.Lerp(ys[k],ys[k+1],t)+(2-layer)*.047f;
  }
  static void LayeredMountainRidges(Transform parent)
  {
   for(int layer=2;layer>=0;layer--){var mesh=new PaintMesh();float depth=650+layer*150,front=490+layer*145;
    for(float u=-.18f;u<1.19f;u+=.0027f){var crest=ReferencePoint(u,RidgeLine(u,layer),depth);var next=ReferencePoint(u+.003f,RidgeLine(u+.003f,layer),depth);float baseY=StarryWorldLayout.GroundHeight(crest.x,front);crest.y=Mathf.Max(crest.y,baseY+12);float slope=Mathf.Clamp((next.y-crest.y)/(next.x-crest.x),-1,1);float back=crest.z+170;
     float Height(float z){if(z<=crest.z)return Mathf.Lerp(baseY,crest.y,Mathf.SmoothStep(0,1,Mathf.InverseLerp(front,crest.z,z)));return Mathf.Lerp(crest.y,StarryWorldLayout.GroundHeight(crest.x,back),Mathf.SmoothStep(0,1,Mathf.InverseLerp(crest.z,back,z)));}
     for(float z=front;z<back;z+=2.8f){float edge=Mathf.Min((z-front)/22,(back-z)/30);if(R(0,1)>Mathf.Clamp01(edge))continue;float y=Height(z);var p=new Vector3(crest.x+R(-.65f,.65f),y+R(-.25f,.25f),z+R(-.65f,.65f));float ramp=Mathf.SmoothStep(0,1,Mathf.InverseLerp(front,crest.z,z));var tangent=new Vector3(1,slope*ramp+.10f*Mathf.Sin(z*.05f+u*15),.14f*Mathf.Sin(u*12)).normalized;
      float dz=(Height(z+1)-Height(z-1))*.5f;var normal=new Vector3(-slope*ramp,1,-dz).normalized;
      Color dark=layer==2?C("142044"):layer==1?C("1C2F59"):C("263E6C");Color light=layer==2?C("30426F"):layer==1?C("45608A"):C("57739A");float stripe=.5f+.5f*Mathf.Sin(u*200+z*.38f);
      Mark(mesh,p,tangent,normal,R(3.0f,5.0f),R(1.25f,1.85f),Color.Lerp(dark,light,stripe*.52f+R(0,.14f)),u*8+z*.015f,.25f);
     }
    }Save(mesh,"Landscape layered blue ridge "+layer,parent);
   }
  }
  static void ExpandedFeatheredGround(Transform parent)
  {
   var flowing=paint;paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/StarryStudy/FittedMeshes/Measured pigment.mat");
   for(int sector=0;sector<16;sector++){var mesh=new PaintMesh();
    for(int i=0;i<19000;i++){float az=(sector+R(0,1))/16*Mathf.PI*2;float outer=2300+70*Mathf.Sin(az*7)+35*Mathf.Sin(az*13);float r=Mathf.Pow(R(0,1),.85f)*outer;float feather=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1450+Mathf.Sin(az*5)*80,outer,r));if(R(0,1)>feather)continue;
     float x=StarryWorldLayout.Origin.x+Mathf.Sin(az)*r,z=StarryWorldLayout.Origin.z+Mathf.Cos(az)*r;float y=StarryWorldLayout.GroundHeight(x,z);float dx=(StarryWorldLayout.GroundHeight(x+2,z)-y)*.5f,dz=(StarryWorldLayout.GroundHeight(x,z+2)-y)*.5f;
     float turn=.2f*Mathf.Sin(x*.008f+z*.006f);var dir=new Vector3(Mathf.Cos(turn),dx,Mathf.Sin(turn)).normalized;var normal=new Vector3(-dx,1,-dz).normalized;float lod=Mathf.Lerp(1,4,Mathf.SmoothStep(0,1,Mathf.InverseLerp(150,1600,r))),edgeScale=Mathf.Lerp(.42f,1,Mathf.Sqrt(feather));
     var color=Color.Lerp(C("193D50"),C("527066"),R(.1f,.8f));color=Color.Lerp(C("1A354E"),color,Mathf.Lerp(.35f,1,feather));Mark(mesh,new Vector3(x,y+R(-.08f,.03f),z),dir,normal,R(2.5f,4.8f)*lod*edgeScale,R(.8f,1.4f)*lod*edgeScale,color,0,0);
    }Save(mesh,"Landscape expanded feathered ground sector "+sector,parent);
   }paint=flowing;
  }
  static void CorrectSkyBehindLandscape()
  {
   var hero=StarryWorldLayout.Origin;var inverse=Quaternion.Inverse(Quaternion.Euler(-8,0,0));float tan=Mathf.Tan(53*Mathf.Deg2Rad/2);
   var world=GameObject.Find("Reference fitted short-stroke world");
   var reference=new Texture2D(2,2,TextureFormat.RGB24,false);ImageConversion.LoadImage(reference,File.ReadAllBytes("Assets/StarryStudy/Editor/Reference/StarryNight.jpg"));
   try{foreach(var marker in world.GetComponentsInChildren<RibbonGeometry>(true)){
    bool background=marker.Role=="Far blue intervals"||marker.Role=="Horizon wind"||marker.Role=="Reference fitted volume 100";
    bool paintedSky=background||marker.Role=="Main S current"||marker.Role=="Small vortex"||marker.Role=="Moon"||marker.Role.StartsWith("Star ");if(!paintedSky)continue;
    var filter=marker.GetComponent<MeshFilter>();var current=filter.sharedMesh;string currentPath=AssetDatabase.GetAssetPath(current),file=Path.GetFileNameWithoutExtension(currentPath);Mesh source=current;
    if(file.StartsWith("SkyDepth-"))source=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(file.Substring(9)));if(source==null)throw new InvalidOperationException("Missing original sky depth source.");
    var vertices=source.vertices;var colors=source.colors32;var flow=new List<Vector4>();source.GetUVs(2,flow);int moved=0;
    for(int i=0;i<vertices.Length;i+=12){var center=Vector3.zero;for(int j=0;j<12;j++)center+=marker.transform.TransformPoint(vertices[i+j]);center/=12;var p=inverse*(center-hero);if(p.z<=0)continue;float u=.5f+p.x/(2*tan*1.255f*p.z),v=.5f-p.y/(2*tan*p.z);
     if(marker.Role!="Reference fitted volume 100"&&u>=-.005f&&u<=1.005f&&v>=-.005f&&v<.78f&&(u<.014f||u>.986f||v<.014f)){var c=(Color)colors[i];if(c.r>c.b*1.04f&&c.g>c.b*.98f){Color32 clean=reference.GetPixelBilinear(Mathf.Clamp(u,.025f,.975f),1-Mathf.Clamp(v,.025f,.77f));for(int j=0;j<12;j++)colors[i+j]=clean;moved++;}}
     if(!background||p.z>=1350||u<-.18f||u>1.19f||v<RidgeLine(u,2)+.007f)continue;
     float scale=1350/p.z;for(int j=0;j<12;j++){var worldPoint=marker.transform.TransformPoint(vertices[i+j]);vertices[i+j]=marker.transform.InverseTransformPoint(hero+(worldPoint-hero)*scale);if(flow.Count==vertices.Length){var f=flow[i+j];f.w*=scale;flow[i+j]=f;}}moved++;
    }
    if(moved==0)continue;var copy=UnityEngine.Object.Instantiate(source);copy.vertices=vertices;copy.colors32=colors;if(flow.Count==vertices.Length)copy.SetUVs(2,flow);copy.RecalculateBounds();string path=AssetsRoot+"/SkyDepth-"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".asset";var target=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(target==null){copy.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(copy,path);target=copy;}else{SculptedStarrySky.OverwriteMesh(copy,target);UnityEngine.Object.DestroyImmediate(copy);}filter.sharedMesh=target;EditorUtility.SetDirty(filter);
   }}finally{UnityEngine.Object.DestroyImmediate(reference);}
  }
  static void SkyEdgeContinuation(Transform parent)
  {
   var reference=new Texture2D(2,2,TextureFormat.RGB24,false);ImageConversion.LoadImage(reference,File.ReadAllBytes("Assets/StarryStudy/Editor/Reference/StarryNight.jpg"));
   try{
    var rotation=Quaternion.Euler(-8,0,0);float tan=Mathf.Tan(53*Mathf.Deg2Rad/2);
    Color Pixel(float u,float v)=>reference.GetPixelBilinear(Mathf.Clamp(u,.025f,.975f),1-Mathf.Clamp(v,.025f,.77f));
    float L(float u,float v){var c=Pixel(u,v);return c.r*.2126f+c.g*.7152f+c.b*.0722f;}
    for(int band=0;band<3;band++){var mesh=new PaintMesh();
     for(int i=0;i<180000;i++){float u=R(-.48f,1.48f),v=R(-.48f,.79f);int side=u<.055f?0:u>.945f?1:2;if(side!=band||side==2&&v>.055f)continue;float outside=Mathf.Max(-u,u-1,-v),fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.16f,.48f,outside));float inside=Mathf.Max(0,Mathf.Min(Mathf.Min(u,1-u),v));float blend=Mathf.Clamp01(1-inside/.057f);if(R(0,1)>fade*blend)continue;
      float su=u<0?.012f+Mathf.Min(.10f,-u*.20f):u>1?.988f-Mathf.Min(.10f,(u-1)*.20f):u;float sv=v<0?.012f+Mathf.Min(.10f,-v*.20f):v;
      var semantic=StarrySemanticSky.At(Mathf.Clamp01(u),Mathf.Clamp(v,0,.78f));float depth=Mathf.Lerp(semantic.Depth*2.6f,R(650,950),Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.28f,outside)))+R(-12,12);var center=ReferencePoint(u,v,depth);
      // Tensor tangent of the original edge continues the short painted marks into space.
      float xx=0,xy=0,yy=0;for(int a=-2;a<=2;a++)for(int b=-2;b<=2;b++){float x=su+a*.0025f,y=sv+b*.0025f,gx=L(x+.001f,y)-L(x-.001f,y),gy=L(x,y+.001f)-L(x,y-.001f);xx+=gx*gx;xy+=gx*gy;yy+=gy*gy;}
      float angle=.5f*Mathf.Atan2(2*xy,xx-yy)+Mathf.PI*.5f;var direction=(rotation*new Vector3(Mathf.Cos(angle),-Mathf.Sin(angle),0)).normalized;var normal=-(center-StarryWorldLayout.Origin).normalized;var color=Pixel(su,sv)*R(.91f,1.06f);if(outside>.04f&&color.r>color.b*.98f&&color.g>color.b*.98f)color=Color.Lerp(C("23486C"),C("527D91"),R(0,1));
      // Finish the moon halo rather than extending its yellow edge into a straight column.
      if(u>.99f&&u<1.06f&&v>.02f&&v<.34f){float dx=(u-.915f)/.11f,dy=(v-.17f)/.14f,r=Mathf.Sqrt(dx*dx+dy*dy);if(r<1.18f){direction=(rotation*new Vector3(-dy,dx,0)).normalized;color=Color.Lerp(C("638D94"),C("C3B968"),Mathf.Clamp01((1.18f-r)*2));}}
      float scale=depth/520;Mark(mesh,center,direction,normal,R(1.4f,3.2f)*scale,R(.30f,.66f)*scale,color,u*3+v*2,.38f);
     }Save(mesh,"Landscape seamless sky continuation "+band,parent);
    }
   }finally{UnityEngine.Object.DestroyImmediate(reference);}
  }
  static bool ClearSite(Vector3 p,List<Bounds> obstacles,float margin)
  {
   foreach(var b in obstacles)if(p.x>b.min.x-margin&&p.x<b.max.x+margin&&p.z>b.min.z-margin&&p.z<b.max.z+margin)return false;return true;
  }
  static List<Vector3> VillagePath(List<Bounds> obstacles)
  {
   const float step=3;const int width=151,height=161;Vector3 Point(int id)=>new Vector3(-180+(id%width)*step,0,50+(id/width)*step);
   int Id(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z-50)/step),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt((p.x+180)/step),0,width-1);
   var start=Id(new Vector3(-52,0,84));var end=Id(new Vector3(135,0,430));int count=width*height;var cost=new float[count];var previous=new int[count];var closed=new bool[count];for(int i=0;i<count;i++){cost[i]=float.MaxValue;previous[i]=-1;}
   cost[start]=0;var open=new List<int>{start};int[] dx={-1,0,1,-1,1,-1,0,1},dz={-1,-1,-1,0,0,1,1,1};int current=start;
   while(open.Count>0){int best=0;float score=float.MaxValue;for(int j=0;j<open.Count;j++){int id=open[j];float f=cost[id]+Vector3.Distance(Point(id),Point(end));if(f<score){score=f;best=j;}}current=open[best];open.RemoveAt(best);if(current==end)break;if(closed[current])continue;closed[current]=true;
    for(int j=0;j<8;j++){int x=current%width+dx[j],z=current/width+dz[j];if(x<0||x>=width||z<0||z>=height)continue;int id=z*width+x;if(closed[id])continue;var p=Point(id);if(!ClearSite(p,obstacles,3.2f)||!ClearSite((p+Point(current))*.5f,obstacles,3.2f))continue;float next=cost[current]+step*(dx[j]!=0&&dz[j]!=0?1.4142f:1);if(next<cost[id]){cost[id]=next;previous[id]=current;open.Add(id);}}
   }
   if(current!=end)throw new InvalidOperationException("No clear tree-to-village path.");var points=new List<Vector3>();for(int id=end;id!=-1;id=previous[id])points.Add(Point(id));points.Reverse();
   for(int iteration=0;iteration<5;iteration++)for(int i=1;i<points.Count-1;i++){var p=(points[i-1]+points[i]*2+points[i+1])*.25f;if(ClearSite(p,obstacles,3.2f))points[i]=p;}
   return points;
  }
  static void ThroughVillageRoad(Transform parent,List<Bounds> obstacles)
  {
   var points=VillagePath(obstacles);var path=new PaintMesh();var plants=new PaintMesh();float distance=0;
   for(int i=1;i<points.Count;i++){var a=points[i-1];var b=points[i];var tangent=(b-a).normalized;var side=Vector3.Cross(Vector3.up,tangent);float span=Vector3.Distance(a,b);
    for(float t=0;t<span;t+=.42f){distance+=.42f;for(float w=-2.1f;w<=2.1f;w+=.48f){var p=Vector3.Lerp(a,b,t/span)+side*(w+R(-.2f,.2f));if(!ClearSite(p,obstacles,.4f))continue;p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+R(.14f,.26f);Mark(path,p,tangent,Vector3.up,R(1.0f,1.8f),R(.24f,.43f),Color.Lerp(C("465D66"),C("A19D79"),R(.1f,.85f)),distance*.018f,.24f);}
     if(i%4==0&&t<.5f)for(int sideSign=-1;sideSign<=1;sideSign+=2){var p=a+side*(sideSign*R(3.3f,4.8f));if(!ClearSite(p,obstacles,1))continue;p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+.3f;for(int j=0;j<14;j++)Mark(plants,p+new Vector3(R(-1,1),R(0,1.1f),R(-1,1)),new Vector3(.2f,1,.1f),Vector3.forward,R(.5f,1.2f),R(.12f,.24f),Color.Lerp(C("294D4D"),C("929663"),R(0,1)),i*.1f+j*.04f,.35f);}
    }
   }Save(path,"Landscape path from cypress through village",parent);Save(plants,"Landscape roadside herb clumps",parent);
  }
  static void ClearingDecorations(Transform parent,List<Bounds> obstacles)
  {
   var stone=new PaintMesh();var stems=new PaintMesh();
   Vector2[] sites={new(-105,180),new(-90,285),new(170,255),new(220,330),new(120,470),new(-130,390)};
   for(int i=0;i<sites.Length;i++){var site=sites[i];var center=new Vector3(site.x,StarryWorldLayout.GroundHeight(site.x,site.y),site.y);if(!ClearSite(center,obstacles,7))continue;
    // Low dry-stone garden enclosures and circular village wells, all broken into short dabs.
    for(float a=0;a<Mathf.PI*2;a+=.15f)for(float y=.3f;y<2.7f;y+=.52f){var p=center+new Vector3(Mathf.Cos(a)*2.8f,y,Mathf.Sin(a)*2.8f);Mark(stone,p,new Vector3(-Mathf.Sin(a),.08f,Mathf.Cos(a)),new Vector3(Mathf.Cos(a),.2f,Mathf.Sin(a)),R(.65f,1.1f),.34f,Color.Lerp(C("38545E"),C("8A9684"),R(0,1)),a+y,.12f);}
    for(int plant=0;plant<6;plant++){var p=center+new Vector3(R(-15,15),0,R(7,22));if(!ClearSite(p,obstacles,4))continue;Tree(new Vector2(p.x,p.z),R(4,8),R(2,3.6f),"Landscape clearing olive "+i+" / "+plant,parent);}
    var offset=center+new Vector3(-9,0,1);for(float x=-4;x<=4;x+=.8f)for(float y=.3f;y<1.7f;y+=.5f)Mark(stone,offset+new Vector3(x,y,0),Vector3.right,Vector3.forward,1.0f,.32f,C("657A76")*R(.8f,1.1f),x*.1f,.12f);
    for(int j=0;j<90;j++){var p=center+new Vector3(R(-10,10),0,R(-12,-5));p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+R(.2f,1);Mark(stems,p,new Vector3(.5f,1,.1f),Vector3.forward,R(.5f,1.3f),.18f,Color.Lerp(C("355952"),C("929267"),R(0,1)),j*.07f,.3f);}
   }Save(stone,"Landscape wells and low garden walls",parent);Save(stems,"Landscape clearing flower strokes",parent);
  }
  static void RichGroundPigment(Transform parent,List<Bounds> obstacles)
  {
   var route=VillagePath(obstacles);float RoadDistance(Vector3 p){float best=float.MaxValue;for(int j=1;j<route.Count;j++){var a=route[j-1];var b=route[j];var delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,delta)/Mathf.Max(.01f,delta.sqrMagnitude));best=Mathf.Min(best,(p-a-delta*t).sqrMagnitude);}return Mathf.Sqrt(best);}
   for(int sector=0;sector<4;sector++){var baseMesh=new PaintMesh();var fine=new PaintMesh();
    for(int i=0;i<28000;i++){float x=R(-200+sector*125,-75+sector*125),z=R(45,555);var p=new Vector3(x,0,z);if(!ClearSite(p,obstacles,-2))continue;float road=RoadDistance(p);if(road<2.8f)continue;
     float y=StarryWorldLayout.GroundHeight(x,z);float dx=(StarryWorldLayout.GroundHeight(x+1,z)-y),dz=StarryWorldLayout.GroundHeight(x,z+1)-y;var normal=new Vector3(-dx,1,-dz).normalized;
     float curve=.24f*Mathf.Sin(x*.019f+z*.013f)+.18f*Mathf.Sin(z*.028f);var direction=new Vector3(Mathf.Cos(curve),dx,Mathf.Sin(curve)).normalized;
     float patch=.5f+.5f*Mathf.Sin(x*.032f+Mathf.Sin(z*.022f)*2);var color=Color.Lerp(C("183E4D"),C("49635C"),patch*.6f+R(0,.25f));
     Mark(baseMesh,new Vector3(x,y+R(.02f,.10f),z),direction,normal,R(2.0f,3.8f),R(.55f,.95f),color,x*.013f+z*.011f,.15f);
     if(i%3==0){var q=new Vector3(x+R(-.7f,.7f),y+R(.13f,.35f),z+R(-.7f,.7f));bool grass=patch>.35f;var accent=grass?Color.Lerp(C("41615A"),C("8B9062"),R(0,1)):Color.Lerp(C("385465"),C("868577"),R(0,1));Mark(fine,q,(direction+normal*(grass?.45f:.03f)).normalized,normal,R(.45f,1.25f),R(.12f,.26f),accent,x*.02f+z*.04f,.26f);}
    }Save(baseMesh,"Landscape dense ground underpainting "+sector,parent);Save(fine,"Landscape grass and small stone detail "+sector,parent);
   }
  }
  static void LeftRoadFarmhouses(Transform parent)
  {
   Vector2[] sites={new(-145,200),new(-82,255),new(-170,335),new(-78,405),new(-230,440),new(-235,280)};
   for(int i=0;i<sites.Length;i++){
    var house=House("Landscape left lane farmhouse "+(i+1),sites[i],i%2==0?-18:16,R(17,23),R(10,15),R(18,25));house.transform.SetParent(parent,true);
    for(int j=0;j<3;j++)Tree(sites[i]+new Vector2(-18-j*6,16+j*5),R(5,9),R(2.2f,3.5f),"Landscape farm garden "+i+" / "+j,parent);
   }
  }
  static void GoldenFields(Transform parent,List<Bounds> obstacles)
  {
   var route=VillagePath(obstacles);
   float DistanceToRoad(Vector3 p){float best=float.MaxValue;for(int j=1;j<route.Count;j++){var d=route[j]-route[j-1];float t=Mathf.Clamp01(Vector3.Dot(p-route[j-1],d)/Mathf.Max(.01f,d.sqrMagnitude));best=Mathf.Min(best,(p-route[j-1]-d*t).sqrMagnitude);}return Mathf.Sqrt(best);}
   Vector4[] plots={new(-208,155,48,62),new(-123,336,48,64),new(-235,375,58,62),new(-83,176,43,46),new(-165,465,78,42)};
   for(int k=0;k<plots.Length;k++){
    var plot=plots[k];var wheat=new PaintMesh();var soil=new PaintMesh();
    for(float z=-plot.w/2;z<plot.w/2;z+=1.05f)for(float x=-plot.z/2;x<plot.z/2;x+=.95f){float nx=x/(plot.z*.5f),nz=z/(plot.w*.5f);if(Mathf.Pow(Mathf.Abs(nx),5)+Mathf.Pow(Mathf.Abs(nz),5)>1-.07f*Mathf.Sin(z*.27f)||R(0,1)<.16f||Mathf.Abs(Mathf.Sin(x*.23f))<.10f)continue;var p=new Vector3(plot.x+x+R(-.24f,.24f),0,plot.y+z+R(-.24f,.24f));if(!ClearSite(p,obstacles,4)||DistanceToRoad(p)<5.5f)continue;p.y=StarryWorldLayout.GroundHeight(p.x,p.z);
     float height=R(1.0f,1.9f);var lean=new Vector3(.28f+.16f*Mathf.Sin(x*.12f+z*.07f),1,.16f).normalized;float phase=p.x*.025f+p.z*.018f;
     Mark(wheat,p+Vector3.up*(height*.5f),lean,Vector3.forward,height,R(.13f,.21f),Color.Lerp(C("5E6C42"),C("8E8754"),R(0,1)),phase,.25f);
     for(int ear=0;ear<2;ear++)Mark(wheat,p+Vector3.up*(height+.15f)+new Vector3(ear*.16f,ear*.10f,0),new Vector3(.7f,1,.1f).normalized,Vector3.forward,R(.36f,.62f),R(.21f,.32f),Color.Lerp(C("8B7D47"),C("B6A46B"),R(.05f,.95f)),phase,.36f);
     if(((int)(x+plot.z)+(int)(z+plot.w))%2==0)Mark(soil,p+Vector3.up*.12f,Vector3.forward,Vector3.up,R(1.5f,2.3f),R(.35f,.55f),Color.Lerp(C("5B6046"),C("8A8051"),R(0,1)),phase,.10f);
    }
    Save(soil,"Landscape wheat furrows "+k,parent);Save(wheat,"Landscape golden wheat ears "+k,parent);
   }
   var hay=new PaintMesh();Vector2[] stacks={new(-156,160),new(-120,375),new(-196,426)};
   foreach(var site in stacks){var center=new Vector3(site.x,StarryWorldLayout.GroundHeight(site.x,site.y),site.y);for(int j=0;j<500;j++){float t=R(0,1),a=R(0,Mathf.PI*2),radius=3.8f*Mathf.Pow(1-t,.4f);var p=center+new Vector3(Mathf.Cos(a)*radius,t*4.5f,Mathf.Sin(a)*radius);Mark(hay,p,new Vector3(-Mathf.Sin(a),.45f,Mathf.Cos(a)),new Vector3(Mathf.Cos(a),.15f,Mathf.Sin(a)),R(.7f,1.3f),R(.18f,.32f),Color.Lerp(C("7F7542"),C("C1A35C"),R(0,1)),t+a*.08f,.12f);}}
   Save(hay,"Landscape gathered harvest hay",parent);
  }
  public static void RepairSkySeamPigment()
  {
   var world=GameObject.Find("Reference fitted short-stroke world");root=world.transform.Find("城镇与柏树相连的笔触山谷");var parent=root.Find("山脊、村路与空地装饰");
   foreach(var marker in parent.GetComponentsInChildren<RibbonGeometry>())if(marker.Role.StartsWith("Town Landscape seamless sky continuation"))UnityEngine.Object.DestroyImmediate(marker.gameObject);
   paint=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Flowing town pigment.mat");random=new System.Random(189031);meshId=1800;total=0;CorrectSkyBehindLandscape();SkyEdgeContinuation(parent);EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();
  }
  public static void RebuildLandscapeDetails(bool save=true)
  {
   var world=GameObject.Find("Reference fitted short-stroke world");root=world.transform.Find("城镇与柏树相连的笔触山谷");if(root==null)throw new InvalidOperationException("Build the town first.");
   foreach(var marker in world.GetComponentsInChildren<RibbonGeometry>(true))if(marker.Role.StartsWith("Extended landscape ")){if(int.TryParse(marker.Role.Substring(marker.Role.LastIndexOf(' ')+1),out int id)&&id>=320&&id<=367)UnityEngine.Object.DestroyImmediate(marker.gameObject);}
   var old=root.Find("山脊、村路与空地装饰");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var parent=new GameObject("山脊、村路与空地装饰").transform;parent.SetParent(root,false);
   paint=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Flowing town pigment.mat");random=new System.Random(189027);meshId=1000;total=0;
   LeftRoadFarmhouses(parent);
   var obstacles=new List<Bounds>();foreach(var house in root.GetComponentsInChildren<StarryTownBuilding>())obstacles.Add(house.GetWorldBounds());
   ExpandedFeatheredGround(parent);LayeredMountainRidges(parent);CorrectSkyBehindLandscape();SkyEdgeContinuation(parent);RichGroundPigment(parent,obstacles);ThroughVillageRoad(parent,obstacles);GoldenFields(parent,obstacles);ClearingDecorations(parent,obstacles);
   if(save){EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();}
   Debug.Log($"LANDSCAPE_DETAILS_READY strokes={total}; layered mountain volumes, clear village path, wells and planting");
  }
  [MenuItem("Starry Study/Rebuild flowing painterly town")]
  public static void FromMenu()=>Rebuild();
  public static void Rebuild(bool configureCamera=true)
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before rebuilding the town.");var world=GameObject.Find("Reference fitted short-stroke world");if(world==null)throw new InvalidOperationException("Build the starry world first.");
   foreach(var marker in world.GetComponentsInChildren<RibbonGeometry>(true)){
    bool remove=marker.Role.StartsWith("Reference grove")||marker.Role.StartsWith("Town ");
    if(marker.Role.StartsWith("Reference fitted volume ")){int id=int.Parse(marker.Role.Substring(marker.Role.LastIndexOf(' ')+1));remove|=id>=4&&id<=17||id>=50&&id<=65;}
    if(marker.Role.StartsWith("Extended landscape ")){int id=int.Parse(marker.Role.Substring(marker.Role.LastIndexOf(' ')+1));remove|=id>=400&&id<468;}
    if(remove)UnityEngine.Object.DestroyImmediate(marker.gameObject);
   }
   foreach(Transform child in world.transform)if(child.childCount==0&&child.GetComponent<MeshRenderer>()==null)UnityEngine.Object.DestroyImmediate(child.gameObject);
   var old=world.transform.Find("城镇与柏树相连的笔触山谷");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   root=new GameObject("城镇与柏树相连的笔触山谷").transform;root.SetParent(world.transform,false);Directory.CreateDirectory(AssetsRoot);AssetDatabase.Refresh();
   string materialPath=AssetsRoot+"/Flowing town pigment.mat";paint=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(paint==null){paint=new Material(Shader.Find("StarryStudy/MeasuredPigment"));AssetDatabase.CreateAsset(paint,materialPath);}paint.shader=Shader.Find("StarryStudy/MeasuredPigment");paint.SetFloat("_MotionEnabled",1);paint.SetFloat("_FlowRate",.75f);paint.SetFloat("_FlowGain",1.3f);EditorUtility.SetDirty(paint);
   random=new System.Random(189022);meshId=total=0;
   ReferenceVillage();
   ValleyGround();MiddleLandscape();StarryForegroundGround.Rebuild(false);RebuildLandscapeDetails(false);UnityEngine.Shader.SetGlobalFloat("_BrushFlowTime",0);
   if(configureCamera)StarryCinematicBuilder.Configure();EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);AssetDatabase.SaveAssets();
   Debug.Log($"PAINTERLY_TOWN_READY strokes={total} meshes={meshId}; flowing packets, connected cypress path, 58 buildings, reference village composition");
  }
 }
}
#endif
