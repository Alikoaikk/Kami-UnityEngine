using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// Kami's look, on the child sprite: picks the shape's sprite (run / jump / fall / throw / hurt frames
    /// when the shape has them, a paper-puppet hop and tilt when it doesn't), squash and stretch, the fold
    /// puff, and the crease / stain / tear state through PaperLook.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(PaperLook))]
    public class KamiVisual : MonoBehaviour
    {
        [SerializeField] KamiController kami;
        [Tooltip("Ground tiles have a 10 px torn edge; this sinks the feet onto the ink line.")]
        [SerializeField] float groundOffset = -0.06f;
        [SerializeField] float runFramesPerSecond = 9f;
        [SerializeField] float tearSeconds = 0.6f;

        [Header("Effects")]
        [SerializeField] Material fxMaterial;
        [SerializeField] Sprite foldPuff;
        [SerializeField] Sprite tearBurst;
        [SerializeField] Sprite paperScrap;
        [SerializeField] Sprite dust;

        SpriteRenderer spriteRenderer;
        PaperLook look;
        float runTime, squash, abilityShowTime, shakeTime, tearTime;
        bool tearing, wasGrounded = true;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            look = GetComponent<PaperLook>();
            if (kami == null)
                kami = GetComponentInParent<KamiController>();
        }

        public void SetShape(ShapeData shape)
        {
            if (spriteRenderer == null)
                Awake();
            spriteRenderer.sprite = shape.sprite;
            squash = -0.2f;
        }

        public void PlayFold()
        {
            look.flash = 1f;
            Spawn(foldPuff, 0.3f, 0.5f, 1.3f);
        }

        public void PlayJump() => squash = -0.18f;

        public void PlayAbility() => abilityShowTime = 0.2f;

        public void Shake() => shakeTime = 0.25f;

        public void Bump()
        {
            squash = 0.2f;
            look.flash = 0.6f;
        }

        public void PlayTear()
        {
            tearing = true;
            tearTime = 0f;
            Spawn(tearBurst, 0.45f, 0.4f, 1.2f);
            if (paperScrap == null)
                return;
            for (int i = 0; i < 8; i++)
            {
                var dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 1.2f));
                OneShotFx.Spawn(paperScrap, fxMaterial, kami.Center, 0.9f, 1f, 0.6f, 31, dir * 4f, Random.Range(-360f, 360f))
                         .gravity = 9f;
            }
        }

        public void ResetLook()
        {
            tearing = false;
            look.tear = 0f;
            look.stain = 0f;
            spriteRenderer.color = Color.white;
        }

        void LateUpdate()
        {
            if (kami == null || kami.Shape == null)
                return;

            var shape = kami.Shape;
            var velocity = kami.Velocity;
            float dt = Time.deltaTime;
            bool grounded = kami.IsGrounded || kami.IsFloating;
            bool moving = Mathf.Abs(velocity.x) > 0.3f;
            abilityShowTime -= dt;

            // Sprite choice.
            var sprite = shape.sprite;
            if (kami.StainTimeLeft > 0f && shape.hurtSprite != null)
                sprite = shape.hurtSprite;
            else if (abilityShowTime > 0f && shape.abilitySprite != null)
                sprite = shape.abilitySprite;
            else if (!grounded && velocity.y > 0.5f && shape.jumpSprite != null)
                sprite = shape.jumpSprite;
            else if (!grounded && velocity.y < -1f && shape.fallSprite != null)
                sprite = shape.fallSprite;
            else if (grounded && moving && shape.runFrames != null && shape.runFrames.Length > 0)
                sprite = shape.runFrames[(int)(runTime * runFramesPerSecond) % shape.runFrames.Length];
            spriteRenderer.sprite = sprite;
            spriteRenderer.flipX = kami.Facing < 0;

            // Paper-puppet motion for shapes without run frames.
            runTime = grounded && moving ? runTime + dt : 0f;
            bool puppet = shape.runFrames == null || shape.runFrames.Length == 0;
            float hop = 0f, tilt = 0f;
            if (puppet && grounded && moving && !kami.IsFloating)
            {
                hop = Mathf.Abs(Mathf.Sin(runTime * 12f)) * 0.07f;
                tilt = Mathf.Sin(runTime * 12f) * 4f;
            }
            if (kami.IsFloating)
                tilt = Mathf.Sin(Time.time * 2f) * 3f;
            else if (!grounded && shape.glideFallSpeed > 0f)
                tilt = Mathf.Clamp(-velocity.y * 2f, -8f, 8f) * kami.Facing;

            // Landing squash.
            if (grounded && !wasGrounded && !kami.IsFloating)
            {
                squash = 0.15f;
                Spawn(dust, 0.3f, 0.5f, 0.9f, Vector2.zero, transform.position);
            }
            wasGrounded = grounded;
            squash = Mathf.MoveTowards(squash, 0f, dt * 1.2f);

            float shake = 0f;
            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                shake = Mathf.Sin(Time.time * 70f) * 0.05f;
            }
            transform.localPosition = new Vector3(shake, groundOffset + hop, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -tilt);
            transform.localScale = new Vector3(1f + squash, 1f - squash, 1f);

            // Paper state.
            look.creases = kami.Creases.Creases;
            look.creaseLimit = kami.Creases.Limit;
            look.stain = Mathf.Clamp01(kami.StainTimeLeft / kami.StainSeconds) * 0.6f;
            if (tearing)
            {
                tearTime += dt;
                look.tear = Mathf.Clamp01(tearTime / tearSeconds);
                float fade = Mathf.Clamp01((tearTime - tearSeconds) / 0.4f);
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f - fade);
            }
            look.Apply();
        }

        void Spawn(Sprite sprite, float life, float from, float to, Vector2 velocity = default, Vector3? at = null)
        {
            if (sprite != null)
                OneShotFx.Spawn(sprite, fxMaterial, at ?? (Vector3)kami.Center, life, from, to, 30, velocity);
        }
    }
}
