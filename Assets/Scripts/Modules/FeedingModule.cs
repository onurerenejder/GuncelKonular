using UnityEngine;
using System.Collections;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class FeedingModule : MonoBehaviour, IModule
    {
        public enum DietType { Carnivore, Herbivore }

        [Header("Procedural Inverse Kinematics (IK Spine Bending)")]
        [Tooltip("The actual neck/spine bone of the 3D model. We procedurally control its local rotation to track the food.")]
        public Transform headBone;
        public float ikTrackingSpeed = 5.0f;
        public float maxProceduralBendAngle = 45f;

        [Header("Feeding Logic Engine")]
        public DietType fishDiet;
        public Animator jawAnimator;
        public Transform mouthSocket;
        
        [Header("Procedural Physics & Particle Interactions")]
        public GameObject meatPreyPrefab;
        public GameObject vegetationPrefab;
        public ParticleSystem hitBloodMuzzle;
        public ParticleSystem hitAlgaeMuzzle;

        private GameObject currentFoodTarget;

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

        public ModuleType GetModuleType() => ModuleType.Feeding;

        public void OnModuleActivated()
        {
            StopAllCoroutines();
            StartCoroutine(FeedingSequenceController());
        }

        public void OnModuleDeactivated()
        {
            if (currentFoodTarget != null) Destroy(currentFoodTarget);
            if (jawAnimator != null) jawAnimator.SetBool("IsBiting", false);
            
            // Release IK overrides mathematically
            if (headBone != null) headBone.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// LateUpdate is strictly required to override animation system transforms.
        /// Here we calculate an IK Lookat constraint manually towards the food.
        /// </summary>
        private void LateUpdate()
        {
            if (SystemStateManager.Instance.CurrentModule == GetModuleType() && currentFoodTarget != null && headBone != null)
            {
                // Vector geometry to point spine/head bone at the food
                Vector3 directionToFood = (currentFoodTarget.transform.position - headBone.position).normalized;
                Quaternion targetLookRotation = Quaternion.LookRotation(directionToFood, transform.up);
                
                // Clamping the spherical rotation to strictly avoid breaking the spine's topology
                float deviationAngle = Quaternion.Angle(transform.rotation, targetLookRotation);
                if (deviationAngle <= maxProceduralBendAngle)
                {
                    headBone.rotation = Quaternion.Slerp(headBone.rotation, targetLookRotation, Time.deltaTime * ikTrackingSpeed);
                }
            }
        }

        private IEnumerator FeedingSequenceController()
        {
            // Calculate a completely randomized spawn zone within the fish's vision
            Vector3 spawnPoint = transform.position + (transform.forward * 2.0f) + (transform.right * Random.Range(-0.8f, 0.8f));
            
            if (fishDiet == DietType.Carnivore && meatPreyPrefab != null)
                currentFoodTarget = Instantiate(meatPreyPrefab, spawnPoint, Quaternion.identity);
            else if (fishDiet == DietType.Herbivore && vegetationPrefab != null)
                currentFoodTarget = Instantiate(vegetationPrefab, spawnPoint, Quaternion.identity);

            // Wait 1.5 seconds, specifically allowing the IK math to bend the fish toward the food
            yield return new WaitForSeconds(1.5f);

            if (jawAnimator != null) jawAnimator.SetTrigger("BiteTrigger");
            
            // Execute Burst Translation
            float executionTimeRemaining = 0.25f;
            Vector3 defaultAnchorPos = transform.position;
            
            while (executionTimeRemaining > 0)
            {
                transform.position = Vector3.Lerp(transform.position, spawnPoint, 0.15f);
                executionTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            // Cleanup & Digestion phase
            if (currentFoodTarget != null)
            {
                Destroy(currentFoodTarget);
                
                if (fishDiet == DietType.Carnivore && hitBloodMuzzle != null)
                    Instantiate(hitBloodMuzzle, mouthSocket.position, Quaternion.identity).Play();
                else if (fishDiet == DietType.Herbivore && hitAlgaeMuzzle != null)
                    Instantiate(hitAlgaeMuzzle, mouthSocket.position, Quaternion.identity).Play();
            }

            yield return new WaitForSeconds(0.4f);
            
            // Recovery algorithm back to center anchor
            float returnTimeLimit = 0.6f;
            while(returnTimeLimit > 0)
            {
                float polynomialEase = 1f - (returnTimeLimit / 0.6f);
                transform.position = Vector3.Lerp(transform.position, defaultAnchorPos, polynomialEase * 0.1f);
                returnTimeLimit -= Time.deltaTime;
                yield return null;
            }
        }
    }
}
