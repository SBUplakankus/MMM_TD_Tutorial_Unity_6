using System.Collections.Generic;
using UnityEngine;

namespace Interfaces
{
    public interface ITargetingStrategy
    {
        // TODO: GetTarget — select best target from valid ITargetables based on strategy logic
        ITargetable GetTarget(IEnumerable<ITargetable> targets, Vector3 towerPosition);
    }
}