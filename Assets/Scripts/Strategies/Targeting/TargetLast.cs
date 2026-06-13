using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    /// <summary>
    /// Targets enemies in LIFO order — the most recent to enter range is shot first.
    /// </summary>
    public class TargetLast : ITargetingStrategy
    {
        public ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition)
        {
            var stack = new Stack<ITargetable>();

            foreach (var candidate in candidates)
            {
                if (candidate.IsAlive)
                    stack.Push(candidate);
            }

            return stack.Count > 0 ? stack.Pop() : null;
        }
    }
}
