using UnityEngine;
using UnityEngine.InputSystem;

namespace StarryStudy
{
 // Explicit artwork opening hands over to the existing live mesh scene.
 public sealed class StarryStudyController:MonoBehaviour
 {
  public Camera View;
  public Vector3 HeroPosition;
  public Vector3 HeroEuler;
  public bool UseCinematicOpening=true;
  public float CompositionAspect=1.255f;
  public Vector3 OpeningPosition,OpeningEuler;
  public float OpeningFieldOfView=58;
  public StarryCinematicTour Cinematic;
  public StarryPaintingTransition Opening;
  public StarryInterface Interface;
  public bool InputLocked;
  public bool TourPaused;
  bool referenceView;
  public string Phase {get;private set;}="Roaming";
  public float AverageFrameMilliseconds {get;private set;}
  public bool Tour;
  public bool FlowEnabled=true;
  float brushTime;
  public float TourTime;
  float yaw,pitch,performanceElapsed,performanceMeasured;
  int performanceFrames;
  bool performanceReported;
  Vector3 velocity;
  void Start(){View.farClipPlane=6000;Phase="Roaming";Application.targetFrameRate=60;Application.runInBackground=true;QualitySettings.vSyncCount=0;if(Cinematic==null)Cinematic=Object.FindAnyObjectByType<StarryCinematicTour>();if(!Application.isEditor)Screen.SetResolution(1255,1000,FullScreenMode.Windowed);ResetView();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;FitComposition();if(Opening==null)Opening=GetComponent<StarryPaintingTransition>();if(Opening!=null&&Opening.PlayOnStart)ReplayOpening();}
  void FitComposition(){float a=Screen.width/(float)Mathf.Max(1,Screen.height),target=referenceView?1.255f:CompositionAspect;View.rect=a>target?new Rect((1-target/a)/2,0,target/a,1):new Rect(0,(1-a/target)/2,1,a/target);}
  public void Enter()=>ResetView();
  public void ShowScene(){Phase="Roaming";}
  public void Replay()=>ResetView();
  void SetPose(Vector3 position,Vector3 euler,float fov){View.transform.SetPositionAndRotation(position,Quaternion.Euler(euler));View.fieldOfView=fov;yaw=euler.y;pitch=Mathf.DeltaAngle(0,euler.x);velocity=Vector3.zero;Tour=false;TourPaused=false;TourTime=0;performanceElapsed=performanceMeasured=0;performanceFrames=0;performanceReported=false;brushTime=0;Shader.SetGlobalFloat("_BrushFlowTime",0);}
  public void ResetView(){if(Opening!=null)Opening.Stop();Phase="Roaming";referenceView=false;if(UseCinematicOpening&&Cinematic!=null){Cinematic.Sample(0,out var p,out var r,out float fov);SetPose(p,r.eulerAngles,fov);}else SetPose(HeroPosition,HeroEuler,53);}
  public void ShowReferenceView(){referenceView=true;SetPose(HeroPosition,HeroEuler,53);}
  public void StartTour(){ResetView();Tour=true;TourPaused=false;TourTime=0;}
  public void ReplayOpening(){if(Opening!=null){Opening.Begin(this);Phase=Opening.IsPlaying?"Opening":"Roaming";}}
  public void StopTour(){Tour=false;TourPaused=false;yaw=View.transform.eulerAngles.y;pitch=Mathf.DeltaAngle(0,View.transform.eulerAngles.x);velocity=Vector3.zero;}

  void Update()
  {
   if(Opening!=null&&Opening.IsPlaying){Opening.Advance(Time.unscaledDeltaTime);brushTime=Opening.FlowTime;FitComposition();var keys=Keyboard.current;if(keys!=null){if(keys.escapeKey.wasPressedThisFrame||keys.rKey.wasPressedThisFrame)ResetView();else if(keys.tKey.wasPressedThisFrame)StartTour();}if(!Opening.IsPlaying)Phase="Roaming";return;}
   if(InputLocked){FitComposition();return;}
   brushTime+=Time.deltaTime;Shader.SetGlobalFloat("_BrushFlowTime",FlowEnabled?brushTime:0);
   FitComposition();AverageFrameMilliseconds=Mathf.Lerp(AverageFrameMilliseconds,Time.unscaledDeltaTime*1000,.03f);
   if(!performanceReported){performanceElapsed+=Time.unscaledDeltaTime;if(performanceElapsed>1.5f){performanceMeasured+=Time.unscaledDeltaTime;performanceFrames++;}if(performanceMeasured>=4){Debug.Log($"REFERENCE_RIBBON_PERF frames={performanceFrames} average_ms={performanceMeasured*1000/performanceFrames:F2} resolution={Screen.width}x{Screen.height}");performanceReported=true;}}
   var kb=Keyboard.current;var mouse=Mouse.current;
   if(kb!=null){if(kb.oKey.wasPressedThisFrame&&Interface==null){ReplayOpening();return;}if(kb.rKey.wasPressedThisFrame||(kb.escapeKey.wasPressedThisFrame&&Interface==null))ResetView();if(kb.vKey.wasPressedThisFrame)ShowReferenceView();if(kb.fKey.wasPressedThisFrame)FlowEnabled=!FlowEnabled;if(kb.pKey.wasPressedThisFrame&&Tour){TourPaused=!TourPaused;if(TourPaused){performanceElapsed=performanceMeasured=0;performanceFrames=0;performanceReported=false;}}if(kb.tKey.wasPressedThisFrame){if(Tour)StopTour();else StartTour();}}
   if(Tour&&Cinematic!=null){if(!TourPaused)TourTime+=Time.deltaTime;Cinematic.Apply(View,TourTime);if(TourTime>=Cinematic.Duration)StopTour();return;}
   if(mouse!=null&&mouse.rightButton.isPressed){var delta=mouse.delta.ReadValue();yaw+=delta.x*.065f;pitch=Mathf.Clamp(pitch-delta.y*.065f,-89,89);View.transform.rotation=Quaternion.Euler(pitch,yaw,0);}
   Vector3 input=Vector3.zero;
   if(kb!=null){if(kb.wKey.isPressed)input.z++;if(kb.sKey.isPressed)input.z--;if(kb.dKey.isPressed)input.x++;if(kb.aKey.isPressed)input.x--;if(kb.eKey.isPressed)input.y++;if(kb.qKey.isPressed)input.y--;}
   var target=Quaternion.Euler(0,yaw,0)*Vector3.ClampMagnitude(input,1)*(kb!=null&&kb.leftShiftKey.isPressed?20:8);
   velocity=Vector3.Lerp(velocity,target,1-Mathf.Exp(-Time.deltaTime*7));var next=View.transform.position+velocity*Time.deltaTime;
   var planar=new Vector2(next.x-HeroPosition.x,next.z-HeroPosition.z);if(planar.magnitude>StarryWorldLayout.TravelRadius){planar=planar.normalized*StarryWorldLayout.TravelRadius;next.x=HeroPosition.x+planar.x;next.z=HeroPosition.z+planar.y;}next.y=Mathf.Clamp(next.y,StarryWorldLayout.GroundHeight(next.x,next.z)+2.2f,600);
   View.transform.position=next;
  }
  void OnDestroy(){Shader.SetGlobalFloat("_BrushFlowTime",0);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 }
}
