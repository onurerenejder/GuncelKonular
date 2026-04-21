using UnityEngine;
using System.Collections;

namespace ARFishApp.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioInformationManager : MonoBehaviour
    {
        private AudioSource primarySource;
        private AudioSource secondarySource; 
        private bool usePrimary = true;
        
        [Header("Audio Tracks")]
        public AudioClip anatomyClip;
        public AudioClip habitatClip;
        public AudioClip feedingClip;
        public AudioClip interspeciesClip;
        public AudioClip predatorPreyClip;

        [Header("FFT Real-Time Spectrum Hardware Analysis")]
        [Tooltip("If true, mathematically analyzes soundwaves (Fast Fourier Transform) to drive UI or shaders globally over static AudioAmplitude.")]
        public bool enableAudioReactivity = true;
        
        /// <summary>
        /// Global variable representing Voice Loudness. 
        /// Hook this into your UI Elements' Scale, or Shader Emission intensity!
        /// </summary>
        public static float AudioAmplitude; 
        
        // Cache for FFT execution
        private float[] spectrumData = new float[256];

        private void Awake()
        {
            primarySource = gameObject.AddComponent<AudioSource>();
            secondarySource = gameObject.AddComponent<AudioSource>();
            primarySource.spatialBlend = 1f; 
            secondarySource.spatialBlend = 1f;
        }

        private void Start()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += PlayNarrationForState;
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged -= PlayNarrationForState;
        }

        private void PlayNarrationForState(ModuleType newType)
        {
            AudioClip clipToPlay = null;
            switch (newType)
            {
                case ModuleType.Anatomy: clipToPlay = anatomyClip; break;
                case ModuleType.Habitat: clipToPlay = habitatClip; break;
                case ModuleType.Feeding: clipToPlay = feedingClip; break;
                case ModuleType.InterspeciesRelations: clipToPlay = interspeciesClip; break;
                case ModuleType.PredatorPrey: clipToPlay = predatorPreyClip; break;
            }

            if (clipToPlay != null)
            {
                StopAllCoroutines();
                StartCoroutine(CrossfadeAudioSequence(clipToPlay));
            }
        }

        private IEnumerator CrossfadeAudioSequence(AudioClip nextClip)
        {
            AudioSource active = usePrimary ? primarySource : secondarySource;
            AudioSource fadingIn = usePrimary ? secondarySource : primarySource;

            fadingIn.clip = nextClip;
            fadingIn.volume = 0f;
            fadingIn.Play();

            float transitionTime = 0;
            while (transitionTime < 1f)
            {
                transitionTime += Time.deltaTime; 
                active.volume = Mathf.Lerp(1f, 0f, transitionTime);
                fadingIn.volume = Mathf.Lerp(0f, 1f, transitionTime);
                yield return null;
            }

            active.Stop();
            usePrimary = !usePrimary;
        }

        private void Update()
        {
            if (!enableAudioReactivity) return;

            // Deep DSP (Digital Signal Processing) Engine Injection 
            // Fast Fourier Transform (FFT) calculation to get raw frequency waveform bounds
            AudioSource active = usePrimary ? primarySource : secondarySource;
            if (active.isPlaying)
            {
                // Reading samples algorithmically using Blackman-Harris windowing
                active.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
                
                float frameSum = 0f;
                // Index 0 to 64 typically captures human vocal harmonic ranges reliably
                for (int i = 0; i < 64; i++) 
                {
                    frameSum += spectrumData[i];
                }
                
                // Low-pass interpolation to smooth electrical spikes
                AudioAmplitude = Mathf.Lerp(AudioAmplitude, frameSum * 50f, Time.deltaTime * 15f); 
            }
            else
            {
                AudioAmplitude = Mathf.Lerp(AudioAmplitude, 0f, Time.deltaTime * 10f);
            }
        }
    }
}
