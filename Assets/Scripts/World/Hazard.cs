using Kami.Visual;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>Tears Kami on touch: fire, bamboo spikes, ink pits. Small fires can be blown out by the fan.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] string reason = "Burned! Paper and fire don't mix.";
        [SerializeField] bool extinguishable;
        [SerializeField] Sprite puffSprite;
        [SerializeField] Material puffMaterial;

        public bool Extinguishable => extinguishable;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other) => Touch(other);

        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController kami) && !kami.IsTorn)
                kami.Tear(reason);
        }

        public void Extinguish()
        {
            if (!extinguishable)
                return;
            if (puffSprite != null)
                OneShotFx.Spawn(puffSprite, puffMaterial, transform.position + Vector3.up * 0.4f, 0.4f, 0.6f, 1.2f);
            Destroy(gameObject);
        }
    }
}
