using UnityEngine;
using UnityEngine.Audio;

namespace Data
{
    [CreateAssetMenu(fileName = "AudioData", menuName = "Scriptable Objects/Data/Audio")]
    public class AudioData : ScriptableObject
    {
        #region Fields

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] clips;

        [Header("Mixing")]
        [SerializeField] private AudioMixerGroup outputGroup;
        [SerializeField] private float volume = 1f;

        [Header("Pitch Randomization")]
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

        [Header("Spatial Settings")]
        [SerializeField] private bool is3D;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;

        [Header("Priority")]
        [SerializeField] private int priority = 128;

        #endregion

        #region Properties

        public AudioClip[] Clips => clips;
        public AudioMixerGroup OutputGroup => outputGroup;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public bool Is3D => is3D;
        public float SpatialBlend => spatialBlend;
        public int Priority => priority;

        #endregion

        #region Helpers

        public AudioClip GetRandomClip()
        {
            // TODO: Return random clip from clips array
            // TODO: If clips is empty or null, return null
            return null;
        }

        public float GetRandomPitch()
        {
            // TODO: Return Random.Range(pitchRange.x, pitchRange.y)
            return 1f;
        }

        #endregion
    }
}