using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public class Waypoint : MonoBehaviour
    {
        public Renderer marker;
        public Collider click;

        public void Show(bool visible)
        {
            if (marker != null)
                marker.enabled = visible;
            if (click != null)
                click.enabled = visible;
        }
    }
}
