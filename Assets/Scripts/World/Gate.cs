using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>Stone gate: slides up while any linked switch is held, and back down when released.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Gate : MonoBehaviour
    {
        [SerializeField] FloorSwitch[] switches;
        [SerializeField] float openHeight = 3f;
        [SerializeField] float speed = 4f;

        Rigidbody2D body;
        Vector2 closed;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            closed = body.position;
        }

        void FixedUpdate()
        {
            bool open = false;
            foreach (var s in switches)
                open |= s != null && s.IsPressed;
            var target = closed + (open ? Vector2.up * openHeight : Vector2.zero);
            body.MovePosition(Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime));
        }
    }
}
