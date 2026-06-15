using UnityEngine;
using ARFishApp.Data;
using ARFishApp.Modules;

namespace ARFishApp.Core
{
    /// <summary>
    /// Binds the FishData to the actual 3D Entity.
    /// Distributes parameters from the Data layer to the Visual Modules.
    /// </summary>
    public class FishEntityController : MonoBehaviour
    {
        [Header("Single Source of Truth Config")]
        public FishData fishDataConfig;

        public void SetFishData(FishData newFishData)
        {
            fishDataConfig = newFishData;

            if (fishDataConfig == null)
            {
                Debug.LogWarning("[FishEntityController] Active fish data cleared.");
                return;
            }

            InitializeArchitecture();
        }

        private void Start()
        {
            if (fishDataConfig == null)
            {
                Debug.LogWarning("[FishEntityController] Initialization Failed: No FishData assigned to entity.");
                return;
            }

            InitializeArchitecture();
        }

        private void InitializeArchitecture()
        {
            Debug.Log($"[FishEntityController] Bootstrapping Educational Data for: {fishDataConfig.FishName} ({fishDataConfig.ScientificName})");

            BindFeedingModules();
            BindHabitatModules();
        }

        private void BindFeedingModules()
        {
            FeedingModule[] feedingModules = GetComponentsInChildren<FeedingModule>(true);
            for (int i = 0; i < feedingModules.Length; i++)
            {
                if (feedingModules[i] != null)
                {
                    feedingModules[i].fishData = fishDataConfig;
                }
            }
        }

        private void BindHabitatModules()
        {
            HabitatModule[] habitatModules = GetComponentsInChildren<HabitatModule>(true);
            for (int i = 0; i < habitatModules.Length; i++)
            {
                if (habitatModules[i] != null)
                {
                    habitatModules[i].currentHabitat = ResolveHabitatType(fishDataConfig.HabitatType);
                }
            }
        }

        private EnvironmentType ResolveHabitatType(string habitatType)
        {
            if (string.IsNullOrWhiteSpace(habitatType))
            {
                return EnvironmentType.CoralReef;
            }

            string normalized = habitatType.ToLowerInvariant();
            if (normalized.Contains("deep") || normalized.Contains("ocean") || normalized.Contains("okyanus") || normalized.Contains("derin"))
            {
                return EnvironmentType.DeepOcean;
            }

            return EnvironmentType.CoralReef;
        }
    }
}
