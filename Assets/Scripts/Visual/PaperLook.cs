using UnityEngine;

namespace Kami.Visual
{
    /// <summary>
    /// Shows a paper sprite's state through the Kami/Paper Sprite shader (use M_Kami):
    /// crease lines (one per fold, red within 2 of the limit), tearing apart, ink stain and a flash.
    /// Values go through a MaterialPropertyBlock, so every renderer can differ while sharing one material.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PaperLook : MonoBehaviour
    {
        [Range(0, 12)] public int creases;
        [Range(1, 12)] public int creaseLimit = 8;
        [Tooltip("0 = whole, 1 = torn fully apart. Animate it to 1 when Kami tears.")]
        [Range(0, 1)] public float tear;
        [Tooltip("Ink stain coverage, e.g. 0.6 while an Ink Blot's touch blocks folding.")]
        [Range(0, 1)] public float stain;
        [Range(0, 1)] public float flash;
        [Tooltip("How fast the flash fades, per second (Play mode only).")]
        public float flashFade = 6f;

        static readonly int CreasesId = Shader.PropertyToID("_Creases");
        static readonly int CreaseLimitId = Shader.PropertyToID("_CreaseLimit");
        static readonly int TearId = Shader.PropertyToID("_Tear");
        static readonly int StainId = Shader.PropertyToID("_Stain");
        static readonly int FlashId = Shader.PropertyToID("_Flash");

        SpriteRenderer spriteRenderer;
        MaterialPropertyBlock block;

        /// <summary>Every fold: one more crease and a white flash.</summary>
        public void Fold()
        {
            creases = Mathf.Min(creases + 1, 12);
            flash = 1f;
        }

        /// <summary>Pressing stone: creases back to 0.</summary>
        public void Smooth()
        {
            creases = 0;
            flash = 1f;
        }

        void OnEnable() => Apply();

        void OnValidate() => Apply();

        void LateUpdate()
        {
            if (Application.isPlaying && flash > 0f)
                flash = Mathf.MoveTowards(flash, 0f, flashFade * Time.deltaTime);
            Apply();
        }

        void OnDisable()
        {
            if (spriteRenderer != null)
                spriteRenderer.SetPropertyBlock(null);
        }

        public void Apply()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            block ??= new MaterialPropertyBlock();

            spriteRenderer.GetPropertyBlock(block);
            block.SetFloat(CreasesId, creases);
            block.SetFloat(CreaseLimitId, creaseLimit);
            block.SetFloat(TearId, tear);
            block.SetFloat(StainId, stain);
            block.SetFloat(FlashId, flash);
            spriteRenderer.SetPropertyBlock(block);
        }
    }
}
