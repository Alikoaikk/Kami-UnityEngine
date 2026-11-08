using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>The torii gate at the end of a level: walk through it to finish.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class LevelGoal : MonoBehaviour
    {
        void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController kami) && !kami.IsTorn && LevelManager.Instance != null)
                LevelManager.Instance.CompleteLevel();
        }
    }
}
