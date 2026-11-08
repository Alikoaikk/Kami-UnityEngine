using System;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// The core rule: every fold adds a crease; reaching the level's limit tears Kami.
    /// Pressing stones smooth the creases back to 0. Folds (for par and stars) are counted separately
    /// and never reset during a level.
    /// </summary>
    public class CreaseCounter : MonoBehaviour
    {
        [SerializeField, Range(1, 12)] int limit = 8;

        public int Limit => limit;
        public int Creases { get; private set; }
        public int Folds { get; private set; }

        /// <summary>True for the last two marks before the limit (shown red in the HUD).</summary>
        public bool InWarning => Creases >= limit - 2;

        public event Action Changed;
        public event Action Torn;

        public void Setup(int creaseLimit)
        {
            limit = Mathf.Clamp(creaseLimit, 1, 12);
            Creases = 0;
            Folds = 0;
            Changed?.Invoke();
        }

        /// <summary>Adds the crease for one fold. Returns false if this fold tore Kami.</summary>
        public bool AddCrease()
        {
            Creases++;
            Folds++;
            Changed?.Invoke();
            if (Creases < limit)
                return true;
            Torn?.Invoke();
            return false;
        }

        public void Smooth()
        {
            Creases = 0;
            Changed?.Invoke();
        }
    }
}
