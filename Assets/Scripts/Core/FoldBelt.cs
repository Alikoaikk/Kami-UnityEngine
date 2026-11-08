using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>
    /// The 4-slot fold belt on keys 1-4. Slot 1 always holds the ninja; slots 2-4 can be changed
    /// at a pressing stone (Tab, then 2/3/4 to cycle through the level's swap pool).
    /// </summary>
    public class FoldBelt : MonoBehaviour
    {
        public const int Size = 4;

        [SerializeField] ShapeData[] slots = new ShapeData[Size];

        public event Action Changed;

        public ShapeData this[int index] => index >= 0 && index < Size ? slots[index] : null;

        public void Setup(IReadOnlyList<ShapeData> start)
        {
            slots = new ShapeData[Size];
            for (int i = 0; i < Size && start != null && i < start.Count; i++)
                slots[i] = start[i];
            Changed?.Invoke();
        }

        public int IndexOf(ShapeData shape) => Array.IndexOf(slots, shape);

        /// <summary>Puts the next shape from the pool into slot (1-3), skipping shapes already on the belt.</summary>
        public void Cycle(int index, IReadOnlyList<ShapeData> pool)
        {
            if (index < 1 || index >= Size || pool == null || pool.Count == 0)
                return;

            int start = slots[index] != null ? IndexInPool(pool, slots[index]) : -1;
            for (int step = 1; step <= pool.Count + 1; step++)
            {
                int next = start + step;
                // One extra position past the end means "empty slot".
                var candidate = next % (pool.Count + 1) == pool.Count ? null : pool[next % (pool.Count + 1)];
                if (candidate == null || IndexOf(candidate) < 0)
                {
                    slots[index] = candidate;
                    Changed?.Invoke();
                    return;
                }
            }
        }

        static int IndexInPool(IReadOnlyList<ShapeData> pool, ShapeData shape)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] == shape)
                    return i;
            return -1;
        }
    }
}
