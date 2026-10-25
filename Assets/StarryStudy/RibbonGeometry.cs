using UnityEngine;

namespace StarryStudy
{
    // Identifies visible geometry built exclusively from closed three-dimensional paint ribbons.
    // Collision/reference surfaces have no such marker and must not render in the painted world.
    public sealed class RibbonGeometry : MonoBehaviour
    {
        public string Role;
        public int StrokeCount;
        public float LongestStrokeLength;
    }
}
