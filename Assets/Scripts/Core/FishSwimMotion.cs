using UnityEngine;

namespace ARFishApp.Core
{
    public class FishSwimMotion : MonoBehaviour
    {
        public float verticalAmplitude = 0.16f;
        public float verticalSpeed = 1.45f;
        public float yawAmplitude = 18f;
        public float yawSpeed = 1.1f;
        public float rollAmplitude = 9f;
        public float rollSpeed = 1.7f;
        public float forwardAmplitude = 0.18f;
        public float forwardSpeed = 0.95f;
        public float sideAmplitude = 0.12f;
        public float sideSpeed = 0.85f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private float phaseOffset;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            float time = Time.time + phaseOffset;
            float vertical = Mathf.Sin(time * verticalSpeed) * verticalAmplitude;
            float forward = Mathf.Sin(time * forwardSpeed) * forwardAmplitude;
            float side = Mathf.Sin(time * sideSpeed) * sideAmplitude;
            float yaw = Mathf.Sin(time * yawSpeed) * yawAmplitude;
            float roll = Mathf.Sin(time * rollSpeed) * rollAmplitude;

            transform.localPosition = baseLocalPosition + new Vector3(side, vertical, forward);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, yaw, roll);
        }
    }
}
