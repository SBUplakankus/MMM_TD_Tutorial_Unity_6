using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Strategies.Targeting
{
    /// <summary>
    /// Targets the strongest enemy using a LinkedList that keeps candidates sorted by health descending.
    /// </summary>
    public class TargetStrongest : ITargetingStrategy
    {
        public ITargetable GetTarget(IReadOnlyList<ITargetable> candidates, Vector3 towerPosition)
        {
            LinkedList<ITargetable> order = new();

            foreach (var target in candidates)
            {
                if (target.IsAlive)
                    InsertSorted(order, target);
            }

            return order.Count == 0 ? null : order.First.Value;
        }

        private static void InsertSorted(LinkedList<ITargetable> list, ITargetable target)
        {
            var node = list.First;

            while (node != null)
            {
                if (node.Value.CurrentHealth <= target.CurrentHealth)
                    break;

                node = node.Next;
            }

            if (node == null)
                list.AddLast(target);
            else
                list.AddBefore(node, target);
        }
    }
}
