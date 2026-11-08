using System.Collections.Generic;
using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// The ninja's paper star: flies straight (14 tiles/s, 8 tiles), pops the first Ink Blot it touches
    /// (1 hit), and stops on walls. Uses a circle cast each frame, so it needs no Rigidbody.
    /// </summary>
    public class PaperStar : MonoBehaviour
    {
        const float Radius = 0.18f;

        Vector2 direction;
        float speed, range, travelled;
        Collider2D owner;
        Material material;
        static readonly List<RaycastHit2D> Hits = new();
        static ContactFilter2D filter = new() { useTriggers = true };

        public static void Launch(Sprite sprite, Material material, Vector2 origin, int facing, float speed, float range,
                                  Collider2D owner)
        {
            var go = new GameObject("Paper Star");
            go.transform.position = origin;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 21;
            if (material != null)
                sr.sharedMaterial = material;
            var star = go.AddComponent<PaperStar>();
            star.direction = new Vector2(facing, 0f);
            star.speed = speed;
            star.range = range;
            star.owner = owner;
            star.material = material;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            Vector2 position = transform.position;

            Hits.Clear();
            Physics2D.CircleCast(position, Radius, direction, filter, Hits, step);
            Hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in Hits)
            {
                if (hit.collider == owner)
                    continue;
                if (hit.collider.GetComponentInParent<InkBlot>() is { } blot)
                {
                    blot.Pop();
                    Stop(hit.point);
                    return;
                }
                if (!hit.collider.isTrigger)
                {
                    Stop(hit.point);
                    return;
                }
            }

            transform.position = position + direction * step;
            transform.Rotate(0f, 0f, -900f * Time.deltaTime * direction.x);
            travelled += step;
            if (travelled >= range)
                Stop(transform.position);
        }

        void Stop(Vector2 at)
        {
            var sr = GetComponent<SpriteRenderer>();
            OneShotFx.Spawn(sr.sprite, material, at, 0.2f, 1f, 0.3f, 21, -direction * 1.5f, 400f);
            Destroy(gameObject);
        }
    }
}
