using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>Follows Kami with a little look-ahead, and keeps the view inside the level.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new(0f, 1.6f);
        [SerializeField] float lookAhead = 2f;
        [SerializeField] float smoothTime = 0.22f;
        [Tooltip("World rectangle the view must stay inside.")]
        [SerializeField] Rect bounds = new(0f, -1f, 64f, 16f);

        Camera cam;
        KamiController kami;
        Vector3 velocity;
        float look;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target != null)
                kami = target.GetComponent<KamiController>();
        }

        void Start() => Snap();

        public void Snap()
        {
            if (target == null)
                return;
            transform.position = Desired();
            velocity = Vector3.zero;
        }

        void LateUpdate()
        {
            if (target != null)
                transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }

        Vector3 Desired()
        {
            if (kami != null)
                look = Mathf.MoveTowards(look, kami.Facing * lookAhead, Time.deltaTime * 4f);
            var p = (Vector2)target.position + offset + new Vector2(look, 0f);
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            p.x = bounds.width > halfW * 2f ? Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW) : bounds.center.x;
            p.y = bounds.height > halfH * 2f ? Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH) : bounds.center.y;
            return new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
