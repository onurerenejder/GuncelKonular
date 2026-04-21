using UnityEngine;
using System.Collections;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class HabitatModule : MonoBehaviour, IModule
    {
        [Header("Procedural Biome Engine")]
        public EnvironmentType currentHabitat;
        public int objectDensity = 25;
        public float algorithmicScatterRadius = 4.0f;
        
        [Header("AR World Tracking Integrations")]
        [Tooltip("Layer mask assigned to AR Foundation generated physical planes (floor/tables).")]
        public LayerMask arFloorMeshLayer; 
        public GameObject[] reefPropPrefabs;
        public GameObject[] deepSeaRockPrefabs;

        [Header("Post-Processing Environment")]
        public Light mainDirectionalLight;
        public ParticleSystem ambientFloatingParticles;

        private GameObject generatedEnvironmentRoot;

        private void Start()
        {
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

        public ModuleType GetModuleType() => ModuleType.Habitat;

        public void OnModuleActivated()
        {
            Debug.Log("[Habitat Module] Engaged: Raycasting real world to procedurally generate Biome assets...");
            CleanEnvironment();
            generatedEnvironmentRoot = new GameObject("Procedural_Habitat_Cluster");
            generatedEnvironmentRoot.transform.SetParent(this.transform);

            Color targetLightColor = Color.white;
            float targetIntensity = 1f;

            switch(currentHabitat)
            {
                case EnvironmentType.DeepOcean:
                    targetLightColor = new Color(0.02f, 0.08f, 0.25f);
                    targetIntensity = 0.3f;
                    ScatterObjectsProcedurally(deepSeaRockPrefabs);
                    break;
                case EnvironmentType.CoralReef:
                    targetLightColor = new Color(0.15f, 0.85f, 0.95f);
                    targetIntensity = 1.3f;
                    ScatterObjectsProcedurally(reefPropPrefabs);
                    break;
            }

            if (ambientFloatingParticles != null) ambientFloatingParticles.Play();
            StopAllCoroutines();
            StartCoroutine(EngineTransitionLighting(targetLightColor, targetIntensity));
        }

        public void OnModuleDeactivated()
        {
            CleanEnvironment();
            if (ambientFloatingParticles != null) ambientFloatingParticles.Stop();
        }

        private void ScatterObjectsProcedurally(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return;

            // Algorithm generates physical sea-floor assets mapped dynamically to the real physical room using Camera Raycasts
            for (int i = 0; i < objectDensity; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * algorithmicScatterRadius;
                // Calculate from sky down
                Vector3 originPoint = transform.position + new Vector3(randomCircle.x, 3f, randomCircle.y);
                
                // Physics Query against physical geometry (the AR tracked room mesh)
                if (Physics.Raycast(originPoint, Vector3.down, out RaycastHit hit, 10f, arFloorMeshLayer))
                {
                    GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Length)];
                    
                    // Align the rock or coral exactly to the slope normal of the user's couch/floor
                    Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    GameObject prop = Instantiate(prefabToSpawn, hit.point, slopeRotation * Quaternion.Euler(0, Random.Range(0, 360), 0));
                    
                    prop.transform.SetParent(generatedEnvironmentRoot.transform);
                    
                    // Introduce organic scaling variables
                    prop.transform.localScale *= Random.Range(0.5f, 1.6f);
                }
            }
        }

        private void CleanEnvironment()
        {
            if (generatedEnvironmentRoot != null) Destroy(generatedEnvironmentRoot);
        }

        private IEnumerator EngineTransitionLighting(Color targetColor, float targetIntensity)
        {
            if (mainDirectionalLight == null) yield break;
            Color startColor = mainDirectionalLight.color;
            float startIntensity = mainDirectionalLight.intensity;
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime * 1.8f;
                mainDirectionalLight.color = Color.Lerp(startColor, targetColor, t);
                mainDirectionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                yield return null;
            }
        }
    }
}
