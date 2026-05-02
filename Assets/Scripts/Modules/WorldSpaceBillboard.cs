using UnityEngine;

namespace ARFishApp.Modules
{
    public class WorldSpaceBillboard : MonoBehaviour
    {
        [Tooltip("If empty, Camera.main is used.")]
        public Camera targetCamera;

        private void LateUpdate()
        {
            Camera activeCamera = targetCamera != null ? targetCamera : Camera.main;
            if (activeCamera == null) return;

            Vector3 direction = transform.position - activeCamera.transform.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            // Keep text facing the viewer while preserving upright orientation.
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
