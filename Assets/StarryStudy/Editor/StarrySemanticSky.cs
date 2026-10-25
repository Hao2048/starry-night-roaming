#if UNITY_EDITOR
using UnityEngine;
namespace StarryStudy
{
 // Each guide is a different spatial object. Z is artistic inferred depth, W is projected brush bundle radius.
 public static class StarrySemanticSky
 {
  public struct Sample {public int Feature;public float Depth,HalfDepth,Density,Progress;public Vector2 Direction;}
  static readonly Vector4[][] Controls={
   new[]{new Vector4(0,.18f,136,.04f),new(.08f,.27f,126,.055f),new(.18f,.31f,116,.065f),new(.28f,.26f,105,.055f),new(.39f,.20f,89,.055f),new(.50f,.20f,70,.07f),new(.59f,.28f,61,.075f),new(.61f,.36f,68,.075f),new(.56f,.42f,82,.06f),new(.49f,.40f,93,.06f),new(.45f,.34f,99,.05f),new(.46f,.30f,95,.035f)},
   new[]{new Vector4(.47f,.48f,137,.03f),new(.56f,.54f,140,.045f),new(.66f,.55f,130,.05f),new(.74f,.51f,114,.05f),new(.76f,.45f,102,.05f),new(.70f,.42f,96,.045f),new(.65f,.45f,102,.035f),new(.65f,.49f,116,.03f),new(.69f,.50f,123,.025f),new(.70f,.47f,122,.02f)},
   new[]{new Vector4(0,.59f,165,.035f),new(.14f,.65f,157,.045f),new(.31f,.65f,151,.045f),new(.47f,.61f,146,.04f),new(.65f,.56f,142,.045f),new(.80f,.50f,139,.055f),new(.94f,.43f,154,.05f),new(1,.39f,169,.045f)}
  };
  static readonly Vector4[][] Guides=SmoothGuides();
  static Vector4[][] SmoothGuides()
  {
   var result=new Vector4[Controls.Length][];
   for(int g=0;g<Controls.Length;g++){
    var c=Controls[g];var samples=new System.Collections.Generic.List<Vector4>();
    for(int j=0;j<c.Length-1;j++)for(int k=0;k<4;k++){
     float t=k/4f,t2=t*t,t3=t2*t;var p0=c[Mathf.Max(0,j-1)];var p1=c[j];var p2=c[j+1];var p3=c[Mathf.Min(c.Length-1,j+2)];
     var p=.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t2+(-p0+3*p1-3*p2+p3)*t3);p.w=Mathf.Max(.015f,p.w);samples.Add(p);
    }
    samples.Add(c[c.Length-1]);result[g]=samples.ToArray();
   }
   return result;
  }
  static readonly float[][] ArcLengths=BuildArcLengths();
  static float[][] BuildArcLengths(){var result=new float[Guides.Length][];for(int g=0;g<Guides.Length;g++){result[g]=new float[Guides[g].Length];for(int j=1;j<Guides[g].Length;j++){var a=Guides[g][j-1];var b=Guides[g][j];result[g][j]=result[g][j-1]+new Vector2((b.x-a.x)*1.255f,b.y-a.y).magnitude;}}return result;}
  static readonly Vector3[] Stars={new(.11f,.045f,.060f),new(.24f,.03f,.025f),new(.35f,.04f,.05f),new(.42f,.065f,.035f),new(.61f,.075f,.065f),new(.715f,.235f,.045f),new(.33f,.32f,.027f),new(.35f,.535f,.075f),new(.13f,.47f,.05f),new(.045f,.45f,.032f),new(.245f,.18f,.047f)};
  public static string Name(int id)=>id==0?"Far blue intervals":id==1?"Main S current":id==2?"Small vortex":id==3?"Horizon wind":id==4?"Moon": "Star "+(id-4);
  public static Sample At(float u,float v)
  {
   float x=(u-.5f)*1.255f,y=.5f-v;
   var result=new Sample{Feature=0,Depth=210/Mathf.Sqrt(1+x*x+y*y),HalfDepth=.7f,Density=.16f,Progress=u,Direction=Vector2.right};
   float best=1;
   for(int g=0;g<Guides.Length;g++){
    var guide=Guides[g];
    for(int j=0;j<guide.Length-1;j++){
     var a=guide[j];var b=guide[j+1];var p=new Vector2(u*1.255f,v);var p0=new Vector2(a.x*1.255f,a.y);var line=new Vector2((b.x-a.x)*1.255f,b.y-a.y);
     float t=Mathf.Clamp01(Vector2.Dot(p-p0,line)/Mathf.Max(.00001f,line.sqrMagnitude));float width=Mathf.Lerp(a.w,b.w,t),q=Vector2.Distance(p,p0+line*t)/width;
     if(q>=best)continue;best=q;float progress=Mathf.Lerp(ArcLengths[g][j],ArcLengths[g][j+1],t)/ArcLengths[g][guide.Length-1],cross=Mathf.Sqrt(Mathf.Max(0,1-q*q));float swell=Mathf.Sin(progress*Mathf.PI);swell*=swell;
     result=new Sample{Feature=g+1,Depth=Mathf.Lerp(a.z,b.z,t),HalfDepth=(g==0?6+9*swell:g==1?4+6*swell:2+4*swell)*cross,Density=.25f+.72f*cross,Progress=progress,Direction=new Vector2(b.x-a.x,b.y-a.y).normalized};
    }
   }
   float moon=Mathf.Sqrt((u-.915f)*(u-.915f)/(.11f*.11f)+(v-.17f)*(v-.17f)/(.14f*.14f));
   if(moon<1){float cross=Mathf.Sqrt(1-moon*moon);result=new Sample{Feature=4,Depth=238,HalfDepth=.3f+28*cross,Density=.56f+.42f*cross,Progress=moon,Direction=new Vector2(-(v-.17f),(u-.915f)*1.255f).normalized};}
   for(int i=0;i<Stars.Length;i++){
    var star=Stars[i];float dx=(u-star.x)*1.255f,dy=v-star.y,r=Mathf.Sqrt(dx*dx+dy*dy)/star.z;
    if(r>=1)continue;float cross=Mathf.Sqrt(1-r*r);result=new Sample{Feature=i+5,Depth=178+i*4,HalfDepth=.3f+7*cross,Density=.5f+.48f*cross,Progress=Mathf.Atan2(dy,dx)/(Mathf.PI*2),Direction=new Vector2(-dy,dx/1.255f).normalized};
   }
   return result;
  }
 }
}
#endif
