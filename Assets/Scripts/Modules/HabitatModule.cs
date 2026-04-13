using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class HabitatModule : MonoBehaviour, IModule
    {
        [Header("Habitat Visuals")]
        public GameObject environmentProps; // Rocks, corals, sea-weed
        public ParticleSystem waterParticles;
        public Light directionalLight;

        private Color originalLightColor;

        private void Start()
        {
            if (directionalLight != null) originalLightColor = directionalLight.color;
            if (SystemStateManager.Instance != null)
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            
            OnModuleDeactivated(); // default state
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

        public ModuleType GetModuleType() => ModuleType.Habitat;

        public void OnModuleActivated()
        {
            Debug.Log("[Habitat Module] Activated: Spawning environment geometry.");
            if (environmentProps != null) environmentProps.SetActive(true);
            if (waterParticles != null) waterParticles.Play();
            
            // Example of contextual lighting adjustment for deep-water simulation
            if (directionalLight != null)
            {
                directionalLight.color = new Color(0.15f, 0.45f, 0.8f); // Deep blue tint
                directionalLight.intensity = 0.7f;
            }
        }

        public void OnModuleDeactivated()
        {
            if (environmentProps != null) environmentProps.SetActive(false);
            if (waterParticles != null) waterParticles.Stop();
            
            if (directionalLight != null)
            {
                directionalLight.color = originalLightColor;
                directionalLight.intensity = 1.0f;
            }
        }
    }
}
