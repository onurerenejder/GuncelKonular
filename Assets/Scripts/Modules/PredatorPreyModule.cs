using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class PredatorPreyModule : MonoBehaviour, IModule
    {
        [Header("Machine Learning / AI Constraints")]
        public GameObject apexPredatorPrefab;
        public float chaseSpeed = 3.8f;
        public float collisionAvoidanceWeight = 2.0f;
        
        [Header("Biological Prey Defenses")]
        public Renderer preySkinRenderer;
        public Color camouflageColor = new Color(0.6f, 0.5f, 0.4f); // Mimes sand/ocean floor tones
        public GameObject inkCloudParticle; 
        
        private GameObject activePredator;
        private Color originalSkinColor;
        private bool isDefenseCurrentlyActive = false;

        private void Start()
        {
            if (preySkinRenderer != null) originalSkinColor = preySkinRenderer.material.color;
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

        public ModuleType GetModuleType() => ModuleType.PredatorPrey;

        public void OnModuleActivated()
        {
            if (apexPredatorPrefab != null && activePredator == null)
            {
                Vector3 ambushCoordinate = transform.position + (transform.right * 4.5f) + (transform.up * 1f);
                activePredator = Instantiate(apexPredatorPrefab, ambushCoordinate, Quaternion.LookRotation(transform.position - ambushCoordinate));
            }
        }

        public void OnModuleDeactivated()
        {
            if (activePredator != null) Destroy(activePredator);
            if (preySkinRenderer != null) preySkinRenderer.material.color = originalSkinColor;
            isDefenseCurrentlyActive = false;
        }

        private void Update()
        {
            if (activePredator == null) return;

            float distanceToPrey = Vector3.Distance(activePredator.transform.position, transform.position);

            // Execute AI Pathfinding Vectors
            Vector3 desiredTrajectory = (transform.position - activePredator.transform.position).normalized;
            InjectObstacleAvoidanceSteering(ref desiredTrajectory);

            activePredator.transform.position += desiredTrajectory * chaseSpeed * Time.deltaTime;
            activePredator.transform.rotation = Quaternion.Slerp(activePredator.transform.rotation, Quaternion.LookRotation(desiredTrajectory), Time.deltaTime * 6f);

            // Nervous System threshold triggers
            if (distanceToPrey < 3.0f && !isDefenseCurrentlyActive)
            {
                TriggerBiologicalDefenseSystem();
            }

            if (distanceToPrey <= 1.0f)
            {
                // Prey invokes Panic Evasion maneuver computing a rapid cross-product escape route
                Vector3 evasionVector = Vector3.Cross(transform.up, desiredTrajectory);
                transform.position += evasionVector * 6f * Time.deltaTime;
            }
        }

        /// <summary>
        /// Raycast-based environmental awareness AI. Reping obstacle normals to push Predator trajectory away from physical walls.
        /// </summary>
        private void InjectObstacleAvoidanceSteering(ref Vector3 currentPath)
        {
            if (Physics.Raycast(activePredator.transform.position, activePredator.transform.forward, out RaycastHit hitInfo, 2.5f))
            {
                // Push the heading away utilizing the geometry's inverse normal vector
                Vector3 repulsiveNormal = hitInfo.normal;
                currentPath = Vector3.Lerp(currentPath, repulsiveNormal, collisionAvoidanceWeight * Time.deltaTime).normalized;
                Debug.DrawRay(activePredator.transform.position, repulsiveNormal * 2, Color.red);
            }
        }

        private void TriggerBiologicalDefenseSystem()
        {
            isDefenseCurrentlyActive = true;
            Debug.Log("[AI Engine] Predator in critical proximity range! Executing Chromatic Camouflage & Particulate Evasion.");
            
            if (preySkinRenderer != null)
                preySkinRenderer.material.color = camouflageColor;

            if (inkCloudParticle != null)
            {
                // Spreads optical illusion / ink to confuse AI logic
                GameObject cloud = Instantiate(inkCloudParticle, transform.position, Quaternion.identity);
                Destroy(cloud, 3.5f);
            }
        }
    }
}
