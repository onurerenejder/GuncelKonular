using UnityEngine;
using ARFishApp.Data;

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
            
            // Example architectural workflow:
            // Extracting values from FishData and passing it to the UI or audio systems
            // e.g., FindObjectOfType<UIController>().SetDescriptionText(fishDataConfig.GeneralDescription);
        }
    }
}
