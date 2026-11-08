using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>A golden crane: optional collectible, counted on the level-complete screen.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] Sprite sparkle;
        [SerializeField] Material fxMaterial;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void Start()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.RegisterCrane();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out KamiController kami) || kami.IsTorn)
                return;
            if (LevelManager.Instance != null)
                LevelManager.Instance.CollectCrane();
            if (sparkle != null)
                OneShotFx.Spawn(sparkle, fxMaterial, transform.position, 0.5f, 0.6f, 1.6f, 30, Vector2.up, 180f);
            Destroy(gameObject);
        }
    }
}
