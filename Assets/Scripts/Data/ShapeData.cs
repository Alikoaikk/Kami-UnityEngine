using UnityEngine;

namespace Kami.Gameplay
{
    public enum ShapeId { Ninja, Frog, Crane, Boat, Mouse, Box, Fish, Fan, Umbrella, Plane }

    public enum ShapeAbility { None, PaperStar, FanGust, PlaneDash }

    /// <summary>
    /// One of Kami's fold shapes. Every tuning number lives here (GDD section 3), in tiles and tiles/s,
    /// so balancing never needs a code change. The assets live in Assets/Data/Shapes.
    /// </summary>
    [CreateAssetMenu(fileName = "Shape_", menuName = "Kami/Shape Data")]
    public class ShapeData : ScriptableObject
    {
        public ShapeId id;
        public string displayName;
        [Tooltip("Shown on the belt / unlock screen.")]
        public string strength;
        public string weakness;

        [Header("Look")]
        public Sprite sprite;
        [Tooltip("Optional: looped while moving on the ground.")]
        public Sprite[] runFrames;
        public Sprite jumpSprite;
        public Sprite fallSprite;
        public Sprite hurtSprite;
        public Sprite abilitySprite;

        [Header("Body (tiles)")]
        public Vector2 colliderSize = new(0.6f, 1.5f);

        [Header("Movement (tiles/s, tiles)")]
        public float landSpeed = 6f;
        [Tooltip("Speed while floating or swimming. Only used by shapes that float or swim.")]
        public float waterSpeed;
        public float jumpHeight = 2.3f;
        [Tooltip("Max fall speed while gliding. 0 = no glide.")]
        public float glideFallSpeed;

        [Header("Paper rules")]
        public bool floats;
        public bool swims;
        public float swimSeconds = 6f;
        public bool ridesWind;
        [Tooltip("Lifted by wind but pushed around by it too (umbrella).")]
        public bool blownByWind;
        [Tooltip("Holds floor switches down; sinks fast.")]
        public bool heavy;
        public bool blotsBounceOff;
        public bool blocksDrips;

        [Header("Ability (F / left click)")]
        public ShapeAbility ability;
        public float abilityCooldown = 0.35f;
    }
}
