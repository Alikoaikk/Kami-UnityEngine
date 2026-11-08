using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// A body of water (trigger). The top of the collider is the surface. Kami floats as the boat, swims as
    /// the fish, and sinks in every other shape. Pair it with a Square sprite using M_Water.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WaterVolume : MonoBehaviour
    {
        BoxCollider2D area;

        public float SurfaceY => area.bounds.max.y;

        void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }
    }
}
