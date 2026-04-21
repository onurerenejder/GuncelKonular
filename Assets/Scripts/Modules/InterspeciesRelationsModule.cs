using UnityEngine;
using ARFishApp.Core;
using System.Collections.Generic;

namespace ARFishApp.Modules
{
    public class InterspeciesRelationsModule : MonoBehaviour, IModule
    {
        [Header("Symbiosis Setup")]
        public GameObject symbioticPartnerPrefab; 
        public Transform symbioticAttachPoint;
        private GameObject spawnedPartner;

        [Header("Boids Intelligence")]
        public GameObject schoolingFishPrefab;
        public int schoolSize = 20;
        public float boidSpeed = 2f;
        public Camera playerCamera; // Used for dynamic avoidance

        private List<Transform> activeSchool = new List<Transform>();
        private List<Vector3> boidVelocities = new List<Vector3>();

        private void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;
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

        public ModuleType GetModuleType() => ModuleType.InterspeciesRelations;

        public void OnModuleActivated()
        {
            // Trigger 1: Interaction with symbiotic species (e.g. Clownfish & Anemone mapping)
            if (symbioticPartnerPrefab != null && symbioticAttachPoint != null)
            {
                spawnedPartner = Instantiate(symbioticPartnerPrefab, symbioticAttachPoint.position, symbioticAttachPoint.rotation);
                spawnedPartner.transform.SetParent(this.transform);
            }

            // Trigger 2: Generation of Boids Algorithm cluster
            if (schoolingFishPrefab != null && activeSchool.Count == 0)
            {
                for (int i = 0; i < schoolSize; i++)
                {
                    GameObject obj = Instantiate(schoolingFishPrefab, transform.position + Random.insideUnitSphere * 2f, Random.rotation);
                    obj.transform.SetParent(this.transform);
                    activeSchool.Add(obj.transform);
                    boidVelocities.Add(obj.transform.forward * boidSpeed);
                }
            }
        }

        public void OnModuleDeactivated()
        {
            if (spawnedPartner != null) Destroy(spawnedPartner);
            foreach (var b in activeSchool) { if (b != null) Destroy(b.gameObject); }
            activeSchool.Clear();
            boidVelocities.Clear();
        }

        private void Update()
        {
            if (activeSchool.Count == 0) return;

            // Boid Flocking execution with User avoidance rules
            for (int i = 0; i < activeSchool.Count; i++)
            {
                Transform boid = activeSchool[i];
                Vector3 vel = boidVelocities[i];
                Vector3 cohesion = Vector3.zero;
                Vector3 separation = Vector3.zero;
                Vector3 alignment = Vector3.zero;
                int count = 0;

                // Flee from AR Camera if user gets too close to the virtual bodies
                if (playerCamera != null && Vector3.Distance(boid.position, playerCamera.transform.position) < 1.0f)
                {
                    Vector3 fleeForce = (boid.position - playerCamera.transform.position).normalized * 5f;
                    vel += fleeForce * Time.deltaTime;
                }

                for (int j = 0; j < activeSchool.Count; j++)
                {
                    if (i == j) continue;
                    Transform other = activeSchool[j];
                    float dist = Vector3.Distance(boid.position, other.position);

                    if (dist < 1.5f)
                    {
                        cohesion += other.position;
                        alignment += boidVelocities[j];
                        separation += (boid.position - other.position) / dist;
                        count++;
                    }
                }

                if (count > 0)
                {
                    cohesion = (cohesion / count - boid.position) * 0.5f;
                    alignment = (alignment / count) * 0.5f;
                    separation = separation * 1.5f;
                    vel += (cohesion + alignment + separation) * Time.deltaTime;
                }

                Vector3 centerPull = (transform.position - boid.position) * 0.3f;
                vel += centerPull * Time.deltaTime;
                
                vel = vel.normalized * boidSpeed;
                boid.position += vel * Time.deltaTime;
                if (vel != Vector3.zero)
                    boid.rotation = Quaternion.Slerp(boid.rotation, Quaternion.LookRotation(vel), Time.deltaTime * 4f);

                boidVelocities[i] = vel;
            }
        }
    }
}
