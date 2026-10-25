#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem.UI;
namespace StarryStudy
{
 public static class StarryInterfaceBuilder
 {
  static TMP_FontAsset font;
  static Color Ink=>C("EAE4D2");static Color Muted=>C("A3B1B8");static Color Gold=>C("C8B17B");
  static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
  static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
  {
   var existing=parent.Find(name);var rt=existing!=null?(RectTransform)existing:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(existing==null)rt.SetParent(parent,false);
   rt.anchorMin=new Vector2(x0,y0);rt.anchorMax=new Vector2(x1,y1);rt.pivot=new Vector2(.5f,.5f);rt.offsetMin=rt.offsetMax=Vector2.zero;return rt;
  }
  static T Add<T>(GameObject go)where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
  static RectTransform Panel(string name,Transform parent,Color color,float x0=0,float y0=0,float x1=1,float y1=1,bool block=false)
  {
   var rt=Rect(name,parent,x0,y0,x1,y1);var image=Add<UnityEngine.UI.Image>(rt.gameObject);image.color=color;image.raycastTarget=block;return rt;
  }
  static TextMeshProUGUI Label(string name,Transform parent,string text,float x0,float y0,float x1,float y1,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft)
  {
   var rt=Rect(name,parent,x0,y0,x1,y1);var label=Add<TextMeshProUGUI>(rt.gameObject);label.font=font;label.text=text;label.fontSize=size;label.color=color;label.alignment=alignment;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;return label;
  }
  static UnityEngine.UI.Button Button(string name,Transform parent,string text,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction action,bool primary=false)
  {
   var rt=Panel(name,parent,primary?Gold:new Color(.11f,.18f,.25f,.87f),x0,y0,x1,y1,true);var button=Add<UnityEngine.UI.Button>(rt.gameObject);button.targetGraphic=rt.GetComponent<UnityEngine.UI.Image>();var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.selectedColor=new Color(1.06f,1.06f,1.06f);colors.pressedColor=new Color(.78f,.84f,.88f);button.colors=colors;
   while(button.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(button.onClick,0);UnityEventTools.AddPersistentListener(button.onClick,action);
   var label=Label("Label",rt,text,.08f,0,.92f,1,22,primary?C("152B3C"):Ink);label.fontStyle=primary?FontStyles.Bold:FontStyles.Normal;return button;
  }
  static void StyleCoverButton(UnityEngine.UI.Button button,Color normal)
  {
   button.GetComponent<UnityEngine.UI.Image>().color=Color.clear;var label=button.GetComponentInChildren<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Center;label.fontStyle=FontStyles.Normal;button.targetGraphic=label;var colors=button.colors;colors.normalColor=normal;colors.highlightedColor=Gold;colors.selectedColor=Gold;colors.pressedColor=Gold*.8f;colors.fadeDuration=.25f;button.colors=colors;label.color=Color.white;
  }
  static RectTransform Modal(string name,Transform parent)
  {
   var backdrop=Panel(name,parent,new Color(.015f,.035f,.065f,.86f),block:true);var sheet=Panel("Sheet",backdrop,C("12283B"));sheet.anchorMin=sheet.anchorMax=new Vector2(.5f,.5f);sheet.sizeDelta=new Vector2(620,650);sheet.anchoredPosition=Vector2.zero;return sheet;
  }
  [MenuItem("Starry Study/Configure single painting interface")]
  public static void Configure()
  {
   if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before editing the interface.");
   var controller=Object.FindAnyObjectByType<StarryStudyController>();if(controller==null)throw new System.InvalidOperationException("Open the starry scene.");
   StarryOpeningBuilder.Configure(false);
   font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/StarryStudy/UI/Fonts/ArtWalkFont.asset");if(font==null)throw new System.InvalidOperationException("UI font is missing.");
   font.TryAddCharacters("在笔触流动处走进梵高的夜按开始观看自动运镜星空漫游一幅画整个夜晚从沿着笔触走进梵高流动的夜文森特原作自动运镜自由暂停封面回到起始机位继续在里操作指南隐藏界面恢复画中观看重播返回关闭穿越村庄蓝色地平线年右键拖动观察升降加速显示切换流动结束次按解锁鼠标退出帮助录制适合记录点亮窗灯像素金色月亮→·—/0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz WASDQERHFOETP Esc Shift F10",out string missing);EditorUtility.SetDirty(font);
   var ui=controller.GetComponent<StarryInterface>();if(ui==null)ui=controller.gameObject.AddComponent<StarryInterface>();ui.Controller=controller;controller.Interface=ui;controller.Opening.PlayOnStart=false;controller.View.enabled=true;if((controller.View.cullingMask&1)==0)controller.View.cullingMask=~0;
   var canvasObject=GameObject.Find("Starry Night Interface");if(canvasObject==null)canvasObject=new GameObject("Starry Night Interface",typeof(RectTransform));var canvas=Add<Canvas>(canvasObject);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;var scaler=Add<UnityEngine.UI.CanvasScaler>(canvasObject);scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=.5f;Add<UnityEngine.UI.GraphicRaycaster>(canvasObject);ui.Canvas=canvas;
   if(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()==null)new GameObject("Starry Night Event System",typeof(UnityEngine.EventSystems.EventSystem),typeof(InputSystemUIInputModule));
   var cover=Rect("Cover",canvas.transform,0,0,1,1);ui.Cover=cover.gameObject;
   var background=Panel("Background",cover,C("0D2031"));ui.CoverBackground=Add<CanvasGroup>(background.gameObject);
   var information=Rect("Information",cover,0,0,1,1);ui.CoverInformation=Add<CanvasGroup>(information.gameObject);information.gameObject.SetActive(true);ui.CoverInformation.alpha=1;ui.CoverInformation.interactable=true;ui.CoverInformation.blocksRaycasts=true;ui.CoverBackground.alpha=1;
   var displayFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/StarryStudy/UI/Fonts/StarryTitle.asset");
   if(displayFont==null){var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/StarryStudy/UI/Fonts/NotoSerifCJKsc-Light.otf");if(source==null)throw new System.InvalidOperationException("The cover serif font is not imported.");displayFont=TMP_FontAsset.CreateFontAsset(source,96,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);displayFont.name="StarryTitle";AssetDatabase.CreateAsset(displayFont,"Assets/StarryStudy/UI/Fonts/StarryTitle.asset");AssetDatabase.AddObjectToAsset(displayFont.material,displayFont);foreach(var atlas in displayFont.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,displayFont);}
   displayFont.TryAddCharacters("星空漫游开始THE STARRY NIGHTVINCENTVANGOGH1889",out string missingTitle);EditorUtility.SetDirty(displayFont);
   Label("Brand",information,"VINCENT VAN GOGH",.055f,.925f,.52f,.97f,13,new Color(.90f,.90f,.84f,.72f)).characterSpacing=2.5f;
   Label("Edition",information,"1889",.72f,.925f,.945f,.97f,15,Gold,TextAlignmentOptions.MidlineRight);
   foreach(var obsolete in new[]{"HeaderRule","FooterRule","Artist","ArtworkCaption"}){var old=information.Find(obsolete);if(old!=null)old.gameObject.SetActive(false);}
   var overline=Label("Overline",information,"THE STARRY NIGHT",.12f,.625f,.88f,.677f,18,Gold,TextAlignmentOptions.Center);overline.font=displayFont;overline.characterSpacing=7;
   var title=Label("Title",information,"星空漫游",.08f,.478f,.92f,.625f,100,Ink,TextAlignmentOptions.Center);title.font=displayFont;title.fontStyle=FontStyles.Normal;title.characterSpacing=13;
   var titleShadow=Add<UnityEngine.UI.Shadow>(title.gameObject);titleShadow.effectColor=new Color(0,0,0,.33f);titleShadow.effectDistance=new Vector2(0,-2);
   Label("Invitation",information,"在笔触流动处，走进梵高的夜。",.15f,.410f,.85f,.46f,18,new Color(.90f,.91f,.87f,.72f),TextAlignmentOptions.Center);
   var entry=Button("EnterWalk",information,"开始漫游",.34f,.276f,.66f,.341f,ui.EnterWalk);var entryLabel=entry.GetComponentInChildren<TextMeshProUGUI>();entryLabel.font=displayFont;entryLabel.fontSize=30;entryLabel.fontStyle=FontStyles.Bold;entryLabel.characterSpacing=4;StyleCoverButton(entry,Ink);entryLabel.fontStyle=FontStyles.Bold;
   Panel("EntryUnderline",information,new Color(.78f,.69f,.48f,.58f),.445f,.268f,.555f,.2692f);
   var auto=Button("EnterFilm",information,"观看自动运镜",.34f,.202f,.66f,.253f,ui.EnterFilm);auto.GetComponentInChildren<TextMeshProUGUI>().fontSize=16;auto.GetComponentInChildren<TextMeshProUGUI>().characterSpacing=1.7f;StyleCoverButton(auto,new Color(.86f,.88f,.84f,.90f));
   var ornament=Panel("StarMark",information,Gold);ornament.anchorMin=ornament.anchorMax=new Vector2(.5f,.735f);ornament.anchoredPosition=Vector2.zero;ornament.sizeDelta=new Vector2(5,5);ornament.localRotation=Quaternion.Euler(0,0,45);
   var oldAxis=information.Find("QuietAxis");if(oldAxis!=null)oldAxis.gameObject.SetActive(false);
   var footer=information.Find("Footer");if(footer!=null)footer.gameObject.SetActive(false);
   Label("EnterHint",information,"按 Enter 开始",.34f,.092f,.66f,.131f,12,new Color(.85f,.87f,.84f,.52f),TextAlignmentOptions.Center).characterSpacing=2;
   var guide=Button("CoverHelp",information,"操作指南  H",.78f,.027f,.945f,.073f,ui.OpenHelp);guide.GetComponentInChildren<TextMeshProUGUI>().fontSize=14;StyleCoverButton(guide,new Color(.85f,.87f,.84f,.62f));
   var art=Rect("Artwork",cover,0,0,1,1);art.SetSiblingIndex(1);information.SetAsLastSibling();ui.Artwork=art;var imageRect=Rect("OriginalArtwork",art,0,0,1,1);var raw=Add<UnityEngine.UI.RawImage>(imageRect.gameObject);raw.texture=controller.Opening.SourcePainting;raw.raycastTarget=false;ui.CoverArtwork=raw;var fitter=Add<UnityEngine.UI.AspectRatioFitter>(imageRect.gameObject);fitter.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;fitter.aspectRatio=1.255f;
   const string materialPath="Assets/StarryStudy/UI/CinematicCover.mat";var coverMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(coverMaterial==null){coverMaterial=new Material(Shader.Find("StarryStudy/CinematicCover"));AssetDatabase.CreateAsset(coverMaterial,materialPath);}coverMaterial.SetFloat("_Veil",1);coverMaterial.SetFloat("_CoverTime",0);raw.material=coverMaterial;EditorUtility.SetDirty(coverMaterial);
   var hud=Rect("QuietHUD",canvas.transform,0,0,1,1);ui.HUD=hud.gameObject;ui.HUDOpacity=Add<CanvasGroup>(hud.gameObject);
   Panel("TopShade",hud,new Color(.025f,.06f,.10f,.72f),.025f,.915f,.33f,.992f);
   Label("HUDTitle",hud,"星空 · 漫游",.035f,.942f,.33f,.985f,23,Ink);
   ui.Status=Label("HUDStatus",hud,"自由漫游",.035f,.915f,.42f,.946f,13,Gold);
   Button("HUDTour",hud,"运镜",.65f,.935f,.745f,.98f,ui.ToggleTour);
   Button("HUDPause",hud,"暂停",.76f,.935f,.855f,.98f,ui.Pause);
   Button("HUDCover",hud,"封面",.87f,.935f,.965f,.98f,ui.ShowCover);
   Panel("ControlShade",hud,new Color(.025f,.06f,.10f,.73f),.025f,.022f,.73f,.075f);
   Label("Controls",hud,"WASD 移动  ·  右键观察  ·  Q/E 升降  ·  Shift 加速",.041f,.028f,.725f,.071f,16,Ink);
   Button("HideHUD",hud,"F10  隐藏界面",.76f,.027f,.972f,.073f,ui.ToggleInterface);
   var pause=Modal("Pause",canvas.transform);ui.PauseSheet=pause.parent.gameObject;
   Label("PauseOverline",pause,"THE STARRY NIGHT",.10f,.85f,.90f,.91f,14,Gold);
   Label("PauseTitle",pause,"停在星空里",.10f,.73f,.90f,.86f,42,Ink);
   Panel("PauseRule",pause,new Color(.75f,.68f,.48f,.32f),.10f,.717f,.90f,.719f);
   Button("Resume",pause,"继续漫游     →",.10f,.58f,.90f,.68f,ui.Resume,true);
   Button("Film",pause,"自动运镜",.10f,.455f,.90f,.555f,ui.StartFilm);
   Button("Reset",pause,"回到起始机位",.10f,.33f,.90f,.43f,ui.ResetView);
   Button("Help",pause,"操作指南",.10f,.205f,.90f,.305f,ui.OpenHelp);
   Button("ReturnCover",pause,"返回封面",.10f,.08f,.90f,.18f,ui.ShowCover);
   var help=Modal("Help",canvas.transform);ui.HelpSheet=help.parent.gameObject;
   Label("HelpTitle",help,"在星夜里漫游",.10f,.78f,.90f,.93f,38,Ink);
   Label("HelpContent",help,"W A S D     移动\n右键拖动     观察\nQ / E       降低 / 升高\nShift       加速\nR           回到起始机位\nT / P       运镜 / 暂停运镜\nO           重播入画过渡\nF           开关笔触流动\nF10         隐藏 / 显示界面\nEsc         暂停 / 继续",.10f,.22f,.90f,.78f,22,Muted);
   Button("CloseHelp",help,"返回     →",.10f,.07f,.90f,.17f,ui.CloseHelp,true);
   foreach(var child in canvasObject.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;
   cover.gameObject.SetActive(true);hud.gameObject.SetActive(false);ui.PauseSheet.SetActive(false);ui.HelpSheet.SetActive(false);
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(controller.gameObject.scene.path,true)};PlayerSettings.productName="星空漫游";PlayerSettings.defaultScreenWidth=1255;PlayerSettings.defaultScreenHeight=1000;
   EditorUtility.SetDirty(ui);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.Opening);EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);EditorSceneManager.SaveScene(controller.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("STARRY_INTERFACE_READY one artwork, cover, quiet HUD, pause and guide");
  }
 }
}
#endif
