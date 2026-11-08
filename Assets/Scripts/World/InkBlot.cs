using System.Collections.Generic;
using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// Ink Blot: patrols at 1.3 tiles/s, turning at walls and ledges. Its touch stains Kami (no folding
    /// for 3 s) unless Kami is the box, which it bounces off. One paper star pops it. The fan pushes it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class InkBlot : MonoBehaviour
    {
        [SerializeField] float speed = 1.3f;
        [SerializeField] int direction = -1;
        [Tooltip("Turn around this far from the start, in tiles. 0 = only at walls and ledges.")]
        [SerializeField] float patrolRange;
        [SerializeField] Sprite splatSprite;
        [SerializeField] Material fxMaterial;

        Rigidbody2D body;
        BoxCollider2D box;
        SpriteRenderer spriteRenderer;
        Vector2 start;
        float pushTime;
        readonly List<RaycastHit2D> hits = new();
        static ContactFilter2D solid = new() { useTriggers = false };

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            body.bodyType = RigidbodyType2D.Kinematic;
            box.isTrigger = true;
            start = body.position;
        }

        void FixedUpdate()
        {
            var position = body.position;
            float halfWidth = box.size.x * 0.5f;

            if (pushTime > 0f)
            {
                pushTime -= Time.fixedDeltaTime;
                if (!Blocked(position + new Vector2(direction * halfWidth, box.size.y * 0.5f), new Vector2(direction, 0f), 0.1f))
                    body.MovePosition(position + new Vector2(direction * 6f * Time.fixedDeltaTime, 0f));
                return;
            }

            float step = speed * Time.fixedDeltaTime * direction;
            bool wall = Blocked(position + new Vector2(direction * halfWidth, box.size.y * 0.5f), new Vector2(direction, 0f), 0.1f);
            bool ground = Blocked(position + new Vector2(direction * (halfWidth + 0.1f), 0.1f), Vector2.down, 0.4f);
            bool outOfRange = patrolRange > 0f && Mathf.Abs(position.x + step - start.x) > patrolRange;
            if (wall || !ground || outOfRange)
                direction = -direction;
            else
                body.MovePosition(position + new Vector2(step, 0f));

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction > 0;
        }

        bool Blocked(Vector2 origin, Vector2 dir, float distance)
        {
            hits.Clear();
            Physics2D.Raycast(origin, dir, solid, hits, distance);
            foreach (var hit in hits)
                if (hit.collider != box && hit.collider.GetComponent<KamiController>() == null)
                    return true;
            return false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out KamiController kami))
                return;
            if (!kami.TryStain())
                Push(body.position.x < kami.transform.position.x ? -1 : 1);
        }

        /// <summary>Knocked back (fan gust, bouncing off the box).</summary>
        public void Push(int dir)
        {
            direction = dir;
            pushTime = 0.35f;
        }

        public void Pop()
        {
            if (splatSprite != null)
                OneShotFx.Spawn(splatSprite, fxMaterial, (Vector3)body.position + Vector3.up * 0.45f, 0.45f, 0.4f, 1f);
            Destroy(gameObject);
        }
    }
}
