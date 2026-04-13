using UnityEngine;
using ARFishApp.Core;
using System.Collections.Generic;

namespace ARFishApp.Modules
{
    public class EcosystemModule : MonoBehaviour, IModule
    {
        [Header("Ecosystem Visuals")]
        public GameObject predatorModel;
        public GameObject schoolingFishPrefab;
        public Transform schoolingCenter;
        public int schoolSize = 10;
        
        private List<GameObject> activeSchool = new List<GameObject>();

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

        public ModuleType GetModuleType() => ModuleType.Ecosystem;

        public void OnModuleActivated()
        {
            Debug.Log("[Ecosystem Module] Activated: Simulating predator-prey dynamics and schooling.");
            
            if (predatorModel != null)
                predatorModel.SetActive(true);

            if (schoolingFishPrefab != null && schoolingCenter != null && activeSchool.Count == 0)
            {
                for (int i = 0; i < schoolSize; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), Random.Range(-1f, 1f));
                    GameObject fish = Instantiate(schoolingFishPrefab, schoolingCenter.position + randomOffset, Quaternion.identity);
                    fish.transform.SetParent(this.transform);
                    activeSchool.Add(fish);
                }
            }
        }

        public void OnModuleDeactivated()
        {
            if (predatorModel != null)
                predatorModel.SetActive(false);

            foreach (var fish in activeSchool)
            {
                if (fish != null) Destroy(fish);
            }
            activeSchool.Clear();
        }
    }
}
