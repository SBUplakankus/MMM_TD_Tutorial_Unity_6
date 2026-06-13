using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    /// <summary>
    /// Targets the weakest enemy by sorting candidates by health and picking the first alive.
    /// </summary>
    public class TargetWeakest : ITargetingStrategy
    {
        public ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition)
        {
            var sorted = new List<ITargetable>(candidates);
            sorted.Sort((a, b) => a.CurrentHealth.CompareTo(b.CurrentHealth));
            return sorted.FirstOrDefault(t => t.IsAlive);
        }
    }
}
