using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class PredatorPreyModule : MonoBehaviour, IModule
    {
        [Header("Apex Vision & Neural AI")]
        public GameObject apexPredatorPrefab;
        public float chasePacingSpeed = 3.8f;
        public float aiCollisionAvoidanceWeight = 2.0f;
        
        [Tooltip("Field of View angle limits. The predator visually scans within this cone mathematically.")]
        [Range(10f, 360f)] public float neuralVisionAngleCone = 90f;
        
        [Header("Prey Bio-Chromatic Defense System")]
        public Renderer preySkinRenderer;
        public Color camouflageEnvironmentTone = new Color(0.6f, 0.5f, 0.4f); 
        public GameObject inkOpticJammerParticle; 
        
        private GameObject generatedApexPredator;
        private Color standardOriginalSkinTheme;
        private bool isEvadingEngaged = false;

        private void Start()
        {
            if (preySkinRenderer != null) standardOriginalSkinTheme = preySkinRenderer.material.color;
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
            if (apexPredatorPrefab != null && generatedApexPredator == null)
            {
                // Ambush placement coordinate geometry calculations
                Vector3 ambushCoordinatePlane = transform.position + (transform.right * 4.5f) + (transform.up * 1f);
                generatedApexPredator = Instantiate(apexPredatorPrefab, ambushCoordinatePlane, Quaternion.LookRotation(transform.position - ambushCoordinatePlane));
            }
        }

        public void OnModuleDeactivated()
        {
            if (generatedApexPredator != null) Destroy(generatedApexPredator);
            if (preySkinRenderer != null) preySkinRenderer.material.color = standardOriginalSkinTheme;
            isEvadingEngaged = false;
        }

        private void Update()
        {
            if (generatedApexPredator == null) return;

            float absolutePhysicalDistanceToPrey = Vector3.Distance(generatedApexPredator.transform.position, transform.position);
            Vector3 unitDirectionToPrey = (transform.position - generatedApexPredator.transform.position).normalized;

            // Optical Field of View (FoV) implementation tracking angle magnitude directly on the Matrix
            float angleDiscrepancyToPrey = Vector3.Angle(generatedApexPredator.transform.forward, unitDirectionToPrey);
            
            if (angleDiscrepancyToPrey <= neuralVisionAngleCone * 0.5f)
            {
                // Target has been visually acquired - Establish Chase Link
                Vector3 optimizedCalculatedTrajectory = unitDirectionToPrey;
                ExtrapolateRaycastObstacleAvoidance(ref optimizedCalculatedTrajectory);

                generatedApexPredator.transform.position += optimizedCalculatedTrajectory * chasePacingSpeed * Time.deltaTime;
                generatedApexPredator.transform.rotation = Quaternion.Slerp(generatedApexPredator.transform.rotation, Quaternion.LookRotation(optimizedCalculatedTrajectory), Time.deltaTime * 6f);
            }
            else
            {
                // Prey is completely outside the optical FoV cone. AI falls back to a confused Patrol Loop.
                generatedApexPredator.transform.Rotate(0, 45f * Time.deltaTime, 0); 
                generatedApexPredator.transform.position += generatedApexPredator.transform.forward * (chasePacingSpeed * 0.4f) * Time.deltaTime;
            }

            // Prey Biological System Overrides
            if (absolutePhysicalDistanceToPrey < 3.0f && !isEvadingEngaged)
            {
                EngageBiologicalChromaticResponse();
            }

            if (absolutePhysicalDistanceToPrey <= 1.0f)
            {
                // Extinction Danger Threshold Reached: Force orthogonal computing maneuver
                Vector3 mathematicalEvasionNode = Vector3.Cross(transform.up, unitDirectionToPrey);
                transform.position += mathematicalEvasionNode * 6f * Time.deltaTime;
            }
        }

        private void ExtrapolateRaycastObstacleAvoidance(ref Vector3 currentTrajectoryNode)
        {
            // Emits lasers out of the predator's head. If a rock is hit, it takes the normal vector and repels the predator path.
            Vector3 originPoint = generatedApexPredator.transform.position;
            Vector3 forwardRay = generatedApexPredator.transform.forward;
            
            if (Physics.Raycast(originPoint, forwardRay, out RaycastHit physicalHitDetection, 2.5f))
            {
                currentTrajectoryNode = Vector3.Lerp(currentTrajectoryNode, physicalHitDetection.normal, aiCollisionAvoidanceWeight * Time.deltaTime).normalized;
                Debug.DrawRay(originPoint, physicalHitDetection.normal * 2, Color.red);
            }
        }

        private void EngageBiologicalChromaticResponse()
        {
            isEvadingEngaged = true;
            Debug.Log("[Stealth AI Sub-System] Apex entity breached proximity defenses! Engaging Adaptive Coloration matrix & Jet Emission.");
            
            if (preySkinRenderer != null) preySkinRenderer.material.color = camouflageEnvironmentTone;

            if (inkDefensiveParticle != null)
            {
                GameObject opticalJammerEntity = Instantiate(inkDefensiveParticle, transform.position, Quaternion.identity);
                Destroy(opticalJammerEntity, 3.5f);
            }
        }
    }
}
