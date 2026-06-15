using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

        private SimplePool _predatorPool;
        private SimplePool _jammerPool;

        private void Start()
        {
            if (preySkinRenderer != null) standardOriginalSkinTheme = preySkinRenderer.material.color;
            if (apexPredatorPrefab != null) _predatorPool = new SimplePool(apexPredatorPrefab, transform, 1);
            if (inkOpticJammerParticle != null) _jammerPool = new SimplePool(inkOpticJammerParticle, transform, 2);
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (_predatorPool != null) _predatorPool.Dispose();
            if (_jammerPool != null) _jammerPool.Dispose();
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
            if (_predatorPool != null && generatedApexPredator == null)
            {
                Vector3 ambushCoordinatePlane = transform.position + (transform.right * 4.5f) + (transform.up * 1f);
                generatedApexPredator = _predatorPool.Get(ambushCoordinatePlane, Quaternion.LookRotation(transform.position - ambushCoordinatePlane));
            }
        }

        public void OnModuleDeactivated()
        {
            if (generatedApexPredator != null)
            {
                if (_predatorPool != null)
                    _predatorPool.Return(generatedApexPredator);
                else
                    Destroy(generatedApexPredator);
                generatedApexPredator = null;
            }
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

            if (_jammerPool != null)
            {
                GameObject jammer = _jammerPool.Get(transform.position, Quaternion.identity);
                StartCoroutine(ReturnToPoolAfter(jammer, _jammerPool, 3.5f));
            }
        }

        private IEnumerator ReturnToPoolAfter(GameObject go, SimplePool pool, float delay)
        {
            yield return new WaitForSeconds(delay);
            pool.Return(go);
        }

        private sealed class SimplePool
        {
            private readonly Stack<GameObject> _stack = new Stack<GameObject>();
            private readonly GameObject _prefab;
            private readonly Transform _parent;

            public SimplePool(GameObject prefab, Transform parent, int warmCount = 1)
            {
                _prefab = prefab;
                _parent = parent;
                for (int i = 0; i < warmCount; i++)
                    _stack.Push(CreateInstance());
            }

            public GameObject Get(Vector3 pos, Quaternion rot)
            {
                GameObject go = _stack.Count > 0 ? _stack.Pop() : CreateInstance();
                go.transform.SetPositionAndRotation(pos, rot);
                go.SetActive(true);
                return go;
            }

            public void Return(GameObject go)
            {
                if (go == null) return;
                go.SetActive(false);
                go.transform.SetParent(_parent, false);
                _stack.Push(go);
            }

            public void Dispose()
            {
                while (_stack.Count > 0)
                {
                    GameObject go = _stack.Pop();
                    if (go != null) Object.Destroy(go);
                }
            }

            private GameObject CreateInstance()
            {
                GameObject go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                return go;
            }
        }
    }
}
