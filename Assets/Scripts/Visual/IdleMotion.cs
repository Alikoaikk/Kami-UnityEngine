using UnityEngine;

namespace Kami.Visual
{
    /// <summary>
    /// Small ambient motion: bob up and down (pickups, clouds), sway (chimes, lanterns),
    /// and drift back and forth (koi, birds, butterflies), optionally flipping to face the way it moves.
    /// </summary>
    public class IdleMotion : MonoBehaviour
    {
        [Header("Bob")]
        public float bobHeight = 0.1f;
        public float bobSpeed = 2f;

        [Header("Sway")]
        public float swayDegrees;
        public float swaySpeed = 1.5f;

        [Header("Drift")]
        [Tooltip("Half the distance travelled back and forth, in units. 0 = no drift.")]
        public float driftRange;
        public float driftSpeed = 1f;
        public bool faceDriftDirection = true;

        Vector3 start;
        float phase;
        float lastX;
        SpriteRenderer spriteRenderer;

        void Awake()
        {
            start = transform.localPosition;
            phase = Random.value * 10f;
            lastX = start.x;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            float t = Time.time + phase;
            var p = start;
            p.y += Mathf.Sin(t * bobSpeed) * bobHeight;
            if (driftRange > 0f)
                p.x += Mathf.PingPong(t * driftSpeed, driftRange * 2f) - driftRange;
            transform.localPosition = p;

            if (swayDegrees != 0f)
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * swaySpeed) * swayDegrees);

            if (driftRange > 0f && faceDriftDirection && spriteRenderer != null && !Mathf.Approximately(p.x, lastX))
                spriteRenderer.flipX = p.x < lastX;
            lastX = p.x;
        }
    }
}
