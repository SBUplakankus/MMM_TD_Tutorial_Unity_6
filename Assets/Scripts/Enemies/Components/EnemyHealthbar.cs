using UnityEngine;
using UnityEngine.UI;

namespace Enemies.Components
{
    public class EnemyHealthBar : MonoBehaviour
    {
        // TODO: Episode 02 — Health bar using Slider
        
        [SerializeField] private Slider healthBar;

        public void UpdateValue(float healthPercent) => healthBar.value = healthPercent;
        public void Hide() => gameObject.SetActive(false);
        public void Show() => gameObject.SetActive(true);
        
        
        // Reference a Slider, drive fill from currentHealth/maxHealth
        // Position above enemy with offset
        // TODO: Episode 12 — Add shield slider (second fill, blue)
    }
}