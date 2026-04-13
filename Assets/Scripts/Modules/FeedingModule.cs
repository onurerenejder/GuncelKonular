using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class FeedingModule : MonoBehaviour, IModule
    {
        [Header("Feeding Components")]
        public Animator fishAnimator;
        public GameObject foodParticlesPrefab;
        public Transform mouthPosition;

        private GameObject currentFoodInstance;

        private void Start()
        {
            if (SystemStateManager.Instance != null)
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
                
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
                SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ModuleType newType)
        {
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Feeding;

        public void OnModuleActivated()
        {
            Debug.Log("[Feeding Module] Activated: Initiating feeding behaviors and animations.");
            
            if (fishAnimator != null)
            {
                fishAnimator.SetBool("IsFeeding", true);
            }

            if (foodParticlesPrefab != null && mouthPosition != null && currentFoodInstance == null)
            {
                currentFoodInstance = Instantiate(foodParticlesPrefab, mouthPosition.position + (transform.forward * 0.3f), Quaternion.identity);
                currentFoodInstance.transform.SetParent(this.transform);
            }
        }

        public void OnModuleDeactivated()
        {
            if (fishAnimator != null)
            {
                fishAnimator.SetBool("IsFeeding", false);
            }

            if (currentFoodInstance != null)
            {
                Destroy(currentFoodInstance);
                currentFoodInstance = null;
            }
        }
    }
}
