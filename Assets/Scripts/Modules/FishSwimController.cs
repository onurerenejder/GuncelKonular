using UnityEngine;

namespace ARFishApp.Modules
{
    /// <summary>
    /// Prefab balığa eklenir veya HabitatModule spawn sırasında otomatik ekler.
    /// Animator varsa → parametre üzerinden yüzme hızını iletir.
    /// Animator yoksa → tailBone ile kod tabanlı kuyruk sallama yapar.
    /// AudioSource → 3D uzamsal balık sesi.
    /// </summary>
    public class FishSwimController : MonoBehaviour
    {
        [Header("Animator (opsiyonel)")]
        public string swimSpeedParam = "SwimSpeed";

        [Header("Kod Tabanlı Kuyruk (Animator yoksa)")]
        public Transform tailBone;
        [Range(1f, 5f)] public float tailWagFrequency = 2.4f;
        [Range(5f, 45f)] public float tailWagAmplitude = 22f;

        [Header("3D Ses")]
        public AudioClip swimAudioClip;
        [Range(0f, 1f)] public float swimVolumeMax = 0.12f;
        public float audioMinDistance = 0.3f;
        public float audioMaxDistance = 5f;

        private Animator _animator;
        private AudioSource _audioSource;
        private float _wagPhaseOffset;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
                _animator.applyRootMotion = false;
            _wagPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            InitAudio();
        }

        private void InitAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.spatialBlend    = 1f;
            _audioSource.rolloffMode     = AudioRolloffMode.Linear;
            _audioSource.minDistance     = audioMinDistance;
            _audioSource.maxDistance     = audioMaxDistance;
            _audioSource.loop            = true;
            _audioSource.playOnAwake     = false;
            _audioSource.volume          = 0f;
            _audioSource.pitch           = Random.Range(0.88f, 1.12f);

            if (swimAudioClip != null)
            {
                _audioSource.clip = swimAudioClip;
                _audioSource.Play();
            }
        }

        private void Update()
        {
            if (_animator == null && tailBone != null)
            {
                float wag = Mathf.Sin(Time.time * tailWagFrequency + _wagPhaseOffset)
                            * tailWagAmplitude;
                tailBone.localRotation = Quaternion.Euler(0f, wag, 0f);
            }
        }

        /// <summary>HabitatModule'dan her frame çağrılır.</summary>
        public void SetSpeed(float speed)
        {
            if (_animator != null)
                _animator.SetFloat(swimSpeedParam, speed);

            if (_audioSource != null && swimAudioClip != null)
                _audioSource.volume = Mathf.Lerp(0f, swimVolumeMax, speed / 0.4f);
        }

        public void StopAudio()
        {
            if (_audioSource != null) _audioSource.Stop();
        }
    }
}
