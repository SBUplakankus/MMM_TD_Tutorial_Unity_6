using System.Collections.Generic;
using Core;
using Interfaces;
using UnityEngine;

namespace Systems.Managers
{
    public enum UpdatePriority
    {
        High,
        Medium,
        Low
    }

    public class UpdateManager : MonoBehaviour
    {
        #region Fields

        private Dictionary<UpdatePriority, List<IUpdatable>> _updatables;
        private Dictionary<UpdatePriority, float> _tickIntervals;
        private Dictionary<UpdatePriority, float> _timers;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // TODO: Initialize dictionaries
            // TODO: Register self in Services: Services.Register<UpdateManager>(this)
            // TODO: Set tick intervals (High: 0f, Medium: 0.15f, Low: 0.4f)
        }

        private void Update()
        {
            // TODO: For each priority, tick all IUpdatable when timer expires
        }

        #endregion

        #region Public API

        public void Register(IUpdatable updatable, UpdatePriority priority)
        {
            // TODO: Add updatable to _updatables[priority]
        }

        public void Unregister(IUpdatable updatable, UpdatePriority priority)
        {
            // TODO: Remove updatable from _updatables[priority]
        }

        #endregion
    }
}