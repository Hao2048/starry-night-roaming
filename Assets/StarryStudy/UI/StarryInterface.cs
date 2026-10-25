using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StarryStudy
{
 [DefaultExecutionOrder(-60)]
 public sealed class StarryInterface:MonoBehaviour
 {
  public StarryStudyController Controller;
  public Canvas Canvas;
  public GameObject Cover,HUD,PauseSheet,HelpSheet;
  public CanvasGroup CoverInformation,CoverBackground,HUDOpacity;
  public RectTransform Artwork;
  public UnityEngine.UI.Image ArtworkFrame;
  public UnityEngine.UI.RawImage CoverArtwork;
  Material coverMaterial;
  int sceneMask;CameraClearFlags sceneClear;Color sceneBackground;bool cameraCached;
  public TextMeshProUGUI Status;
  public bool InCover {get;private set;}
  public bool IsPaused {get;private set;}
  public bool InterfaceHidden {get;private set;}
  public bool Entering {get;private set;}
  public bool AwaitingOpening {get;private set;}
  bool automatic,helpFromPause;
  float enterTime,hudTime;
  Vector2 artworkMin,artworkMax;
  void Awake(){CacheCamera();if(CoverArtwork!=null&&CoverArtwork.material!=null){coverMaterial=new Material(CoverArtwork.material);CoverArtwork.material=coverMaterial;}Controller.Interface=this;Controller.InputLocked=true;Controller.Opening.PlayOnStart=false;artworkMin=Artwork.anchorMin;artworkMax=Artwork.anchorMax;}
  void CacheCamera(){if(cameraCached||Controller==null)return;sceneMask=Controller.View.cullingMask;sceneClear=Controller.View.clearFlags;sceneBackground=Controller.View.backgroundColor;cameraCached=true;}
  void RestoreCamera(){CacheCamera();Controller.View.enabled=true;Controller.View.cullingMask=sceneMask;Controller.View.clearFlags=sceneClear;Controller.View.backgroundColor=sceneBackground;}
  void Start()=>ShowCover();
  public void ShowCover()
  {
   CacheCamera();Controller.ResetView();Controller.InputLocked=true;Controller.View.enabled=true;Controller.View.cullingMask=1<<5;Controller.View.clearFlags=CameraClearFlags.SolidColor;Controller.View.backgroundColor=new Color(.008f,.018f,.035f);InCover=true;IsPaused=false;Entering=AwaitingOpening=false;InterfaceHidden=false;
   Cover.SetActive(true);HUD.SetActive(false);PauseSheet.SetActive(false);HelpSheet.SetActive(false);CoverInformation.interactable=CoverInformation.blocksRaycasts=true;CoverInformation.alpha=CoverBackground.alpha=1;Artwork.anchorMin=artworkMin;Artwork.anchorMax=artworkMax;Artwork.offsetMin=Artwork.offsetMax=Vector2.zero;if(ArtworkFrame!=null)ArtworkFrame.color=new Color(.75f,.65f,.40f,.28f);Cursor.visible=true;Cursor.lockState=CursorLockMode.None;if(coverMaterial!=null)coverMaterial.SetFloat("_Veil",1);
  }
  public void EnterWalk()=>Enter(false);
  public void EnterFilm()=>Enter(true);
  public void Enter(bool film)
  {
   if(!InCover||Entering||AwaitingOpening)return;automatic=film;Entering=true;enterTime=0;CoverInformation.blocksRaycasts=false;RestoreCamera();Controller.InputLocked=true;
  }
  public void Pause()
  {
   if(InCover||Entering||AwaitingOpening)return;IsPaused=true;PauseSheet.SetActive(true);Controller.InputLocked=true;Controller.TourPaused=Controller.Tour;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
  }
  public void Resume()
  {
   IsPaused=false;PauseSheet.SetActive(false);HelpSheet.SetActive(false);Controller.InputLocked=false;Controller.TourPaused=false;
  }
  public void ResetView(){Resume();Controller.ResetView();}
  public void ToggleTour()
  {
   Resume();if(Controller.Tour)Controller.StopTour();else Controller.StartTour();
  }
  public void StartFilm(){Resume();Controller.StartTour();}
  public void OpenHelp()
  {
   helpFromPause=IsPaused;if(!InCover)Pause();PauseSheet.SetActive(false);HelpSheet.SetActive(true);
  }
  public void CloseHelp(){HelpSheet.SetActive(false);if(InCover)return;if(helpFromPause)PauseSheet.SetActive(true);else Resume();}
  public void ToggleInterface(){if(InCover||Entering||AwaitingOpening)return;InterfaceHidden=!InterfaceHidden;HUD.SetActive(!InterfaceHidden);}
  public void ReplayOpening()
  {
   if(InCover||Entering||AwaitingOpening)return;Resume();automatic=false;Controller.ReplayOpening();Controller.InputLocked=true;AwaitingOpening=true;HUD.SetActive(false);
  }
  public void SampleCoverExpansion(float progress)
  {
   float x=Mathf.Clamp01(progress),s=x*x*x*(x*(x*6-15)+10);var rect=Controller.View.rect;
   Artwork.anchorMin=Vector2.Lerp(artworkMin,rect.min,s);Artwork.anchorMax=Vector2.Lerp(artworkMax,rect.max,s);Artwork.offsetMin=Artwork.offsetMax=Vector2.zero;
   CoverInformation.alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.65f,x));CoverBackground.alpha=1;if(coverMaterial!=null)coverMaterial.SetFloat("_Veil",1-s);if(ArtworkFrame!=null)ArtworkFrame.color=new Color(.75f,.65f,.40f,.28f*(1-s));
  }
  void OnDestroy(){if(coverMaterial!=null)Destroy(coverMaterial);}
  void Update()
  {
   var keys=Keyboard.current;
   if(Entering){enterTime+=Time.unscaledDeltaTime;SampleCoverExpansion(enterTime/.95f);if(enterTime>=.95f){Entering=false;InCover=false;Cover.SetActive(false);Controller.ReplayOpening();AwaitingOpening=true;}return;}
   if(AwaitingOpening){if(Controller.Opening.IsPlaying)return;AwaitingOpening=false;Controller.InputLocked=false;HUD.SetActive(!InterfaceHidden);HUDOpacity.alpha=0;hudTime=0;if(automatic){Controller.Tour=true;Controller.TourTime=0;Controller.TourPaused=false;}return;}
   if(keys!=null){if(keys.escapeKey.wasPressedThisFrame){if(HelpSheet.activeSelf)CloseHelp();else if(!InCover){if(IsPaused)Resume();else Pause();}}if(keys.hKey.wasPressedThisFrame){if(HelpSheet.activeSelf)CloseHelp();else OpenHelp();}if(keys.f10Key.wasPressedThisFrame)ToggleInterface();if(keys.oKey.wasPressedThisFrame&&!InCover&&!IsPaused)ReplayOpening();}
   if(InCover){if(coverMaterial!=null)coverMaterial.SetFloat("_CoverTime",Time.unscaledTime);if(keys!=null&&(keys.enterKey.wasPressedThisFrame||keys.numpadEnterKey.wasPressedThisFrame)&&!HelpSheet.activeSelf)EnterWalk();return;}hudTime+=Time.unscaledDeltaTime;HUDOpacity.alpha=Mathf.SmoothStep(0,1,hudTime/.6f);
   if(Status!=null){if(Controller.Tour){int elapsed=Mathf.FloorToInt(Controller.TourTime);Status.text=$"自动运镜  {elapsed/60:00}:{elapsed%60:00} / 01:20";}else Status.text="自由漫游";}
  }
 }
}
