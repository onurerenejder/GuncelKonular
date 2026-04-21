using UnityEngine;
using ARFishApp.Core;
using ARFishApp.Interaction;
using System.Collections.Generic;

namespace ARFishApp.Modules
{
    public class QuizModule : MonoBehaviour, IModule
    {
        [Header("Quiz System Data")]
        public List<string> organsToFind = new List<string> { "Gills", "Heart", "Dorsal Fin" };
        public GameObject successParticlePrefab;
        public ParticleSystem errorBuzzerParticle;
        
        private int currentQuestionIndex = 0;
        private int currentScore = 0;

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

        public ModuleType GetModuleType() => ModuleType.Quiz;

        public void OnModuleActivated()
        {
            currentQuestionIndex = 0;
            currentScore = 0;
            AskNextQuestion();
        }

        public void OnModuleDeactivated()
        {
            Debug.Log($"[Quiz Module] Deactivated. Final Score: {currentScore}/{organsToFind.Count}");
        }

        private void AskNextQuestion()
        {
            if (currentQuestionIndex < organsToFind.Count)
            {
                Debug.Log($"[Quiz Module] QUESTION {currentQuestionIndex + 1}: Find the {organsToFind[currentQuestionIndex]}!");
            }
            else
            {
                Debug.Log($"[Quiz Module] QUIZ COMPLETE! You scored: {currentScore} / {organsToFind.Count}");
            }
        }

        public void ValidateHotspotTap(HotspotNode node)
        {
            // Ignore if quiz is not active or finished
            if (SystemStateManager.Instance.CurrentModule != GetModuleType() || currentQuestionIndex >= organsToFind.Count) return;

            string target = organsToFind[currentQuestionIndex];

            if (node.organName == target)
            {
                Debug.Log($"[Quiz Module] CORRECT! You found the {target}.");
                currentScore++;
                if (successParticlePrefab != null) Instantiate(successParticlePrefab, node.transform.position, Quaternion.identity);
            }
            else
            {
                Debug.Log($"[Quiz Module] WRONG! You tapped the {node.organName}, not the {target}.");
                if (errorBuzzerParticle != null) errorBuzzerParticle.Play();
            }

            currentQuestionIndex++; // Move to next question regardless of correct/wrong
            AskNextQuestion();
        }
    }
}
