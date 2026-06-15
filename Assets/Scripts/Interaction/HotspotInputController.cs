using UnityEngine;

namespace ARFishApp.Interaction
{
    public class HotspotInputController : MonoBehaviour
    {
        [Header("Raycast")]
        public Camera targetCamera;
        public LayerMask hotspotLayerMask = ~0;
        [Min(0.1f)] public float maxRaycastDistance = 30f;
        public bool enableMouseFallback = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindFirstObjectByType<HotspotInputController>() != null) return;

            GameObject controllerObject = new GameObject("HotspotInputController");
            controllerObject.AddComponent<HotspotInputController>();
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryTapAtScreenPosition(touch.position);
                }

                return;
            }

            if (enableMouseFallback && Input.GetMouseButtonDown(0))
            {
                TryTapAtScreenPosition(Input.mousePosition);
            }
        }

        private void TryTapAtScreenPosition(Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, hotspotLayerMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            HotspotNode hotspot = hit.collider.GetComponentInParent<HotspotNode>();
            if (hotspot != null)
            {
                hotspot.ApplyTap();
            }
        }
    }
}
