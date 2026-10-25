using UnityEngine;
using UnityEngine.Rendering;
namespace StarryStudy
{
 // The original artwork is explicit opening media. The live world renders underneath throughout.
 public sealed class StarryPaintingTransition:MonoBehaviour
 {
  public Texture2D SourcePainting;
  public Shader TransitionShader;
  public bool PlayOnStart=true;
  public float Hold=.85f,HandoffEnd=6.5f,Duration=7.3f;
  public bool IsPlaying {get;private set;}
  public float Elapsed {get;private set;}
  public float Reveal {get;private set;}
  public float FlowTime {get;private set;}
  StarryStudyController controller;
  GameObject surface;
  Material material;
  Vector3 destination;
  Quaternion orientation;
  float fieldOfView;
  static float Ease(float x){x=Mathf.Clamp01(x);return x*x*x*(x*(x*6-15)+10);}
  public void Begin(StarryStudyController owner)
  {
   Stop();owner.ResetView();controller=owner;
   if(SourcePainting==null||TransitionShader==null)return;
   destination=owner.View.transform.position;orientation=owner.View.transform.rotation;fieldOfView=owner.View.fieldOfView;
   material=new Material(TransitionShader){hideFlags=HideFlags.DontSave};material.SetTexture("_MainTex",SourcePainting);
   surface=GameObject.CreatePrimitive(PrimitiveType.Quad);surface.name="Opening painting - temporary handoff";surface.hideFlags=HideFlags.DontSave;
   var collider=surface.GetComponent<Collider>();collider.enabled=false;Release(collider);
   var renderer=surface.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
   surface.transform.SetParent(owner.View.transform,false);IsPlaying=true;Sample(0);
  }
  public void Advance(float seconds)=>Sample(Elapsed+Mathf.Max(0,seconds));
  public void Sample(float seconds)
  {
   if(!IsPlaying||controller==null)return;Elapsed=Mathf.Clamp(seconds,0,Duration);
   float progress=Ease(Mathf.InverseLerp(Hold,HandoffEnd,Elapsed));Reveal=progress;
   // Move less than one metre into the established composition and arrive at the exact starting pose.
   float move=Ease(Elapsed/Duration);controller.View.transform.SetPositionAndRotation(destination-orientation*Vector3.forward*(.9f*(1-move)),orientation);controller.View.fieldOfView=fieldOfView;
   FlowTime=Mathf.Max(0,Elapsed-1.2f)*.16f*progress;Shader.SetGlobalFloat("_BrushFlowTime",FlowTime);
   material.SetFloat("_Reveal",progress);material.SetFloat("_Push",.008f*Ease(Elapsed/HandoffEnd));
   float z=controller.View.nearClipPlane+.025f,height=2*z*Mathf.Tan(fieldOfView*Mathf.Deg2Rad*.5f);surface.transform.localPosition=new Vector3(0,0,z);surface.transform.localRotation=Quaternion.identity;surface.transform.localScale=new Vector3(height*controller.CompositionAspect,height,1);
   if(Elapsed>=Duration){controller.View.transform.SetPositionAndRotation(destination,orientation);Stop();}
  }
  public void Stop()
  {
   IsPlaying=false;if(surface!=null){surface.SetActive(false);Release(surface);surface=null;}if(material!=null){Release(material);material=null;}
  }
  static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
  void OnDisable()=>Stop();
 }
}
