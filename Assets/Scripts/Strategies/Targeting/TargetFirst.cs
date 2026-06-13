using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    /// <summary>
    /// Targets enemies in FIFO order — the first to enter range is the first to get shot.
    /// </summary>
    public class TargetFirst : ITargetingStrategy
    {
        public ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition)
        {
            var queue = new Queue<ITargetable>();

            foreach (var target in candidates)
                queue.Enqueue(target);

            return queue.Count > 0 ? queue.Dequeue() : null;
        }
    }
}
