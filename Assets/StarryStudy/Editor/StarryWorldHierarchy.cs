#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace StarryStudy
{
 public static class StarryWorldHierarchy
 {
  static readonly string[] DistrictNames={"西侧居民街区","东侧坡地街区","上谷街区","东北农舍","西南道路街区","东南花园街区","西北山居","南侧入口街区"};
  public static void Organize(bool save=true)
  {
   var world=GameObject.Find("Reference fitted short-stroke world");if(world==null)return;
   var parents=new Dictionary<string,Transform>();
   Transform Parent(string name){if(parents.TryGetValue(name,out var p))return p;p=world.transform.Find(name);if(p==null){p=new GameObject(name).transform;p.SetParent(world.transform,false);}parents[name]=p;return p;}
   foreach(var marker in world.GetComponentsInChildren<RibbonGeometry>(true)){
    if(marker.Role.StartsWith("Town "))continue;
    string role=marker.Role,name="远景星空";
    if(role=="Foreground knoll"||role=="Reference fitted volume 2")name="00 前景高地与柏树";
    else if(role.StartsWith("Reference grove"))name="01 核心山谷村镇与果园";
    else if(role.StartsWith("Extended landscape")){
     int id=int.Parse(role.Substring(role.LastIndexOf(' ')+1));int district=id>=400&&id<424?(id-400)/3:id>=440&&id<448?id-440:id>=460&&id<468?id-460:-1;
     name=district>=0?(district+2).ToString("D2")+" "+DistrictNames[district]:id>=380?"11 环绕山脊":"10 全域地形";
    }else if(role.StartsWith("Reference fitted volume")){
     int id=int.Parse(role.Substring(role.LastIndexOf(' ')+1));name=id>=4&&id<=17||id>=60&&id<=65?"01 核心山谷村镇与果园":id>=50&&id<=55?"11 环绕山脊":"24 环绕笔触天空";
    }else name=role=="Main S current"?"20 主旋流":role=="Small vortex"?"21 小旋流":role=="Horizon wind"?"22 地平线云带":"23 星月与远蓝天空";
    marker.transform.SetParent(Parent(name),true);
   }
   if(save){EditorSceneManager.MarkSceneDirty(world.scene);EditorSceneManager.SaveScene(world.scene);}
  }
 }
}
#endif
