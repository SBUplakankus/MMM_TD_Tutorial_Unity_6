using Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utilities
{
    public class ClickDamageUtil : MonoBehaviour
    {
        [SerializeField] private float damagePerClick = 25f;
        private Camera _camera;

#if UNITY_EDITOR
        private void Awake() => _camera = Camera.main;
        private void Update()
        {
            if (Pointer.current == null) return;
            if (!Pointer.current.press.wasPressedThisFrame) return;
            if (!_camera) return;

            var screenPosition = Pointer.current.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screenPosition);
            
            if (!Physics.Raycast(ray, out var hit)) return;
            if (!hit.collider.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!damageable.IsAlive) return;
            
            damageable.TakeDamage(damagePerClick);
            Debug.Log($"Dealt {damagePerClick} damage to {hit.collider.name}");
        }
#endif
    }
}