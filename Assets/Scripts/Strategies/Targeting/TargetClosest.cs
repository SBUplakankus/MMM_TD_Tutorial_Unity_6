using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    /// <summary>
    /// Targets the closest alive enemy by comparing squared distances from the tower.
    /// </summary>
    public class TargetClosest : ITargetingStrategy
    {
        public ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition)
        {
            if (candidates.Count == 0) return null;

            var closest = candidates[0];
            var closestDist = float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!candidate.IsAlive) continue;
                var dist = (candidate.Position - towerPosition).sqrMagnitude;
                if (dist >= closestDist) continue;
                closestDist = dist;
                closest = candidate;
            }

            return closest;
        }
    }
}
