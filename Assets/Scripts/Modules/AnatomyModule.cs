using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    /// <summary>
    /// Handles the "Anatomy" rendering logic (e.g. bones, internal organs).
    /// </summary>
    public class AnatomyModule : MonoBehaviour, IModule
    {
        [Header("Anatomy Visuals")]
        public GameObject skinModel;
        public GameObject skeletonModel;
        public GameObject internalOrgansModel;

        private void Start()
        {
            // Register to State Manager events
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(ModuleType newType)
        {
            if (newType == GetModuleType())
            {
                OnModuleActivated();
            }
            else
            {
                OnModuleDeactivated();
            }
        }

        public ModuleType GetModuleType() => ModuleType.Anatomy;

        public void OnModuleActivated()
        {
            Debug.Log("[Anatomy Module] Activated: Displaying Anatomical Layers.");
            // Example Logic: Hide skin, show skeleton and organs
            if (skinModel != null) skinModel.SetActive(false);
            if (skeletonModel != null) skeletonModel.SetActive(true);
            if (internalOrgansModel != null) internalOrgansModel.SetActive(true);
        }

        public void OnModuleDeactivated()
        {
            Debug.Log("[Anatomy Module] Deactivated.");
            // Reset logic
            if (skinModel != null) skinModel.SetActive(true);
            if (skeletonModel != null) skeletonModel.SetActive(false);
            if (internalOrgansModel != null) internalOrgansModel.SetActive(false);
        }
    }
}
