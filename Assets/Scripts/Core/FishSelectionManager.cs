using System;
using System.Collections.Generic;
using ARFishApp.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishApp.Core
{
    [Serializable]
    public class SelectableFish
    {
        public string id;
        public string displayName;
        public FishData fishData;
        public GameObject modelPrefab;
        public Vector3 modelEulerOffset;
        public Color previewColor = new Color(0.15f, 0.55f, 0.85f);
    }

    public class FishSelectionManager : MonoBehaviour
    {
        private const string FishContainerName = "FishEntityContainer";
        private const string CanvasName = "Canvas_MainUI";
        private const string SelectionPanelName = "FishSelectionPanel";
        private const string RuntimeModelRootName = "SelectedFishModelRoot";

        public static FishSelectionManager Instance { get; private set; }
        public static event Action<SelectableFish> OnFishSelected;

        [Header("Fish Selection")]
        public List<SelectableFish> fishOptions = new List<SelectableFish>();
        [Min(0)] public int defaultFishIndex;

        [Header("Runtime Placement")]
        public Transform modelRoot;
        public Vector3 modelLocalPosition = new Vector3(0f, 0.2f, 0f);
        public Vector3 modelLocalEulerAngles = new Vector3(0f, 180f, 0f);
        public Vector3 modelLocalScale = Vector3.one;
        public bool autoFitModelToView = true;
        [Min(0.1f)] public float targetModelSize = 1.15f;
        public bool hideExistingPlaceholderMesh = true;
        public bool rotateSelectedModel = false;
        public float rotationSpeed = 18f;
        public bool addSwimMotion = true;
        public bool placeInFrontOfEditorCamera = true;
        public float editorPreviewDistance = 2.25f;
        public bool addUnderwaterPreviewEnvironment = true;

        [Header("Selection UI")]
        public bool createSelectionUI = true;
        public Canvas targetCanvas;

        public SelectableFish CurrentFish { get; private set; }
        public GameObject CurrentModelInstance => currentModelInstance;

        private GameObject currentModelInstance;
        private FishEntityController entityController;
        private readonly List<Button> selectionButtons = new List<Button>();

        private void Reset()
        {
            EnsureDefaultFishOptions();
        }

        private void OnValidate()
        {
            EnsureDefaultFishOptions();
            defaultFishIndex = Mathf.Max(0, defaultFishIndex);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureManagerExists()
        {
            GameObject fishContainer = GameObject.Find(FishContainerName);
            if (fishContainer == null) return;

            if (fishContainer.GetComponent<FishSelectionManager>() == null)
            {
                fishContainer.AddComponent<FishSelectionManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            entityController = GetComponent<FishEntityController>();

            EnsureModelRoot();
            EnsureDefaultFishOptions();
            HideOldPlaceholderIfNeeded();
            EnsureUnderwaterPreviewEnvironment();
        }

        private void Start()
        {
#if UNITY_EDITOR
            PlaceInFrontOfCameraForEditorPreview();
#endif

            if (createSelectionUI)
            {
                BuildSelectionUI();
            }

            SelectFish(Mathf.Clamp(defaultFishIndex, 0, fishOptions.Count - 1));
        }

        private void Update()
        {
            if (rotateSelectedModel && currentModelInstance != null)
            {
                currentModelInstance.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        public void SelectFish(int fishIndex)
        {
            if (fishOptions == null || fishOptions.Count == 0) return;
            if (fishIndex < 0 || fishIndex >= fishOptions.Count) return;

            CurrentFish = fishOptions[fishIndex];
            SpawnSelectedFishModel(CurrentFish);

            if (entityController != null)
            {
                entityController.SetFishData(CurrentFish.fishData);
            }

            UpdateButtonStates(fishIndex);
            OnFishSelected?.Invoke(CurrentFish);

            Debug.Log($"[FishSelection] Active fish: {CurrentFish.displayName}");
        }

        private void PlaceInFrontOfCameraForEditorPreview()
        {
            if (!placeInFrontOfEditorCamera) return;

            Camera camera = Camera.main;
            if (camera == null) return;

            transform.position = camera.transform.position + camera.transform.forward * editorPreviewDistance;
            transform.rotation = Quaternion.identity;
        }

        private void SpawnSelectedFishModel(SelectableFish fish)
        {
            if (currentModelInstance != null)
            {
                Destroy(currentModelInstance);
                currentModelInstance = null;
            }

            if (fish != null && fish.modelPrefab != null)
            {
                currentModelInstance = Instantiate(fish.modelPrefab, modelRoot);
            }
            else
            {
                currentModelInstance = BuildProceduralFishPreview(fish);
            }

            currentModelInstance.name = fish != null ? $"{fish.displayName}_Model" : "SelectedFish_Model";
            currentModelInstance.transform.localPosition = modelLocalPosition;
            currentModelInstance.transform.localRotation = Quaternion.Euler(GetModelEulerAngles(fish));
            currentModelInstance.transform.localScale = modelLocalScale;

            if (autoFitModelToView)
            {
                FitCurrentModelToView();
            }

            if (addSwimMotion && currentModelInstance.GetComponent<FishSwimMotion>() == null)
            {
                currentModelInstance.AddComponent<FishSwimMotion>();
            }
        }

        private Vector3 GetModelEulerAngles(SelectableFish fish)
        {
            Vector3 eulerAngles = modelLocalEulerAngles;
            if (fish == null) return eulerAngles;

            if (fish.modelEulerOffset != Vector3.zero)
            {
                return eulerAngles + fish.modelEulerOffset;
            }

            switch (fish.id)
            {
                case "shark":
                    return eulerAngles + new Vector3(0f, -90f, 0f);
                case "trout":
                    return eulerAngles + new Vector3(0f, 180f, 0f);
                default:
                    return eulerAngles;
            }
        }

        private void FitCurrentModelToView()
        {
            Bounds bounds;
            if (!TryGetRendererBounds(currentModelInstance, out bounds)) return;

            float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestSize <= 0.001f) return;

            float scaleMultiplier = targetModelSize / largestSize;
            currentModelInstance.transform.localScale *= scaleMultiplier;

            if (!TryGetRendererBounds(currentModelInstance, out bounds)) return;

            Vector3 targetWorldCenter = modelRoot.TransformPoint(modelLocalPosition);
            Vector3 worldOffset = targetWorldCenter - bounds.center;
            currentModelInstance.transform.position += worldOffset;
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

        private GameObject BuildProceduralFishPreview(SelectableFish fish)
        {
            Color bodyColor = fish != null ? fish.previewColor : new Color(0.15f, 0.55f, 0.85f);
            Color bellyColor = Color.Lerp(Color.white, bodyColor, 0.25f);
            Material bodyMaterial = CreateRuntimeMaterial(bodyColor);
            Material bellyMaterial = CreateRuntimeMaterial(bellyColor);
            Material finMaterial = CreateRuntimeMaterial(Color.Lerp(bodyColor, new Color(0.1f, 0.2f, 0.45f), 0.35f));
            Material eyeMaterial = CreateRuntimeMaterial(Color.black);

            GameObject root = new GameObject("ProceduralFishPreview");
            root.transform.SetParent(modelRoot, false);

            CreatePrimitive(root.transform, "Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(1.25f, 0.42f, 0.36f), bodyMaterial);
            CreatePrimitive(root.transform, "Belly", PrimitiveType.Sphere, new Vector3(0.12f, -0.12f, 0f), new Vector3(0.92f, 0.2f, 0.3f), bellyMaterial);
            CreatePrimitive(root.transform, "Tail", PrimitiveType.Cube, new Vector3(-0.75f, 0f, 0f), new Vector3(0.12f, 0.62f, 0.06f), finMaterial)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            CreatePrimitive(root.transform, "DorsalFin", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.34f, 0.15f, 0.05f), finMaterial)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            CreatePrimitive(root.transform, "LeftFin", PrimitiveType.Cube, new Vector3(0.1f, -0.04f, -0.3f), new Vector3(0.28f, 0.08f, 0.28f), finMaterial)
                .transform.localRotation = Quaternion.Euler(20f, 20f, 20f);
            CreatePrimitive(root.transform, "RightFin", PrimitiveType.Cube, new Vector3(0.1f, -0.04f, 0.3f), new Vector3(0.28f, 0.08f, 0.28f), finMaterial)
                .transform.localRotation = Quaternion.Euler(-20f, -20f, -20f);
            CreatePrimitive(root.transform, "EyeLeft", PrimitiveType.Sphere, new Vector3(0.46f, 0.08f, -0.18f), Vector3.one * 0.06f, eyeMaterial);
            CreatePrimitive(root.transform, "EyeRight", PrimitiveType.Sphere, new Vector3(0.46f, 0.08f, 0.18f), Vector3.one * 0.06f, eyeMaterial);

            return root;
        }

        private void EnsureModelRoot()
        {
            if (modelRoot != null) return;

            Transform existingRoot = transform.Find(RuntimeModelRootName);
            if (existingRoot != null)
            {
                modelRoot = existingRoot;
                return;
            }

            GameObject root = new GameObject(RuntimeModelRootName);
            root.transform.SetParent(transform, false);
            modelRoot = root.transform;
        }

        private void EnsureDefaultFishOptions()
        {
            if (fishOptions != null && fishOptions.Count > 0) return;

            fishOptions = new List<SelectableFish>
            {
                CreateDefaultFish("dolphin", "Yunus", new Color(0.35f, 0.68f, 0.95f)),
                CreateDefaultFish("shark", "Kopek Baligi", new Color(0.38f, 0.46f, 0.52f)),
                CreateDefaultFish("clownfish", "Palyaço Baligi", new Color(1f, 0.48f, 0.12f)),
                CreateDefaultFish("seabass", "Levrek", new Color(0.16f, 0.58f, 0.7f)),
                CreateDefaultFish("tuna", "Orkinos", new Color(0.08f, 0.32f, 0.65f)),
                CreateDefaultFish("salmon", "Somon", new Color(0.95f, 0.42f, 0.35f)),
                CreateDefaultFish("trout", "Alabalik", new Color(0.38f, 0.62f, 0.35f)),
                CreateDefaultFish("ray", "Vatoz", new Color(0.62f, 0.48f, 0.34f))
            };
        }

        private SelectableFish CreateDefaultFish(string id, string displayName, Color color)
        {
            return new SelectableFish
            {
                id = id,
                displayName = displayName,
                previewColor = color
            };
        }

        private void BuildSelectionUI()
        {
            if (targetCanvas == null)
            {
                GameObject canvasObject = GameObject.Find(CanvasName);
                if (canvasObject != null) targetCanvas = canvasObject.GetComponent<Canvas>();
            }

            if (targetCanvas == null || targetCanvas.transform.Find(SelectionPanelName) != null) return;

            GameObject panelObject = new GameObject(SelectionPanelName);
            panelObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.offsetMin = new Vector2(10f, -84f);
            panelRect.offsetMax = new Vector2(-10f, -10f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            HorizontalLayoutGroup layout = panelObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            selectionButtons.Clear();

            for (int i = 0; i < fishOptions.Count; i++)
            {
                int capturedIndex = i;
                Button button = CreateSelectionButton(panelObject.transform, fishOptions[i].displayName, fishOptions[i].previewColor);
                button.onClick.AddListener(() => SelectFish(capturedIndex));
                selectionButtons.Add(button);
            }
        }

        private Button CreateSelectionButton(Transform parent, string label, Color color)
        {
            GameObject buttonObject = new GameObject($"Btn_Fish_{label}");
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = color * 0.75f;
            button.colors = colors;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text text = labelObject.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return button;
        }

        private void UpdateButtonStates(int activeIndex)
        {
            for (int i = 0; i < selectionButtons.Count; i++)
            {
                Image image = selectionButtons[i].GetComponent<Image>();
                if (image == null) continue;

                Color baseColor = fishOptions[i].previewColor;
                image.color = i == activeIndex ? Color.Lerp(baseColor, Color.white, 0.35f) : baseColor;
            }
        }

        private void HideOldPlaceholderIfNeeded()
        {
            if (!hideExistingPlaceholderMesh) return;

            Transform placeholder = transform.Find("FishMesh_Placeholder");
            if (placeholder == null) return;

            Renderer renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private void EnsureUnderwaterPreviewEnvironment()
        {
            if (!addUnderwaterPreviewEnvironment) return;
            if (GetComponent<UnderwaterPreviewEnvironment>() != null) return;

            gameObject.AddComponent<UnderwaterPreviewEnvironment>();
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Diffuse"));
            material.color = color;
            return material;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            return go;
        }
    }
}
