using Systems.Game;
using TMPro;
using UnityEngine;

namespace UI
{
    public class StatsDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text livesText;

        private void Update()
        {
            goldText.text = $"Gold: {playerStats.Gold}";
            livesText.text = $"Lives: {playerStats.Lives}";
        }
    }
}