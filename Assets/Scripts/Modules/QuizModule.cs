using UnityEngine;
using UnityEngine.UI;
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

        [Header("UI References")]
        [Tooltip("Ana quiz paneli — soruyu ve ilerlemeyi gösterir")]
        public GameObject quizPanel;
        [Tooltip("Soru metnini gösteren Text bileşeni")]
        public Text questionText;
        [Tooltip("1 / 3 gibi ilerleme bilgisini gösterir")]
        public Text progressText;
        [Tooltip("Anlık skoru gösterir")]
        public Text scoreText;
        [Tooltip("Tüm sorular bitince açılan sonuç paneli")]
        public GameObject resultPanel;
        [Tooltip("Sonuç panelindeki toplam skor metni")]
        public Text resultScoreText;
        [Tooltip("Sonuç panelindeki yorum metni (Harika! / İyi iş! vb.)")]
        public Text resultCommentText;

        [Header("State Tracking Variables")]
        private int currentQuestionIndex = 0;
        private int currentGlobalCalculatedScore = 0;
        private float continuousQuestionTimer = 0f;

        private void Start()
        {
            if (cloudQuestionDatabase.Count == 0)
            {
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Gills", questionDescription = "Balığın suda çözünmüş oksijeni alan organı hangisidir?" });
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Heart", questionDescription = "Kanı tüm damar sistemine pompalayan organ hangisidir?" });
                cloudQuestionDatabase.Add(new QuizQuestion { expectedHotspotId = "Dorsal Fin", questionDescription = "Balığın dengesini koruyan ve ani dönüşlere yardım eden sırt yüzgeci hangisidir?" });
            }

            EnsureQuizUI();

            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            HotspotNode.OnAnyHotspotTapped += ValidateHotspotTap;
            OnModuleDeactivated();
        }

        private void EnsureQuizUI()
        {
            if (quizPanel != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            quizPanel = CreatePanel(canvas.transform, "QuizPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(0f, 160f),
                new Color(0f, 0f, 0f, 0.75f));

            questionText   = CreateLabel(quizPanel.transform, "QuestionText",  new Vector2(10f, 50f), new Vector2(-10f, -10f), 16, TextAnchor.UpperLeft);
            progressText   = CreateLabel(quizPanel.transform, "ProgressText",  new Vector2(10f, -10f), new Vector2(-10f, -40f), 13, TextAnchor.UpperRight);
            scoreText      = CreateLabel(quizPanel.transform, "ScoreText",     new Vector2(10f, -10f), new Vector2(-10f, -40f), 13, TextAnchor.UpperLeft);

            resultPanel = CreatePanel(canvas.transform, "QuizResultPanel",
                new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0f, 0.1f, 0.2f, 0.92f));

            resultScoreText   = CreateLabel(resultPanel.transform, "ResultScore",   new Vector2(10f, -20f), new Vector2(-10f, -80f),  20, TextAnchor.MiddleCenter);
            resultCommentText = CreateLabel(resultPanel.transform, "ResultComment",  new Vector2(10f, -90f), new Vector2(-10f, -140f), 14, TextAnchor.MiddleCenter);

            Button retryBtn = CreateButton(resultPanel.transform, "Tekrar Dene", new Vector2(20f, -150f), new Vector2(-20f, -190f));
            retryBtn.onClick.AddListener(OnModuleActivated);

            resultPanel.SetActive(false);
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.pivot      = pivot;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static Text CreateLabel(Transform parent, string name,
            Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Text t = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button CreateButton(Transform parent, string label,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0f);
            rt.anchorMax = new Vector2(0.9f, 0f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.5f, 0.9f);
            Button btn = go.AddComponent<Button>();

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            Text txt = labelGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 14;
            txt.color     = Color.white;
            txt.text      = label;
            txt.alignment = TextAnchor.MiddleCenter;
            return btn;
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
            HotspotNode.OnAnyHotspotTapped -= ValidateHotspotTap;
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

            SetPanelActive(quizPanel, true);
            SetPanelActive(resultPanel, false);
            UpdateScoreUI();
            InvokeNextDatabaseQuestion();
        }

        public void OnModuleDeactivated()
        {
            SetPanelActive(quizPanel, false);
            SetPanelActive(resultPanel, false);
            Debug.Log($"[Gamification UI Engine] Quiz Terminated. Final Secured Score: {currentGlobalCalculatedScore}");
        }

        private void Update()
        {
            // Frame execution logic strictly computing time depletion for the score formula
            if (SystemStateManager.Instance != null && SystemStateManager.Instance.CurrentModule == GetModuleType() && currentQuestionIndex < cloudQuestionDatabase.Count)
            {
                continuousQuestionTimer += Time.deltaTime;
            }
        }

        private void InvokeNextDatabaseQuestion()
        {
            if (currentQuestionIndex < cloudQuestionDatabase.Count)
            {
                continuousQuestionTimer = 0f;
                var queueItem = cloudQuestionDatabase[currentQuestionIndex];
                Debug.Log($"[Gamification Engine] LEVEL {currentQuestionIndex + 1}: {queueItem.questionDescription}");

                if (questionText != null)
                    questionText.text = queueItem.questionDescription;

                if (progressText != null)
                    progressText.text = $"{currentQuestionIndex + 1} / {cloudQuestionDatabase.Count}";
            }
            else
            {
                Debug.Log($"[Gamification Engine] GRAND FINALE! All modules analyzed. Cumulative Player Score: {currentGlobalCalculatedScore}");
                ShowResultScreen();
            }
        }

        private void ShowResultScreen()
        {
            SetPanelActive(quizPanel, false);
            SetPanelActive(resultPanel, true);

            if (resultScoreText != null)
                resultScoreText.text = $"Toplam Puan: {currentGlobalCalculatedScore}";

            if (resultCommentText != null)
            {
                int maxPossible = cloudQuestionDatabase.Count * 100;
                float ratio = maxPossible > 0 ? (float)currentGlobalCalculatedScore / maxPossible : 0f;
                resultCommentText.text = ratio >= 0.8f ? "Harika! Uzman biyolog adayı!" :
                                         ratio >= 0.5f ? "İyi iş! Biraz daha çalış." :
                                         "Tekrar dene, daha iyisini yapabilirsin!";
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Puan: {currentGlobalCalculatedScore}";
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        public void ValidateHotspotTap(HotspotNode node)
        {
            // Pre-validation to discard arbitrary taps out-of-context
            if (node == null || SystemStateManager.Instance == null) return;
            if (SystemStateManager.Instance.CurrentModule != GetModuleType() || currentQuestionIndex >= cloudQuestionDatabase.Count) return;

            var activeLevelTarget = cloudQuestionDatabase[currentQuestionIndex];

            if (node.organName == activeLevelTarget.expectedHotspotId)
            {
                // Dynamic time-depletion scoring algorithm. You lose 2 points for every second you hesitate.
                int hesitationPenalty = Mathf.Clamp(Mathf.FloorToInt(continuousQuestionTimer * 2f), 0, activeLevelTarget.baseLevelPoints - 20);
                int earnedPoints = activeLevelTarget.baseLevelPoints - hesitationPenalty;
                currentGlobalCalculatedScore += earnedPoints;
                UpdateScoreUI();

                Debug.Log($"[Validation System] CORRECT IDENTITY! You clicked {node.organName}. Awarding {earnedPoints} Points! (Solved in {continuousQuestionTimer:F1}s). Total Vault: {currentGlobalCalculatedScore}");

                if (successConfettiParticle != null) Instantiate(successConfettiParticle, node.transform.position, Quaternion.identity);
            }
            else
            {
                Debug.Log($"[Validation System] ASSET MISMATCH! Sensor received {node.organName}, but mission demands {activeLevelTarget.expectedHotspotId}. Zero parameters awarded.");
                if (errorBuzzerEmission != null) errorBuzzerEmission.Play();
            }

            currentQuestionIndex++;
            InvokeNextDatabaseQuestion();
        }
    }
}
