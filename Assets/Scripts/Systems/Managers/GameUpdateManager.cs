using System;
using System.Collections.Generic;
using Enums;
using Interfaces;
using UnityEngine;

namespace Systems.Managers
{
    public class GameUpdateManager : MonoBehaviour
    {
        private readonly List<IUpdateable> _highPriorityUpdates = new();
        private readonly List<IUpdateable> _mediumPriorityUpdates = new();
        private readonly List<IUpdateable> _lowPriorityUpdates = new();
        private readonly List<IUpdateable> _fixedPriorityUpdates = new();
        private readonly List<IUpdateable> _latePriorityUpdates = new();

        private const float MediumPriorityInterval = 0.2f;
        private const float LowPriorityInterval = 0.4f;
        private float _mediumPriorityTimer;
        private float _lowPriorityTimer;

        private static void Iterate(List<IUpdateable> updates, float delta)
        {
            for (int i = 0; i < updates.Count; i++)
                updates[i].Tick(delta);
        }

        private void HighPriorityUpdate()
        {
            Iterate(_highPriorityUpdates, Time.deltaTime);
        }

        private void MediumPriorityUpdate()
        {
            _mediumPriorityTimer += Time.deltaTime;
            if (_mediumPriorityTimer < MediumPriorityInterval) return;
            Iterate(_mediumPriorityUpdates, _mediumPriorityTimer);
            _mediumPriorityTimer = 0f;
        }

        private void LowPriorityUpdate()
        {
            _lowPriorityTimer += Time.deltaTime;
            if (_lowPriorityTimer < LowPriorityInterval) return;
            Iterate(_lowPriorityUpdates, _lowPriorityTimer);
            _lowPriorityTimer = 0f;
        }

        /// <summary>
        /// Registers an IUpdateable to receive updates at the given priority.
        /// </summary>
        /// <param name="updateable">The object to register.</param>
        /// <param name="priority">The update priority bucket to register into.</param>
        public void Register(IUpdateable updateable, UpdatePriority priority)
        {
            switch (priority)
            {
                case UpdatePriority.High:
                    _highPriorityUpdates.Add(updateable);
                    break;
                case UpdatePriority.Medium:
                    _mediumPriorityUpdates.Add(updateable);
                    break;
                case UpdatePriority.Low:
                    _lowPriorityUpdates.Add(updateable);
                    break;
                case UpdatePriority.Fixed:
                    _fixedPriorityUpdates.Add(updateable);
                    break;
                case UpdatePriority.Late:
                    _latePriorityUpdates.Add(updateable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
            }
        }

        /// <summary>
        /// Unregisters an IUpdateable from whichever priority bucket it belongs to.
        /// </summary>
        /// <param name="updateable">The object to unregister.</param>
        public void Unregister(IUpdateable updateable)
        {
            if (_highPriorityUpdates.Remove(updateable)) return;
            if (_mediumPriorityUpdates.Remove(updateable)) return;
            if (_lowPriorityUpdates.Remove(updateable)) return;
            if (_fixedPriorityUpdates.Remove(updateable)) return;
            _latePriorityUpdates.Remove(updateable);
        }

        private void Update()
        {
            HighPriorityUpdate();
            MediumPriorityUpdate();
            LowPriorityUpdate();
        }

        private void FixedUpdate() => Iterate(_fixedPriorityUpdates, Time.fixedDeltaTime);
        private void LateUpdate() => Iterate(_latePriorityUpdates, Time.deltaTime);
    }
}