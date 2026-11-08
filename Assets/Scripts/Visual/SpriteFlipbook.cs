using UnityEngine;

namespace Kami.Visual
{
    /// <summary>
    /// Loops a few sprites: blot wobble, fire flicker, bat and moth flaps, ninja run.
    /// For state-driven character animation use an Animator instead.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] frames;
        public float framesPerSecond = 4f;
        [Tooltip("Start each copy at a random frame so groups don't animate in lockstep.")]
        public bool randomStart = true;

        SpriteRenderer spriteRenderer;
        float time;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (randomStart)
                time = Random.value * 10f;
        }

        void Update()
        {
            if (frames == null || frames.Length == 0)
                return;
            time += Time.deltaTime;
            spriteRenderer.sprite = frames[(int)(time * framesPerSecond) % frames.Length];
        }
    }
}
