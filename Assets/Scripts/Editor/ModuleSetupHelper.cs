using UnityEngine;
using UnityEditor;
using ARFishApp.Core;
using ARFishApp.Modules;

namespace ARFishApp.Editor
{
    /// <summary>
    /// Automatically connects placeholder prefabs to module scripts.
    /// Menu: Tools → ARFish → Auto-Connect Module References
    /// </summary>
    public class ModuleSetupHelper : EditorWindow
    {
        private GameObject fishObject;
        
        [MenuItem("Tools/ARFish/Auto-Connect Module References")]
        public static void ShowWindow()
        {
            GetWindow<ModuleSetupHelper>("Module Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("ARFish Module Reference Connector", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu araç, oluşturduğunuz placeholder prefab'ları otomatik olarak modül scriptlerine bağlar.\n\n" +
                "1. Sahneye ana balık GameObject'ini yerleştirin\n" +
                "2. Tüm modül scriptlerini bu GameObject'e component olarak ekleyin\n" +
                "3. Aşağıdaki alana bu GameObject'i sürükleyin\n" +
                "4. 'Referansları Otomatik Bağla' butonuna tıklayın",
                MessageType.Info);

            GUILayout.Space(10);

            fishObject = (GameObject)EditorGUILayout.ObjectField(
                "Ana Balık GameObject", 
                fishObject, 
                typeof(GameObject), 
                true);

            GUILayout.Space(20);

            GUI.enabled = fishObject != null;
            
            if (GUILayout.Button("Referansları Otomatik Bağla", GUILayout.Height(40)))
            {
                AutoConnectReferences();
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            if (GUILayout.Button("Eksik Modülleri Ekle", GUILayout.Height(30)))
            {
                AddMissingModules();
            }
        }

        private void AutoConnectReferences()
        {
            if (fishObject == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen bir GameObject seçin!", "Tamam");
                return;
            }

            int connectedCount = 0;

            // AnatomyModule
            var anatomyModule = fishObject.GetComponent<AnatomyModule>();
            if (anatomyModule != null)
            {
                connectedCount += ConnectAnatomyModule(anatomyModule);
            }

            // FeedingModule
            var feedingModule = fishObject.GetComponent<FeedingModule>();
            if (feedingModule != null)
            {
                connectedCount += ConnectFeedingModule(feedingModule);
            }

            // QuizModule
            var quizModule = fishObject.GetComponent<QuizModule>();
            if (quizModule != null)
            {
                connectedCount += ConnectQuizModule(quizModule);
            }

            // InterspeciesRelationsModule
            var interspeciesModule = fishObject.GetComponent<InterspeciesRelationsModule>();
            if (interspeciesModule != null)
            {
                connectedCount += ConnectInterspeciesModule(interspeciesModule);
            }

            // PredatorPreyModule
            var predatorPreyModule = fishObject.GetComponent<PredatorPreyModule>();
            if (predatorPreyModule != null)
            {
                connectedCount += ConnectPredatorPreyModule(predatorPreyModule);
            }

            // PortalModule
            var portalModule = fishObject.GetComponent<PortalModule>();
            if (portalModule != null)
            {
                connectedCount += ConnectPortalModule(portalModule);
            }

            EditorUtility.SetDirty(fishObject);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Tamamlandı", 
                $"{connectedCount} referans başarıyla bağlandı!\n\n" +
                "Inspector'da modülleri kontrol edin.", 
                "Tamam");
        }

        private int ConnectAnatomyModule(AnatomyModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Skeleton model
            GameObject skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/FishSkeleton.prefab");
            if (skeleton != null)
            {
                so.FindProperty("skeletonModel").objectReferenceValue = skeleton;
                count++;
                Debug.Log("✅ AnatomyModule: Skeleton bağlandı");
            }

            // Skin renderer (ana balığın renderer'ı)
            Renderer skinRenderer = fishObject.GetComponentInChildren<Renderer>();
            if (skinRenderer != null)
            {
                so.FindProperty("skinRenderer").objectReferenceValue = skinRenderer;
                count++;
                Debug.Log("✅ AnatomyModule: Skin renderer bağlandı");
            }

            so.ApplyModifiedProperties();
            return count;
        }

        private int ConnectFeedingModule(FeedingModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Prey prefab
            GameObject prey = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Food/SmallPreyFish.prefab");
            if (prey != null)
            {
                so.FindProperty("meatPreyPrefab").objectReferenceValue = prey;
                count++;
                Debug.Log("✅ FeedingModule: Prey prefab bağlandı");
            }

            // Vegetation prefab
            GameObject vegetation = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Food/SeaweedFood.prefab");
            if (vegetation != null)
            {
                so.FindProperty("vegetationPrefab").objectReferenceValue = vegetation;
                count++;
                Debug.Log("✅ FeedingModule: Vegetation prefab bağlandı");
            }

            // Blood particle
            GameObject blood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/BloodMuzzle.prefab");
            if (blood != null)
            {
                ParticleSystem ps = blood.GetComponent<ParticleSystem>();
                so.FindProperty("hitBloodMuzzle").objectReferenceValue = ps;
                count++;
                Debug.Log("✅ FeedingModule: Blood particle bağlandı");
            }

            // Algae particle
            GameObject algae = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/AlgaeMuzzle.prefab");
            if (algae != null)
            {
                ParticleSystem ps = algae.GetComponent<ParticleSystem>();
                so.FindProperty("hitAlgaeMuzzle").objectReferenceValue = ps;
                count++;
                Debug.Log("✅ FeedingModule: Algae particle bağlandı");
            }

            // Head bone (ana balığın child'ı olmalı)
            Transform headBone = fishObject.transform.Find("Head");
            if (headBone != null)
            {
                so.FindProperty("headBone").objectReferenceValue = headBone;
                count++;
                Debug.Log("✅ FeedingModule: Head bone bağlandı");
            }

            // Mouth socket (head bone veya body)
            Transform mouthSocket = headBone != null ? headBone : fishObject.transform;
            so.FindProperty("mouthSocket").objectReferenceValue = mouthSocket;
            count++;

            so.ApplyModifiedProperties();
            return count;
        }

