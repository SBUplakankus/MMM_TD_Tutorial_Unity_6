using Enums;
using Interfaces;
using Systems.Managers;
using UnityEngine;

namespace Utilities
{
    public class LookAtCamera : MonoBehaviour, IUpdateable
    {
        private Transform _cameraTransform;

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }
        
        public void Tick(float deltaTime)
        { 
            if (!_cameraTransform) return;

            transform.LookAt(transform.position + _cameraTransform.rotation * Vector3.forward, _cameraTransform.rotation * Vector3.up);
        }

        private void OnEnable() => GameUpdateManager.Instance.Register(this, UpdatePriority.Late);
        private void OnDisable() => GameUpdateManager.Instance.Unregister(this);
    }
}
