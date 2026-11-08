using System.Collections.Generic;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>Floor switch: held down by a heavy shape (the box) or a crate. Opens linked gates.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FloorSwitch : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite upSprite;
        [SerializeField] Sprite downSprite;
        [Tooltip("Rigidbodies at least this heavy hold the switch (crates).")]
        [SerializeField] float minMass = 3f;

        readonly HashSet<Rigidbody2D> bodies = new();
        KamiController kami;

        public bool IsPressed { get; private set; }

        void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        void Update()
        {
            bodies.RemoveWhere(b => b == null);
            bool pressed = kami != null && !kami.IsTorn && kami.Shape != null && kami.Shape.heavy;
            foreach (var b in bodies)
                pressed |= b.mass >= minMass;
            IsPressed = pressed;
            if (spriteRenderer != null)
                spriteRenderer.sprite = pressed ? downSprite : upSprite;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController k))
                kami = k;
            else if (other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                bodies.Add(other.attachedRigidbody);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController k) && k == kami)
                kami = null;
            else if (other.attachedRigidbody != null)
                bodies.Remove(other.attachedRigidbody);
        }
    }
}
