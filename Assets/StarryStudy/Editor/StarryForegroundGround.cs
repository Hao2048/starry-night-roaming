#if UNITY_EDITOR
using ArtWalk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StarryStudy
{
 public static class StarryForegroundGround
 {
  public static void Rebuild(bool save=true)
  {
   var world=GameObject.Find("Reference fitted short-stroke world");if(world==null)throw new System.InvalidOperationException("Build the world before its foreground ground.");
   foreach(var old in world.GetComponentsInChildren<RibbonGeometry>(true))if(old.Role=="Foreground knoll")Object.DestroyImmediate(old.gameObject);
   var mesh=new PaintMesh();var random=new System.Random(189007);float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
   for(int i=0;i<16000;i++){
    float x=R(-140,80),z=R(-15,185),dx=x+30,dz=z-85;if(dx*dx/(110*110)+dz*dz/(100*100)>1)continue;
    var center=new Vector3(StarryWorldLayout.Origin.x+x,0,StarryWorldLayout.Origin.z+z);center.y=StarryWorldLayout.GroundHeight(center.x,center.z)+R(-.18f,.18f);
    var direction=new Vector3(-dz,0,dx).normalized;if(direction.sqrMagnitude<.1f)direction=Vector3.right;
    float length=R(1.1f,2.8f),width=R(.20f,.45f);var points=new Vector3[4];var sizes=new float[4];var colors=new Color[4];var color=Color.Lerp(new Color(.08f,.20f,.29f),new Color(.31f,.41f,.29f),R(0,1));
    for(int j=0;j<4;j++){float t=j/3f;var p=center+direction*((t-.5f)*length);p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+.1f;points[j]=p;sizes[j]=width*(j==0||j==3?.1f:1);colors[j]=color;}
    mesh.SculptedBand(points,sizes,colors,.22f,4,Vector3.up);
   }
   var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/StarryStudy/FittedMeshes/Measured pigment.mat");var go=mesh.Create("Foreground cypress knoll",world.transform,material);var marker=go.AddComponent<RibbonGeometry>();marker.Role="Foreground knoll";marker.StrokeCount=mesh.RibbonCount;marker.LongestStrokeLength=mesh.LongestRibbonLength;
   var filter=go.GetComponent<MeshFilter>();var generated=filter.sharedMesh;generated.hideFlags=HideFlags.None;string path="Assets/StarryStudy/FittedMeshes/Foreground-knoll.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(existing==null)AssetDatabase.CreateAsset(generated,path);else{SculptedStarrySky.OverwriteMesh(generated,existing);Object.DestroyImmediate(generated);filter.sharedMesh=existing;}
   if(save){EditorSceneManager.MarkSceneDirty(go.scene);EditorSceneManager.SaveScene(go.scene);AssetDatabase.SaveAssets();}
  }
 }
}
#endif
