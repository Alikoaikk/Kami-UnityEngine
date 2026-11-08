using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// Pressing stone: smooths Kami's creases back to 0, becomes the checkpoint, and lets Kami change
    /// the belt (Tab) while standing here.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PressingStone : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite inactiveSprite;
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite sparkle;
        [SerializeField] Material fxMaterial;

        public Vector2 RespawnPoint => transform.position;

        void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        public void SetActive(bool active)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = active ? activeSprite : inactiveSprite;
            if (active && sparkle != null)
            {
                for (int i = 0; i < 3; i++)
                    OneShotFx.Spawn(sparkle, fxMaterial, transform.position + new Vector3(-0.6f + i * 0.6f, 0.9f + (i % 2) * 0.3f),
                                    0.6f, 0.4f, 1f, 30, Vector2.up * 0.8f);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out KamiController kami) || kami.IsTorn)
                return;
            kami.StoneHere = this;
            if (LevelManager.Instance != null)
                LevelManager.Instance.ReachStone(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController kami) && kami.StoneHere == this)
                kami.StoneHere = null;
        }
    }
}
