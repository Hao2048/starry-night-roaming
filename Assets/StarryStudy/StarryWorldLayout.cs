using UnityEngine;
namespace StarryStudy
{
 public static class StarryWorldLayout
 {
  public static readonly Vector3 Origin=new(0,13,-35);
  public const float GroundRadius=2300;
  public const float LandScale=2.8f;
  // Planned relative anchors: west quarter, east quarter, upper valley, NE farms, SW lane,
  // SE gardens, NW hillside, southern gateway. The reference village sits around (60,340).
  public static readonly Vector2[] Districts={new(-300,290),new(370,310),new(-80,620),new(650,650),new(-480,-300),new(420,-320),new(-650,570),new(20,-680)};
  public const float TravelRadius=1250;
  public static float GroundHeight(float worldX,float worldZ)
  {
   float x=worldX-Origin.x,z=worldZ-Origin.z,r=Mathf.Sqrt(x*x+z*z);
   float valley=Mathf.Exp(-Mathf.Pow((x-60)/280,2)-Mathf.Pow((z-340)/360,2));
   float rolling=(Mathf.Sin(x*.008f)*12+Mathf.Cos(z*.009f)*9+Mathf.Sin((x+z)*.014f)*4)*(1-valley*.95f);
   float cypressKnoll=54*Mathf.Exp(-Mathf.Pow((x+30)/65,2)-Mathf.Pow((z-85)/55,2));
   float hills=130*Mathf.Exp(-Mathf.Pow((x+520)/220,2)-Mathf.Pow((z-760)/180,2))+150*Mathf.Exp(-Mathf.Pow((x-580)/300,2)-Mathf.Pow((z-840)/220,2))+100*Mathf.Exp(-Mathf.Pow(x/600,2)-Mathf.Pow((z-1050)/120,2));
   return Origin.y-30*LandScale+hills+rolling*Mathf.SmoothStep(0,1,Mathf.InverseLerp(130,390,r))+cypressKnoll;
  }
 }
}
