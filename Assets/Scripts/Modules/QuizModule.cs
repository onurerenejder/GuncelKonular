using UnityEngine;
using ARFishApp.Core;
using ARFishApp.Interaction;
using System.Collections.Generic;

namespace ARFishApp.Modules
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string expectedHotspotId;
        [TextArea] public string questionDescription;
        [Tooltip("The starting theoretical maximum points awarded for this level.")]
        public int baseLevelPoints = 100;
    }

    public class QuizModule : MonoBehaviour, IModule
    {
        [Header("Gamification Database System")]
        public List<QuizQuestion> cloudQuestionDatabase = new List<QuizQuestion>();
        public GameObject successConfettiParticle;
        public ParticleSystem errorBuzzerEmission;
        
        [Header("State Tracking Variables")]
        private int currentQuestionIndex = 0;
        private int currentGlobalCalculatedScore = 0;
        private float continuousQuestionTimer = 0f;

        private void Start()
        {
            // Safely seed a robust default database to prevent NRE limits (Can be overwritten by API Cloud calls)
            if (cloudQuestionDatabase.Count == 0)
            {
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Gills", questionDescription = "Which critical organ extracts dissolved oxygen from the water current?" });
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Heart", questionDescription = "Which organ pumps blood throughout the fish's vascular system?" });
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Dorsal Fin", questionDescription = "Identify the vertical fin that stabilizes the fish against rolling and assists in sudden turns." });
            }

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

        public ModuleType GetModuleType() => ModuleType.Quiz;

        public void OnModuleActivated()
        {
            currentQuestionIndex = 0;
            currentGlobalCalculatedScore = 0;
            InvokeNextDatabaseQuestion();
        }

        public void OnModuleDeactivated()
        {
            Debug.Log($"[Gamification UI Engine] Quiz Terminated. Final Secured Score: {currentGlobalCalculatedScore}");
        }

        private void Update()
        {
            // Frame execution logic strictly computing time depletion for the score formula
            if (SystemStateManager.Instance.CurrentModule == GetModuleType() && currentQuestionIndex < cloudQuestionDatabase.Count)
            {
                continuousQuestionTimer += Time.deltaTime;
            }
        }

        private void InvokeNextDatabaseQuestion()
        {
            if (currentQuestionIndex < cloudQuestionDatabase.Count)
            {
                continuousQuestionTimer = 0f; // Reset chronometer
                var queueItem = cloudQuestionDatabase[currentQuestionIndex];
                Debug.Log($"[Gamification Engine] LEVEL {currentQuestionIndex + 1}: {queueItem.questionDescription}");
                // This string can now be forwarded to a TextMeshPro UI Object dynamically!
            }
            else
            {
                Debug.Log($"[Gamification Engine] GRAND FINALE! All modules analyzed. Cumulative Player Score: {currentGlobalCalculatedScore}");
            }
        }

        public void ValidateHotspotTap(HotspotNode node)
        {
            // Pre-validation to discard arbitrary taps out-of-context
            if (SystemStateManager.Instance.CurrentModule != GetModuleType() || currentQuestionIndex >= cloudQuestionDatabase.Count) return;

            var activeLevelTarget = cloudQuestionDatabase[currentQuestionIndex];

            if (node.organName == activeLevelTarget.expectedHotspotId)
            {
                // Dynamic time-depletion scoring algorithm. You lose 2 points for every second you hesitate.
                int hesitationPenalty = Mathf.Clamp(Mathf.FloorToInt(continuousQuestionTimer * 2f), 0, activeLevelTarget.baseLevelPoints - 20);
                int earnedPoints = activeLevelTarget.baseLevelPoints - hesitationPenalty;
                currentGlobalCalculatedScore += earnedPoints;

                Debug.Log($"[Validation System] CORRECT IDENTITY! You clicked {node.organName}. Awarding {earnedPoints} Points! (Solved in {continuousQuestionTimer:F1}s). Total Vault: {currentGlobalCalculatedScore}");
                
                if (successConfettiParticle != null) Instantiate(successConfettiParticle, node.transform.position, Quaternion.identity);
            }
            else
            {
                Debug.Log($"[Validation System] ASSET MISMATCH! Sensor received {node.organName}, but mission demands {activeLevelTarget.expectedHotspotId}. Zero parameters awarded.");
                if (errorBuzzerEmission != null) errorBuzzerEmission.Play();
            }

            // Push state array index forward
            currentQuestionIndex++;
            InvokeNextDatabaseQuestion();
        }
    }
}
