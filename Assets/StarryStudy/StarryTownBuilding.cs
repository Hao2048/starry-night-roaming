using UnityEngine;
namespace StarryStudy
{
 public sealed class StarryTownBuilding:MonoBehaviour
 {
  public bool IsChurch;
  public Bounds WorldBounds;
  public Bounds GetWorldBounds(){var renderers=GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)return WorldBounds;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);bounds.Expand(3);return bounds;}
 }
}
