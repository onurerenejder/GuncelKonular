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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAutomatically()
        {
            // Sahne baslamadan once otomatik olarak bir obje olusturup bu scripti ekler.
            var go = new GameObject("AudioInformationManager");
            go.AddComponent<AudioInformationManager>();
            DontDestroyOnLoad(go); // Sahneler arasi geciste silinmemesi icin
            Debug.Log("[AudioInformationManager] Automatically created and added to the scene at runtime.");
        }

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
            Debug.Log($"[AudioInformationManager] PlayNarrationForState called for Module: {newType}");
            AudioClip clipToPlay = null;
            
            // Try to get dynamic clip from FishData or Resources
            var activeFish = FishSelectionManager.Instance?.CurrentFish;
            if (activeFish != null)
            {
                Debug.Log($"[AudioInformationManager] Active fish found: {activeFish.id}");
                var data = activeFish.fishData;
                if (data != null)
                {
                    Debug.Log($"[AudioInformationManager] FishData found for {activeFish.id}");
                    switch (newType)
                    {
                        case ModuleType.Anatomy: clipToPlay = data.AnatomyAudioClip; break;
                        case ModuleType.Habitat: clipToPlay = data.HabitatAudioClip; break;
                        case ModuleType.Feeding: clipToPlay = data.FeedingAudioClip; break;
                        case ModuleType.InterspeciesRelations: clipToPlay = data.InterspeciesAudioClip; break;
                        case ModuleType.PredatorPrey: clipToPlay = data.PredatorPreyAudioClip; break;
                        case ModuleType.Quiz: clipToPlay = data.QuizAudioClip; break;
                        case ModuleType.Portal: clipToPlay = data.PortalAudioClip; break;
                    }
                    Debug.Log($"[AudioInformationManager] Clip from FishData after switch: {(clipToPlay != null ? clipToPlay.name : "null")}");
                }
                else
                {
                    Debug.LogWarning($"[AudioInformationManager] activeFish.fishData is null!");
                }

                // Fallback to Resources if not manually assigned in ScriptableObject
                if (clipToPlay == null)
                {
                    string resourcePath = $"Audio/{activeFish.id}/{newType.ToString()}";
                    Debug.Log($"[AudioInformationManager] Attempting to load from Resources at path: {resourcePath}");
                    clipToPlay = Resources.Load<AudioClip>(resourcePath);
                    if (clipToPlay != null)
                    {
                        Debug.Log($"[AudioInformationManager] Loaded audio dynamically from Resources: {resourcePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioInformationManager] Failed to load audio from Resources: {resourcePath}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[AudioInformationManager] FishSelectionManager.Instance?.CurrentFish is null!");
            }

            if (clipToPlay != null)
            {
                Debug.Log($"[AudioInformationManager] Starting CrossfadeAudioSequence with clip: {clipToPlay.name}");
                StopAllCoroutines();
                StartCoroutine(CrossfadeAudioSequence(clipToPlay));
            }
            else
            {
                Debug.LogWarning($"[AudioInformationManager] No audio clip found for module {newType} on fish {activeFish?.id}");
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
