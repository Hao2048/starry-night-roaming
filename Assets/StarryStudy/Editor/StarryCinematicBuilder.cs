#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StarryStudy
{
 public static class StarryCinematicBuilder
 {
  public static void Ensure(){if(Object.FindAnyObjectByType<StarryCinematicTour>()==null)Configure();}
  [MenuItem("Starry Study/Configure painting opening and filming route")]
  public static void Configure()
  {
   if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode before editing the camera route.");
   var controller=Object.FindAnyObjectByType<StarryStudyController>();if(controller==null)throw new System.InvalidOperationException("The preview scene is not open.");
   var old=Object.FindAnyObjectByType<StarryCinematicTour>();if(old!=null)Object.DestroyImmediate(old.gameObject);
   var root=new GameObject("拍摄运镜路径 - 原画构图到村镇（T播放 P暂停）");var tour=root.AddComponent<StarryCinematicTour>();
   Bounds tree=new Bounds(),church=new Bounds();bool hasTree=false,hasChurch=false;
   foreach(var marker in Object.FindObjectsByType<RibbonGeometry>()){
    var renderer=marker.GetComponent<MeshRenderer>();if(renderer==null)continue;
    if(marker.Role=="Reference fitted volume 2"){if(hasTree)tree.Encapsulate(renderer.bounds);else{tree=renderer.bounds;hasTree=true;}}
    if(marker.Role=="Reference fitted volume 4"||marker.Role=="Reference fitted volume 5"){if(hasChurch)church.Encapsulate(renderer.bounds);else{church=renderer.bounds;hasChurch=true;}}
   }
   foreach(var building in Object.FindObjectsByType<StarryTownBuilding>())if(building.IsChurch){church=building.GetWorldBounds();hasChurch=true;break;}
   if(!hasTree||!hasChurch)throw new System.InvalidOperationException("Build the cypress and church before configuring the route.");
   var focus=church.center;var treeBase=new Vector3(tree.center.x,tree.min.y,tree.center.z);
   Vector3 GroundEye(Vector3 p,float height){p.y=StarryWorldLayout.GroundHeight(p.x,p.z)+height;return p;}
   var hero=controller.HeroPosition;var heroForward=Quaternion.Euler(controller.HeroEuler)*Vector3.forward;
   var positions=new[]{hero,hero+new Vector3(-10,8,60),focus+new Vector3(-55,15,-210),GroundEye(focus+new Vector3(-80,0,-130),13),GroundEye(focus+new Vector3(-90,0,0),15),new Vector3(-170,100,440),new Vector3(-250,140,270)};
   var focuses=new[]{hero+heroForward*300,hero+new Vector3(-10,8,60)+heroForward*300,focus+Vector3.up*10,focus+Vector3.up*5,focus+new Vector3(0,-4,0),new Vector3(60,-25,350),new Vector3(10,-25,270)};
   float[] times={0,7,18,34,50,68,80},fovs={53,53,52,46,56,58,62};
   string[] names={"原画构图建立全景","沿笔触缓慢入画","越过高地进入山谷","接近教堂与建筑立面","沿街横移展示纵深","升高环绕屋顶与果园","全景缓停"};
   tour.Keys=new StarryCinematicTour.ShotKey[times.Length];
   for(int i=0;i<times.Length;i++){
    var point=new GameObject(i.ToString("D2")+" "+names[i]).transform;point.SetParent(root.transform,false);point.position=positions[i];var target=new GameObject("Look at").transform;target.SetParent(point,false);target.position=focuses[i];
    tour.Keys[i]=new StarryCinematicTour.ShotKey{Name=names[i],Time=times[i],CameraPoint=point,LookPoint=target,FieldOfView=fovs[i]};
   }
   controller.Cinematic=tour;controller.UseCinematicOpening=true;tour.Sample(0,out controller.OpeningPosition,out var openingRotation,out controller.OpeningFieldOfView);controller.OpeningEuler=openingRotation.eulerAngles;
   controller.View.name="Roaming and cinematic camera";controller.ResetView();controller.View.aspect=controller.CompositionAspect;controller.View.rect=new Rect(0,0,1,1);
   EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
  }
 }
}
#endif
