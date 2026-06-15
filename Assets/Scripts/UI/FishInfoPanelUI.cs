using ARFishApp.Core;
using ARFishApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishApp.UI
{
    public class FishInfoPanelUI : MonoBehaviour
    {
        private const string CanvasName = "Canvas_MainUI";
        private const string PanelName = "FishInfoPanel";

        public Canvas targetCanvas;
        public Vector2 bubbleOffsetFromMouth = new Vector2(44f, 34f);
        public Vector2 minSize = new Vector2(300f, 108f);
        public Vector2 maxSize = new Vector2(410f, 165f);
        public bool followFishMouth = true;

        private GameObject panelObject;
        private RectTransform panelRect;
        private RectTransform contentRect;
        private LayoutElement bodyLayout;
        private Text titleText;
        private Text bodyText;
        private RectTransform[] connectorDots;
        private SelectableFish currentFish;
        private Vector2 currentSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindFirstObjectByType<FishInfoPanelUI>() != null) return;

            GameObject uiObject = new GameObject("FishInfoPanelUI");
            uiObject.AddComponent<FishInfoPanelUI>();
        }

        private void Awake()
        {
            BuildPanel();
            Hide();
        }

        private void OnEnable()
        {
            FishSelectionManager.OnFishSelected += HandleFishSelected;

            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            FishSelectionManager.OnFishSelected -= HandleFishSelected;

            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            if (FishSelectionManager.Instance != null)
            {
                currentFish = FishSelectionManager.Instance.CurrentFish;
            }
        }

        private void LateUpdate()
        {
            if (followFishMouth && panelObject != null && panelObject.activeSelf)
            {
                UpdateBubblePosition();
            }
        }

        private void HandleFishSelected(SelectableFish selectedFish)
        {
            currentFish = selectedFish;

            if (SystemStateManager.Instance != null && SystemStateManager.Instance.CurrentModule != ModuleType.None)
            {
                ShowForModule(SystemStateManager.Instance.CurrentModule);
            }
        }

        private void HandleStateChanged(ModuleType moduleType)
        {
            ShowForModule(moduleType);
        }

        private void ShowForModule(ModuleType moduleType)
        {
            if (moduleType == ModuleType.None)
            {
                Hide();
                return;
            }

            if (currentFish == null && FishSelectionManager.Instance != null)
            {
                currentFish = FishSelectionManager.Instance.CurrentFish;
            }

            FishJsonData data = currentFish != null ? FishJsonDatabase.Load(currentFish.id) : null;
            if (data == null)
            {
                FishData fallbackData = currentFish != null ? currentFish.fishData : null;
                if (fallbackData != null)
                {
                    SetText(BuildTitle(fallbackData, moduleType), ResolveBody(fallbackData, moduleType));
                }
                else
                {
                    SetText("Bilgi bulunamadi", "Bu balik icin JSON verisi henuz hazir degil.");
                }

                Show();
                return;
            }

            string title = BuildTitle(data, moduleType);
            string body = ResolveBody(data, moduleType);
            SetText(title, body);
            Show();
        }

        private static string BuildTitle(FishJsonData data, ModuleType moduleType)
        {
            string moduleName = ResolveModuleDisplayName(moduleType);
            return $"{data.displayName} - {moduleName}";
        }

        private static string BuildTitle(FishData data, ModuleType moduleType)
        {
            string moduleName = ResolveModuleDisplayName(moduleType);
            string fishName = string.IsNullOrWhiteSpace(data.FishName) ? "Balik" : data.FishName;
            return $"{fishName} - {moduleName}";
        }

        private static string ResolveBody(FishJsonData data, ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Anatomy:
                    return data.anatomy;
                case ModuleType.Habitat:
                    return data.habitat;
                case ModuleType.Feeding:
                    return data.feeding;
                case ModuleType.InterspeciesRelations:
                    return data.relations;
                case ModuleType.PredatorPrey:
                    return data.predatorPrey;
                case ModuleType.Quiz:
                    return BuildQuizText(data);
                default:
                    return data.general;
            }
        }

        private static string ResolveBody(FishData data, ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Anatomy:
                    return data.AnatomyDescription;
                case ModuleType.Habitat:
                    return data.HabitatType;
                case ModuleType.Feeding:
                    return data.DietDescription;
                case ModuleType.PredatorPrey:
                    return data.Predators != null && data.Predators.Length > 0
                        ? string.Join(", ", data.Predators)
                        : data.DietDescription;
                default:
                    return data.GeneralDescription;
            }
        }

        private static string ResolveModuleDisplayName(ModuleType moduleType)
        {
            switch (moduleType)
            {
                case ModuleType.Anatomy:
                    return "Anatomi";
                case ModuleType.Habitat:
                    return "Habitat";
                case ModuleType.Feeding:
                    return "Beslenme";
                case ModuleType.InterspeciesRelations:
                    return "Turler Arasi Iliskiler";
                case ModuleType.PredatorPrey:
                    return "Av/Avci";
                case ModuleType.Quiz:
                    return "Quiz";
                default:
                    return "Genel Bilgi";
            }
        }

        private static string BuildQuizText(FishJsonData data)
        {
            if (data.quiz == null || data.quiz.Length == 0)
            {
                return "Bu balik icin quiz sorulari daha sonra eklenecek.";
            }

            FishQuizItem firstQuestion = data.quiz[0];
            return $"Soru: {firstQuestion.question}\nCevap: {firstQuestion.answer}";
        }

        private void BuildPanel()
        {
            if (targetCanvas == null)
            {
                GameObject canvasObject = GameObject.Find(CanvasName);
                if (canvasObject != null)
                {
                    targetCanvas = canvasObject.GetComponent<Canvas>();
                }
            }

            if (targetCanvas == null) return;
            if (targetCanvas.transform.Find(PanelName) != null) return;

            panelObject = new GameObject(PanelName);
            panelObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            this.panelRect = panelRect;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            currentSize = minSize;
            panelRect.sizeDelta = currentSize;

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.sprite = CreateRoundedBubbleSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(1f, 1f, 1f, 0.96f);

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(panelObject.transform, false);

            contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(24f, 18f);
            contentRect.offsetMax = new Vector2(-24f, -18f);

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateText(contentObject.transform, "Title", 13, FontStyle.Bold, TextAnchor.MiddleLeft);
            bodyText = CreateText(contentObject.transform, "Body", 12, FontStyle.Normal, TextAnchor.UpperLeft);

            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 22f;

            bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLayout.preferredHeight = 105f;
            bodyLayout.flexibleHeight = 1f;

            CreateConnectorDots();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.04f, 0.08f, 0.1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;

            return text;
        }

        private void UpdateBubblePosition()
        {
            if (panelRect == null || targetCanvas == null || FishSelectionManager.Instance == null) return;

            GameObject model = FishSelectionManager.Instance.CurrentModelInstance;
            Bounds bounds;
            if (!TryGetRendererBounds(model, out bounds)) return;

            Camera camera = Camera.main;
            if (camera == null) return;

            Vector3 mouthWorldPosition = new Vector3(bounds.max.x, bounds.center.y + bounds.extents.y * 0.02f, bounds.center.z);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, mouthWorldPosition);

            RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
            Vector2 canvasPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out canvasPoint);

            Vector2 targetPosition = ResolveBubblePosition(canvasRect, canvasPoint, bounds, camera);

            panelRect.anchoredPosition = targetPosition;
            UpdateConnectorDots(canvasPoint, targetPosition);
        }

        private Vector2 ResolveBubblePosition(RectTransform canvasRect, Vector2 mouthPoint, Bounds modelBounds, Camera camera)
        {
            float halfWidth = canvasRect.rect.width * 0.5f;
            float halfHeight = canvasRect.rect.height * 0.5f;
            float leftLimit = -halfWidth + 18f;
            float rightLimit = halfWidth - currentSize.x - 18f;
            float bottomLimit = -halfHeight + 132f;
            float topLimit = halfHeight - currentSize.y - 98f;

            Rect modelRect = GetModelCanvasRect(canvasRect, camera, modelBounds);
            Vector2[] candidates =
            {
                new Vector2(mouthPoint.x + bubbleOffsetFromMouth.x, mouthPoint.y + bubbleOffsetFromMouth.y),
                new Vector2(mouthPoint.x - currentSize.x - bubbleOffsetFromMouth.x, mouthPoint.y + bubbleOffsetFromMouth.y),
                new Vector2(mouthPoint.x + bubbleOffsetFromMouth.x, mouthPoint.y - currentSize.y - bubbleOffsetFromMouth.y),
                new Vector2(mouthPoint.x - currentSize.x - bubbleOffsetFromMouth.x, mouthPoint.y - currentSize.y - bubbleOffsetFromMouth.y)
            };

            Vector2 bestPosition = candidates[0];
            float bestScore = float.MaxValue;

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2 candidate = candidates[i];
                candidate.x = Mathf.Clamp(candidate.x, leftLimit, rightLimit);
                candidate.y = Mathf.Clamp(candidate.y, bottomLimit, topLimit);

                Rect bubbleRect = new Rect(candidate.x, candidate.y, currentSize.x, currentSize.y);
                float overlapPenalty = GetOverlapArea(bubbleRect, modelRect) * 12f;
                float distancePenalty = Vector2.Distance(candidate, mouthPoint) * 0.2f;
                float edgePenalty = Mathf.Abs(candidate.x) * 0.01f;
                float score = overlapPenalty + distancePenalty + edgePenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate;
                }
            }

            return bestPosition;
        }

        private static Vector2 WorldToCanvasPoint(RectTransform canvasRect, Camera camera, Vector3 worldPosition)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            Vector2 canvasPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out canvasPoint);
            return canvasPoint;
        }

        private static Rect GetModelCanvasRect(RectTransform canvasRect, Camera camera, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            Vector2 firstPoint = WorldToCanvasPoint(canvasRect, camera, corners[0]);
            float minX = firstPoint.x;
            float maxX = firstPoint.x;
            float minY = firstPoint.y;
            float maxY = firstPoint.y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = WorldToCanvasPoint(canvasRect, camera, corners[i]);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            const float padding = 26f;
            return Rect.MinMaxRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
        }

        private static float GetOverlapArea(Rect a, Rect b)
        {
            float overlapX = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            float overlapY = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            return overlapX * overlapY;
        }

        private void CreateConnectorDots()
        {
            connectorDots = new RectTransform[3];
            float[] sizes = { 14f, 20f, 28f };

            for (int i = 0; i < connectorDots.Length; i++)
            {
                GameObject dotObject = new GameObject($"ConnectorDot_{i + 1}");
                dotObject.transform.SetParent(panelObject.transform, false);

                RectTransform dotRect = dotObject.AddComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0f, 0f);
                dotRect.anchorMax = new Vector2(0f, 0f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta = Vector2.one * sizes[i];

                Image dotImage = dotObject.AddComponent<Image>();
                dotImage.sprite = CreateDotSprite();
                dotImage.type = Image.Type.Sliced;
                dotImage.color = Color.white;
                dotImage.raycastTarget = false;

                connectorDots[i] = dotRect;
            }
        }

        private void UpdateConnectorDots(Vector2 mouthPoint, Vector2 bubblePosition)
        {
            if (connectorDots == null) return;

            Vector2 nearestBubblePoint = new Vector2(
                Mathf.Clamp(mouthPoint.x, bubblePosition.x + 26f, bubblePosition.x + currentSize.x - 26f),
                Mathf.Clamp(mouthPoint.y, bubblePosition.y + 20f, bubblePosition.y + currentSize.y - 20f));

            Vector2 direction = nearestBubblePoint - mouthPoint;
            float length = direction.magnitude;
            if (length < 0.001f) return;

            direction /= length;

            for (int i = 0; i < connectorDots.Length; i++)
            {
                float t = (i + 1f) / (connectorDots.Length + 1f);
                Vector2 worldPoint = Vector2.Lerp(mouthPoint, nearestBubblePoint, t);
                connectorDots[i].anchoredPosition = worldPoint - bubblePosition - direction * 8f;
            }
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null) return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return hasBounds;
        }

        private static Sprite CreateRoundedBubbleSprite()
        {
            const int width = 96;
            const int height = 96;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated_Rounded_Speech_Bubble";

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            Color outline = new Color(0.03f, 0.03f, 0.03f, 0.95f);
            Color fill = new Color(1f, 1f, 1f, 0.94f);

            DrawRoundedRect(texture, 2, 2, width - 4, height - 4, 22, outline);
            DrawRoundedRect(texture, 7, 7, width - 14, height - 14, 17, fill);

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(28f, 28f, 28f, 28f));
        }

        private static Sprite CreateDotSprite()
        {
            const int size = 48;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Generated_Speech_Dot";

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            DrawEllipse(texture, 24, 24, 22, 22, new Color(0.03f, 0.03f, 0.03f, 0.95f));
            DrawEllipse(texture, 24, 24, 17, 17, new Color(1f, 1f, 1f, 0.96f));
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
        }

        private static void DrawRoundedRect(Texture2D texture, int x, int y, int width, int height, int radius, Color color)
        {
            int right = x + width - 1;
            int top = y + height - 1;

            for (int py = y; py <= top; py++)
            {
                for (int px = x; px <= right; px++)
                {
                    int closestX = Mathf.Clamp(px, x + radius, right - radius);
                    int closestY = Mathf.Clamp(py, y + radius, top - radius);
                    int dx = px - closestX;
                    int dy = py - closestY;

                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void DrawEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            if (radiusX <= 0 || radiusY <= 0) return;

            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    if (x < 0 || x >= texture.width || y < 0 || y >= texture.height) continue;

                    float nx = (x - centerX) / (float)radiusX;
                    float ny = (y - centerY) / (float)radiusY;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void SetText(string title, string body)
        {
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            ApplyDynamicBubbleSize(title, body);
        }

        private void ApplyDynamicBubbleSize(string title, string body)
        {
            int titleLength = string.IsNullOrEmpty(title) ? 0 : title.Length;
            int bodyLength = string.IsNullOrEmpty(body) ? 0 : body.Length;
            int totalLength = titleLength + bodyLength;

            float width = Mathf.Lerp(minSize.x, maxSize.x, Mathf.InverseLerp(80f, 230f, totalLength));
            float height = Mathf.Lerp(minSize.y, maxSize.y, Mathf.InverseLerp(95f, 300f, totalLength));
            currentSize = new Vector2(width, height);

            if (panelRect != null)
            {
                panelRect.sizeDelta = currentSize;
            }

            if (contentRect != null)
            {
                contentRect.offsetMin = new Vector2(24f, 18f);
                contentRect.offsetMax = new Vector2(-24f, -18f);
            }

            if (titleText != null)
            {
                titleText.resizeTextMinSize = 9;
                titleText.resizeTextMaxSize = totalLength > 220 ? 11 : 13;
            }

            if (bodyText != null)
            {
                bodyText.resizeTextMinSize = totalLength > 260 ? 7 : 8;
                bodyText.resizeTextMaxSize = totalLength > 220 ? 10 : 11;
            }

            if (bodyLayout != null)
            {
                bodyLayout.preferredHeight = Mathf.Max(62f, currentSize.y - 76f);
            }
        }

        private void Show()
        {
            if (panelObject != null)
            {
                panelObject.SetActive(true);
            }
        }

        private void Hide()
        {
            if (panelObject != null)
            {
                panelObject.SetActive(false);
            }
        }
    }
}
