using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>A rising wind column (trigger). Only the crane rides it; the umbrella gets blown around.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class WindColumn : MonoBehaviour
    {
        [SerializeField] float liftSpeed = 8f;
        [Tooltip("Sideways push for shapes that get blown around (umbrella), tiles/s².")]
        [SerializeField] float sidePush;

        public float LiftSpeed => liftSpeed;
        public float SidePush => sidePush;

        void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    }
}
