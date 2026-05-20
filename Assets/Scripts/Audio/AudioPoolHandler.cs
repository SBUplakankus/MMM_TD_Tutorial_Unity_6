using Data;
using UnityEngine;

namespace Audio
{
    public class AudioPoolHandler : MonoBehaviour
    {
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            // TODO: Ensure AudioSource exists, add if missing
        }

        public void Play(AudioData data, Vector3 position)
        {
            // TODO: Set transform.position to position
            // TODO: Configure _audioSource from data:
            //   - Clip: random from data.clips array
            //   - OutputAudioMixerGroup: data.outputGroup
            //   - Volume: data.volume
            //   - Pitch: Random.Range(data.pitchRange.x, data.pitchRange.y)
            //   - SpatialBlend: data.is3D ? data.spatialBlend : 0f
            //   - Priority: data.priority
            // TODO: Call _audioSource.Play()
            // TODO: Schedule auto-return to pool after clip length + small buffer
            // TODO: Use StartCoroutine or PrimeTween delay to call ReturnToPool()
        }

        public void ReturnToPool()
        {
            // TODO: Stop _audioSource if playing
            // TODO: Return this gameObject via ObjectPoolManager.Return()
        }

        public void Reset()
        {
            // TODO: Stop AudioSource, clear clip reference, reset pitch/volume
        }
    }
}