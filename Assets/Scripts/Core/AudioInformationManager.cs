using UnityEngine;

namespace ARFishApp.Core
{
    /// <summary>
    /// Audio Information Manager orchestrates Voice-Overs.
    /// Listens directly to SystemStateManager to trigger educational narratives.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioInformationManager : MonoBehaviour
    {
        private AudioSource audioSource;
        
        [Header("Module Narrations")]
        public AudioClip anatomyClip;
        public AudioClip habitatClip;
        public AudioClip feedingClip;
        public AudioClip ecosystemClip;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += PlayNarrationForState;
            }
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= PlayNarrationForState;
            }
        }

        private void PlayNarrationForState(ModuleType newType)
        {
            audioSource.Stop();
            AudioClip clipToPlay = null;

            switch (newType)
            {
                case ModuleType.Anatomy: clipToPlay = anatomyClip; break;
                case ModuleType.Habitat: clipToPlay = habitatClip; break;
                case ModuleType.Feeding: clipToPlay = feedingClip; break;
                case ModuleType.Ecosystem: clipToPlay = ecosystemClip; break;
            }

            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.Play();
                Debug.Log($"[Audio Manager] Playing narrative track for {newType}");
            }
        }
    }
}
