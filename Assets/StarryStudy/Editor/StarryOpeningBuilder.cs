#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace StarryStudy
{
 public static class StarryOpeningBuilder
 {
  [MenuItem("Starry Study/Configure painting opening transition")]
  public static void FromMenu()=>Configure();
  public static void Configure(bool save=true)
  {
   if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before configuring the opening.");
   const string path="Assets/StarryStudy/Opening/StarryNight-opening.jpg";
   AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   var controller=Object.FindAnyObjectByType<StarryStudyController>();if(controller==null)throw new System.InvalidOperationException("Open the starry preview scene.");
   var transition=controller.GetComponent<StarryPaintingTransition>();if(transition==null)transition=controller.gameObject.AddComponent<StarryPaintingTransition>();
   transition.SourcePainting=AssetDatabase.LoadAssetAtPath<Texture2D>(path);transition.TransitionShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/StarryStudy/Shaders/PaintingHandoff.shader");transition.PlayOnStart=true;controller.Opening=transition;
   EditorUtility.SetDirty(controller);EditorUtility.SetDirty(transition);EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);if(save){EditorSceneManager.SaveScene(controller.gameObject.scene);AssetDatabase.SaveAssets();}
  }
 }
}
#endif
