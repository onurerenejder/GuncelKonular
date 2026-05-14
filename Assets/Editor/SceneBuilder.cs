using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ARFishApp.Core;
using ARFishApp.AR;
using ARFishApp.UI;
using ARFishApp.Modules;
using ARFishApp.Interaction;
using ARFishApp.Data;

namespace ARFishApp.Editor
{
    public static class SceneBuilder
    {
        [MenuItem("Tools/ARFishApp/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 1. AR SESSION ──────────────────────────────────────────────
            var arSessionGO = new GameObject("AR Session");
            arSessionGO.AddComponent<ARSession>();

            // ── 2. XR ORIGIN ───────────────────────────────────────────────
            var xrOriginGO = new GameObject("XR Origin");
            var xrOrigin = xrOriginGO.AddComponent<XROrigin>();

            var cameraOffsetGO = new GameObject("Camera Offset");
            cameraOffsetGO.transform.SetParent(xrOriginGO.transform);

            var cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(cameraOffsetGO.transform);
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.black;
            cameraGO.AddComponent<ARCameraManager>();
            cameraGO.AddComponent<ARCameraBackground>();
            cameraGO.AddComponent<AudioListener>();

            xrOrigin.Camera = cam;
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

            // ARMarkerHandler has [RequireComponent(ARTrackedImageManager)] — auto-added
            var arMarkerHandler = xrOriginGO.AddComponent<ARMarkerHandler>();
            // ⚠️ xrOriginGO → ARTrackedImageManager → referenceLibrary: manuel atama gerekiyor

            // ── 3. SYSTEM STATE MANAGER ────────────────────────────────────
            var stateManagerGO = new GameObject("SystemStateManager");
            stateManagerGO.AddComponent<SystemStateManager>();

            // ── 4. FISH ENTITY CONTAINER ───────────────────────────────────
            var fishContainerGO = new GameObject("FishEntityContainer");

            // Balık gövdesi — placeholder kapsül
            var fishMeshGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fishMeshGO.name = "FishMesh_Placeholder";
            fishMeshGO.transform.SetParent(fishContainerGO.transform);
            fishMeshGO.transform.localPosition = Vector3.zero;
            fishMeshGO.transform.localScale = new Vector3(0.5f, 0.3f, 1f);
            var fishRenderer = fishMeshGO.GetComponent<Renderer>();

            // İskelet — placeholder (başlangıçta gizli)
            var skeletonGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            skeletonGO.name = "Skeleton_Placeholder";
            skeletonGO.transform.SetParent(fishContainerGO.transform);
            skeletonGO.transform.localPosition = Vector3.zero;
            skeletonGO.transform.localScale = new Vector3(0.45f, 0.28f, 0.95f);
            skeletonGO.SetActive(false);

            // FishEntityController + FishData ScriptableObject
            var fishController = fishContainerGO.AddComponent<FishEntityController>();
            fishController.fishDataConfig = GetOrCreateFishData();

            // ── 5. MODÜLLER ────────────────────────────────────────────────
            var anatomyGO = new GameObject("AnatomyModule");
            anatomyGO.transform.SetParent(fishContainerGO.transform);
            var anatomyModule = anatomyGO.AddComponent<AnatomyModule>();
            anatomyModule.skinRenderer = fishRenderer;
            anatomyModule.skeletonModel = skeletonGO;
            if (anatomyModule.biologicalSystems == null)
                anatomyModule.biologicalSystems = new System.Collections.Generic.List<OrganSystem>();
            anatomyModule.biologicalSystems.Add(new OrganSystem
            {
                systemName = "Solungaçlar",
                organRenderer = fishRenderer,
                isPulsating = true,
                pulseRate = 0.8f,
                pulseMagnitude = 0.04f
            });

            // HabitatModule DynamicWaterSurface gerektirir ([ExecuteAlways]) — ayrı GO'ya alıyoruz
            var habitatGO = new GameObject("HabitatModule");
            habitatGO.transform.SetParent(fishContainerGO.transform);
            try { habitatGO.AddComponent<HabitatModule>(); }
            catch (System.Exception e) { Debug.LogWarning("[SceneBuilder] HabitatModule eklenemedi: " + e.Message); }

            var feedingGO = new GameObject("FeedingModule");
            feedingGO.transform.SetParent(fishContainerGO.transform);
            try { feedingGO.AddComponent<FeedingModule>(); }
            catch (System.Exception e) { Debug.LogWarning("[SceneBuilder] FeedingModule eklenemedi: " + e.Message); }

            var predatorPreyGO = new GameObject("PredatorPreyModule");
            predatorPreyGO.transform.SetParent(fishContainerGO.transform);
            try
            {
                var ppModule = predatorPreyGO.AddComponent<PredatorPreyModule>();
                ppModule.preySkinRenderer = fishRenderer;
            }
            catch (System.Exception e) { Debug.LogWarning("[SceneBuilder] PredatorPreyModule eklenemedi: " + e.Message); }

            var quizGO = new GameObject("QuizModule");
            quizGO.transform.SetParent(fishContainerGO.transform);
            try { quizGO.AddComponent<QuizModule>(); }
            catch (System.Exception e) { Debug.LogWarning("[SceneBuilder] QuizModule eklenemedi: " + e.Message); }

            // ── 6. HOTSPOT NODE'LAR ────────────────────────────────────────
            CreateHotspot(fishContainerGO, "Gills",      "Solungaçlar, suda çözünmüş oksijeni kana aktarır.",          new Vector3(-0.25f,  0.05f,  0.35f));
            CreateHotspot(fishContainerGO, "Heart",      "Kalp, kanı tüm vücuda pompalayan kas organdır.",              new Vector3( 0.00f,  0.00f,  0.10f));
            CreateHotspot(fishContainerGO, "Dorsal Fin", "Sırt yüzgeci balığın su içinde dengede kalmasını sağlar.",   new Vector3( 0.00f,  0.40f,  0.00f));

            // Editörde görünür, cihazda ARMarkerHandler gizler
            arMarkerHandler.fishEntityContainer = fishContainerGO;

            // ── 7. UI CANVAS ───────────────────────────────────────────────
            BuildCanvas();

            // ── 8. EVENT SYSTEM ────────────────────────────────────────────
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();

            // ── 9. DIRECTIONAL LIGHT ───────────────────────────────────────
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ── 10. SAHNEYI KAYDET ─────────────────────────────────────────
            const string scenePath = "Assets/Scenes/MainScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log("[SceneBuilder] Sahne oluşturuldu: " + scenePath);

            EditorUtility.DisplayDialog(
                "ARFishApp — Sahne Hazır!",
                "Assets/Scenes/MainScene.unity oluşturuldu.\n\n" +
                "Sonraki manuel adımlar:\n" +
                "1. XR Origin → ARTrackedImageManager → Reference Library: bir XRReferenceImageLibrary asset'i ata\n" +
                "2. Edit → Project Settings → XR Plug-in Management → iOS/Android: ARKit / ARCore etkinleştir\n" +
                "3. Play Mode'da test etmek için Window → XR → AR Foundation → XR Simulation aktif et\n\n" +
                "Modüller placeholder mesh ile çalışır. 3D model geldiğinde FishMesh_Placeholder ile değiştir.",
                "Tamam"
            );
        }

        private static void CreateHotspot(GameObject parent, string organName, string description, Vector3 localPos)
        {
            var hotspotGO = new GameObject($"Hotspot_{organName.Replace(" ", "")}");
            hotspotGO.transform.SetParent(parent.transform);
            hotspotGO.transform.localPosition = localPos;

            // Görsel — küçük sarı küre
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(hotspotGO.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.06f;

            // UI metin konumu (line renderer bu noktaya çizgi çeker)
            var uiAnchor = new GameObject("UIAnchor");
            uiAnchor.transform.SetParent(hotspotGO.transform);
            uiAnchor.transform.localPosition = new Vector3(0.35f, 0.2f, 0f);

            // HotspotNode'un [RequireComponent(LineRenderer)] var — otomatik eklenir
            var hotspot = hotspotGO.AddComponent<HotspotNode>();
            hotspot.organName = organName;
            hotspot.infoDescription = description;
            hotspot.uiPanelLocation = uiAnchor.transform;
        }

        private static void BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas_MainUI");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            var uiManager = canvasGO.AddComponent<MainUIManager>();

            // Alt buton paneli
            var panelGO = new GameObject("ButtonPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot    = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(10f, 10f);
            panelRect.offsetMax = new Vector2(-10f, 110f);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.55f);

            var hlg = panelGO.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 6f;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;

            // Buton tanımları: label, MainUIManager metot adı, renk
            var buttons = new (string Label, string Method, Color Color)[]
            {
                ("Anatomi",  "OnAnatomyButtonClicked",        new Color(0.20f, 0.55f, 1.00f)),
                ("Habitat",  "OnHabitatButtonClicked",        new Color(0.15f, 0.75f, 0.45f)),
                ("Beslenme", "OnFeedingButtonClicked",        new Color(1.00f, 0.55f, 0.15f)),
                ("Türler",   "OnInterspeciesButtonClicked",   new Color(0.65f, 0.25f, 0.85f)),
                ("Av/Avcı", "OnPredatorPreyButtonClicked",   new Color(0.85f, 0.20f, 0.20f)),
                ("Quiz",     "OnQuizButtonClicked",           new Color(1.00f, 0.80f, 0.10f)),
            };

            foreach (var (label, methodName, color) in buttons)
            {
                var btnGO = new GameObject($"Btn_{label}");
                btnGO.transform.SetParent(panelGO.transform, false);

                var img = btnGO.AddComponent<Image>();
                img.color = color;

                var btn = btnGO.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = Color.white;
                colors.pressedColor = color * 0.7f;
                btn.colors = colors;

                var methodInfo = typeof(MainUIManager).GetMethod(methodName);
                if (methodInfo != null)
                {
                    var action = System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction), uiManager, methodInfo
                    ) as UnityEngine.Events.UnityAction;
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
                }

                // Etiket
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var text = labelGO.AddComponent<Text>();
                text.text = label;
                text.fontSize = 16;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }
        }

        private static FishData GetOrCreateFishData()
        {
            const string path = "Assets/Data/BalikData_Demo.asset";

            var existing = AssetDatabase.LoadAssetAtPath<FishData>(path);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<FishData>();
            data.FishName            = "Levrek";
            data.ScientificName      = "Dicentrarchus labrax";
            data.GeneralDescription  = "Akdeniz'in en yaygın balıklarından biri. Hem tuzlu hem tatlı sularda yaşayabilir.";
            data.AnatomyDescription  = "Kemikli yapıya sahiptir. Solungaçları suda çözünmüş oksijeni kana aktarır.";
            data.HabitatType         = "Coastal Marine";
            data.EnvironmentalLightColor = new Color(0.2f, 0.5f, 0.85f);
            data.DietDescription     = "Küçük balıklar, karides ve yumuşakçalarla beslenir.";
            data.FoodChain           = new[] { "Fitoplankton", "Zooplankton", "Küçük Balık", "Levrek" };
            data.Predators           = new[] { "Köpek Balığı", "Yunus", "Büyük Orkinos" };

            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            return data;
        }
    }
}
