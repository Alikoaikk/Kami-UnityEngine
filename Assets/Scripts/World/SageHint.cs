using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>The Paper Sage: shows the level's hint while Kami stands nearby.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SageHint : MonoBehaviour
    {
        [TextArea(2, 4)] [SerializeField] string hint;

        void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController _) && LevelManager.Instance != null)
                LevelManager.Instance.ShowHint(string.IsNullOrEmpty(hint) ? LevelManager.Instance.Level.sageHint : hint);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out KamiController _) && LevelManager.Instance != null)
                LevelManager.Instance.ShowHint(null);
        }
    }
}
