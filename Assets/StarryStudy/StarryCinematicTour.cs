using System;
using UnityEngine;
namespace StarryStudy
{
 public sealed class StarryCinematicTour:MonoBehaviour
 {
  [Serializable] public sealed class ShotKey {public string Name;public float Time;public Transform CameraPoint;public Transform LookPoint;[Range(25,90)]public float FieldOfView=56;}
  public ShotKey[] Keys=Array.Empty<ShotKey>();
  public float Duration=>Keys.Length==0?0:Keys[Keys.Length-1].Time;
  Vector3 Position(int i,bool focus)=>focus?Keys[i].LookPoint.position:Keys[i].CameraPoint.position;
  Vector3 Velocity(int i,bool focus){if(i==0||i==Keys.Length-1)return Vector3.zero;return (Position(i+1,focus)-Position(i-1,focus))/Mathf.Max(.01f,Keys[i+1].Time-Keys[i-1].Time);}
  Vector3 Interpolate(int i,float t,bool focus)
  {
   float span=Keys[i+1].Time-Keys[i].Time,t2=t*t,t3=t2*t;
   return Position(i,focus)*(2*t3-3*t2+1)+Velocity(i,focus)*span*(t3-2*t2+t)+Position(i+1,focus)*(-2*t3+3*t2)+Velocity(i+1,focus)*span*(t3-t2);
  }
  public void Sample(float seconds,out Vector3 position,out Quaternion rotation,out float fov)
  {
   if(Keys.Length<2){position=transform.position;rotation=transform.rotation;fov=56;return;}
   float time=Mathf.Clamp(seconds,0,Duration);int i=0;while(i<Keys.Length-2&&Keys[i+1].Time<time)i++;
   float t=Mathf.InverseLerp(Keys[i].Time,Keys[i+1].Time,time);position=Interpolate(i,t,false);position.y=Mathf.Max(position.y,StarryWorldLayout.GroundHeight(position.x,position.z)+3.5f);
   var focus=Interpolate(i,t,true);rotation=Quaternion.LookRotation((focus-position).normalized,Vector3.up);fov=Mathf.Lerp(Keys[i].FieldOfView,Keys[i+1].FieldOfView,Mathf.SmoothStep(0,1,t));
  }
  public void Apply(Camera camera,float time){Sample(time,out var position,out var rotation,out float fov);camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
  void OnDrawGizmosSelected()
  {
   if(Keys==null||Keys.Length<2)return;Gizmos.color=new Color(1,.75f,.23f);
   Sample(0,out var previous,out _,out _);for(float t=.5f;t<=Duration;t+=.5f){Sample(t,out var p,out _,out _);Gizmos.DrawLine(previous,p);previous=p;}
   foreach(var key in Keys){if(key.CameraPoint==null||key.LookPoint==null)continue;Gizmos.DrawSphere(key.CameraPoint.position,1.7f);Gizmos.color=new Color(.35f,.75f,1,.5f);Gizmos.DrawLine(key.CameraPoint.position,key.LookPoint.position);Gizmos.DrawWireSphere(key.LookPoint.position,2);Gizmos.color=new Color(1,.75f,.23f);}
  }
 }
}
