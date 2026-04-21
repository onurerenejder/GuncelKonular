using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class PortalModule : MonoBehaviour, IModule
    {
        [Header("Portal Mechanics")]
        public Transform portalDoorway;
        public GameObject underwaterEnvironment;
        public Camera arCamera; // References the mobile device's camera tracking
        
        private bool isInsidePortal = false;

        private void Start()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ModuleType newType)
        {
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Portal;

        public void OnModuleActivated()
        {
            Debug.Log("[Portal Module] Portal Anchored. Walk physically forward through the phone to cross dimensions.");
            if (portalDoorway != null) portalDoorway.gameObject.SetActive(true);
            if (underwaterEnvironment != null) underwaterEnvironment.SetActive(true);
        }

        public void OnModuleDeactivated()
        {
            if (portalDoorway != null) portalDoorway.gameObject.SetActive(false);
            if (underwaterEnvironment != null) underwaterEnvironment.SetActive(false);
            isInsidePortal = false;
        }

        private void Update()
        {
            if (SystemStateManager.Instance.CurrentModule == GetModuleType() && arCamera != null && portalDoorway != null)
            {
                // Vector math to detect if the user's physical camera crossed the portal's threshold
                Vector3 cameraOffset = arCamera.transform.position - portalDoorway.position;
                bool isNowInside = Vector3.Dot(portalDoorway.forward, cameraOffset) > 0;

                if (isNowInside != isInsidePortal)
                {
                    isInsidePortal = isNowInside;
                    if (isInsidePortal)
                        Debug.Log("[Portal Module] 🌊 USER ENTERED UNDERWATER DIMENSION! Audio muffles, lighting shifts.");
                    else
                        Debug.Log("[Portal Module] 🏙️ USER RETURNED TO REAL WORLD. Sea renders only through the doorway.");
                }
            }
        }
    }
}
