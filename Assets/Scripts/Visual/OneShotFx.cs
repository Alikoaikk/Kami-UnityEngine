using UnityEngine;

namespace Kami.Visual
{
    /// <summary>
    /// A short sprite effect (fold puff, splat, sparkle, paper scraps): scales, drifts, spins, fades, then
    /// destroys itself. Spawn one with <see cref="Spawn"/>.
    /// </summary>
    public class OneShotFx : MonoBehaviour
    {
        public float lifetime = 0.3f;
        public float fromScale = 0.5f;
        public float toScale = 1.2f;
        public Vector2 velocity;
        public float spin;
        public float gravity;

        SpriteRenderer spriteRenderer;
        float age;

        public static OneShotFx Spawn(Sprite sprite, Material material, Vector3 position, float lifetime, float fromScale,
                                      float toScale, int sortingOrder = 30, Vector2 velocity = default, float spin = 0f,
                                      bool flipX = false)
        {
            var go = new GameObject("FX " + (sprite != null ? sprite.name : "?"));
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.flipX = flipX;
            if (material != null)
                sr.sharedMaterial = material;
            var fx = go.AddComponent<OneShotFx>();
            fx.lifetime = Mathf.Max(0.01f, lifetime);
            fx.fromScale = fromScale;
            fx.toScale = toScale;
            fx.velocity = velocity;
            fx.spin = spin;
            fx.spriteRenderer = sr;
            go.transform.localScale = Vector3.one * fromScale;
            return fx;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, 1f - (1f - t) * (1f - t));
            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = 1f - t * t;
                spriteRenderer.color = c;
            }
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