        private int ConnectQuizModule(QuizModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Success confetti
            GameObject confetti = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/SuccessConfetti.prefab");
            if (confetti != null)
            {
                so.FindProperty("successConfettiParticle").objectReferenceValue = confetti;
                count++;
                Debug.Log("✅ QuizModule: Confetti particle bağlandı");
            }

            // Error buzzer
            GameObject buzzer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/ErrorBuzzer.prefab");
            if (buzzer != null)
            {
                ParticleSystem ps = buzzer.GetComponent<ParticleSystem>();
                so.FindProperty("errorBuzzerEmission").objectReferenceValue = ps;
                count++;
                Debug.Log("✅ QuizModule: Error buzzer bağlandı");
            }

            so.ApplyModifiedProperties();
            return count;
        }

        private int ConnectInterspeciesModule(InterspeciesRelationsModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Schooling fish
            GameObject schoolFish = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/SchoolingFish.prefab");
            if (schoolFish != null)
            {
                so.FindProperty("schoolingFishPrefab").objectReferenceValue = schoolFish;
                count++;
                Debug.Log("✅ InterspeciesModule: School fish bağlandı");
            }

            // Player camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                so.FindProperty("playerCamera").objectReferenceValue = mainCam;
                count++;
                Debug.Log("✅ InterspeciesModule: Camera bağlandı");
            }

            // Symbiotic attach point (ana balığın yanı)
            Transform attachPoint = fishObject.transform;
            so.FindProperty("symbioticAttachPoint").objectReferenceValue = attachPoint;
            count++;

            so.ApplyModifiedProperties();
            return count;
        }

        private int ConnectPredatorPreyModule(PredatorPreyModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Apex predator
            GameObject predator = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/ApexPredator.prefab");
            if (predator != null)
            {
                so.FindProperty("apexPredatorPrefab").objectReferenceValue = predator;
                count++;
                Debug.Log("✅ PredatorPreyModule: Predator bağlandı");
            }

            // Ink cloud
            GameObject ink = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/InkCloud.prefab");
            if (ink != null)
            {
                so.FindProperty("inkOpticJammerParticle").objectReferenceValue = ink;
                count++;
                Debug.Log("✅ PredatorPreyModule: Ink cloud bağlandı");
            }

            // Prey skin renderer
            Renderer skinRenderer = fishObject.GetComponentInChildren<Renderer>();
            if (skinRenderer != null)
            {
                so.FindProperty("preySkinRenderer").objectReferenceValue = skinRenderer;
                count++;
                Debug.Log("✅ PredatorPreyModule: Skin renderer bağlandı");
            }

            so.ApplyModifiedProperties();
            return count;
        }

        private int ConnectPortalModule(PortalModule module)
        {
            int count = 0;
            var so = new SerializedObject(module);

            // Portal doorway - Sahneye yerleştirilmeli
            GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/UnderwaterPortal.prefab");
            if (portalPrefab != null)
            {
                // Sahneye instantiate et
                GameObject portalInstance = PrefabUtility.InstantiatePrefab(portalPrefab) as GameObject;
                if (portalInstance != null)
                {
                    portalInstance.transform.position = fishObject.transform.position + Vector3.forward * 3f;
                    so.FindProperty("portalDoorway").objectReferenceValue = portalInstance.transform;
                    count++;
                    Debug.Log("✅ PortalModule: Portal doorway sahneye eklendi");
                }
            }

            // AR Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                so.FindProperty("arCamera").objectReferenceValue = mainCam;
                count++;
                Debug.Log("✅ PortalModule: Camera bağlandı");
            }

            so.ApplyModifiedProperties();
            return count;
        }

        private void AddMissingModules()
        {
            if (fishObject == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen bir GameObject seçin!", "Tamam");
                return;
            }

            int addedCount = 0;

            if (fishObject.GetComponent<FishEntityController>() == null)
            {
                fishObject.AddComponent<FishEntityController>();
                addedCount++;
            }

            if (fishObject.GetComponent<AnatomyModule>() == null)
            {
                fishObject.AddComponent<AnatomyModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<FeedingModule>() == null)
            {
                fishObject.AddComponent<FeedingModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<QuizModule>() == null)
            {
                fishObject.AddComponent<QuizModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<HabitatModule>() == null)
            {
                fishObject.AddComponent<HabitatModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<InterspeciesRelationsModule>() == null)
            {
                fishObject.AddComponent<InterspeciesRelationsModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<PredatorPreyModule>() == null)
            {
                fishObject.AddComponent<PredatorPreyModule>();
                addedCount++;
            }

            if (fishObject.GetComponent<PortalModule>() == null)
            {
                fishObject.AddComponent<PortalModule>();
                addedCount++;
            }

            EditorUtility.DisplayDialog("Tamamlandı", 
                $"{addedCount} modül eklendi!\n\n" +
                "Şimdi 'Referansları Otomatik Bağla' butonuna tıklayın.", 
                "Tamam");
        }
    }
}
