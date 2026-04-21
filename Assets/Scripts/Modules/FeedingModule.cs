using UnityEngine;
using System.Collections;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class FeedingModule : MonoBehaviour, IModule
    {
        public enum DietType { Carnivore, Herbivore }

        [Header("Feeding Anatomy & Logic")]
        public DietType fishDiet;
        public Animator jawAnimator;
        public Transform mouthSocket;
        
        [Header("Dynamic Procedural Food Elements")]
        public GameObject meatPreyPrefab;
        public GameObject vegetationPrefab;
        public ParticleSystem hitBloodMuzzle;
        public ParticleSystem hitAlgaeMuzzle;

        private GameObject currentTarget;

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

        public ModuleType GetModuleType() => ModuleType.Feeding;

        public void OnModuleActivated()
        {
            StopAllCoroutines();
            StartCoroutine(FeedingSequence());
        }

        public void OnModuleDeactivated()
        {
            if (currentTarget != null) Destroy(currentTarget);
            if (jawAnimator != null) jawAnimator.SetBool("IsBiting", false);
        }

        private IEnumerator FeedingSequence()
        {
            // Spawn Food floating in front of the mouth
            Vector3 spawnPoint = transform.position + (transform.forward * 1.5f) + (Vector3.up * 0.5f);
            
            if (fishDiet == DietType.Carnivore && meatPreyPrefab != null)
                currentTarget = Instantiate(meatPreyPrefab, spawnPoint, Quaternion.identity);
            else if (fishDiet == DietType.Herbivore && vegetationPrefab != null)
                currentTarget = Instantiate(vegetationPrefab, spawnPoint, Quaternion.identity);

            // Wait for user or simulation to register target existence
            yield return new WaitForSeconds(1.0f);

            // Execute Jaw Extension (Vertex/Bone animation)
            if (jawAnimator != null) jawAnimator.SetTrigger("BiteTrigger");
            
            // Simulating a fast strike leap forward
            float dashTime = 0.2f;
            Vector3 startPos = transform.position;
            
            while (dashTime > 0)
            {
                transform.position = Vector3.Lerp(transform.position, spawnPoint, 0.1f);
                dashTime -= Time.deltaTime;
                yield return null;
            }

            // Consumption Logic
            if (currentTarget != null)
            {
                Destroy(currentTarget);
                
                // Spawn appropriate debris/gore particles based on biological diet type
                if (fishDiet == DietType.Carnivore && hitBloodMuzzle != null)
                    Instantiate(hitBloodMuzzle, mouthSocket.position, Quaternion.identity).Play();
                else if (fishDiet == DietType.Herbivore && hitAlgaeMuzzle != null)
                    Instantiate(hitAlgaeMuzzle, mouthSocket.position, Quaternion.identity).Play();
            }

            // Return to neutral hover
            yield return new WaitForSeconds(0.5f);
            float returnTime = 0.5f;
            while(returnTime > 0)
            {
                transform.position = Vector3.Lerp(transform.position, startPos, 0.05f);
                returnTime -= Time.deltaTime;
                yield return null;
            }
        }
    }
}
