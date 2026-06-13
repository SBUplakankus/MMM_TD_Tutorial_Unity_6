using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    // TODO: Episode 13 — ITargetingStrategy: GetTarget(IEnumerable<ITargetable>, Vector3 towerPosition)

    public interface ITargetingStrategy
    {
        ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition);
    }
}