using UnityEngine;
using UnityEditor;
using System.IO;

namespace ARFishApp.Editor
{
    /// <summary>
    /// Unity Editor tool to quickly generate placeholder assets for ARFish project.
    /// Menu: Tools → ARFish → Setup Placeholder Assets
    /// </summary>
    public class AssetSetupHelper : EditorWindow
    {
        private bool createMainFish = true;
        private bool createSkeleton = true;
        private bool createOrgans = true;
        private bool createFood = true;
        private bool createSchoolFish = true;
        private bool createPredator = true;
        private bool createPortal = true;
        private bool createParticles = true;

        [MenuItem("Tools/ARFish/Setup Placeholder Assets")]
        public static void ShowWindow()
        {
            GetWindow<AssetSetupHelper>("ARFish Asset Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("ARFish Placeholder Asset Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu araç, projeyi hızlıca test etmek için Unity primitive'lerinden placeholder asset'ler oluşturur. " +
                "Gerçek modelleri Asset Store'dan indirdikten sonra bunları değiştirebilirsiniz.",
                MessageType.Info);

            GUILayout.Space(10);

            createMainFish = EditorGUILayout.Toggle("Ana Balık Modeli", createMainFish);
            createSkeleton = EditorGUILayout.Toggle("İskelet Modeli", createSkeleton);
            createOrgans = EditorGUILayout.Toggle("Organ Sistemleri", createOrgans);
            createFood = EditorGUILayout.Toggle("Yiyecek Modelleri", createFood);
            createSchoolFish = EditorGUILayout.Toggle("Sürü Balığı", createSchoolFish);
            createPredator = EditorGUILayout.Toggle("Avcı Balık", createPredator);
            createPortal = EditorGUILayout.Toggle("Portal Objesi", createPortal);
            createParticles = EditorGUILayout.Toggle("Particle Sistemleri", createParticles);

            GUILayout.Space(20);

            if (GUILayout.Button("Tüm Placeholder'ları Oluştur", GUILayout.Height(40)))
            {
                CreatePlaceholderAssets();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Prefab Klasörlerini Oluştur", GUILayout.Height(30)))
            {
                CreateFolderStructure();
            }
        }

        private void CreatePlaceholderAssets()
        {
            CreateFolderStructure();

            if (createMainFish) CreateMainFishPlaceholder();
            if (createSkeleton) CreateSkeletonPlaceholder();
            if (createOrgans) CreateOrganPlaceholders();
            if (createFood) CreateFoodPlaceholders();
            if (createSchoolFish) CreateSchoolFishPlaceholder();
            if (createPredator) CreatePredatorPlaceholder();
            if (createPortal) CreatePortalPlaceholder();
            if (createParticles) CreateParticlePlaceholders();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Tamamlandı", 
                "Placeholder asset'ler başarıyla oluşturuldu!\n\n" +
                "Prefab'lar Assets/Prefabs/ klasöründe.\n" +
                "Şimdi FishEntityController'a bu prefab'ları bağlayabilirsiniz.", 
                "Tamam");
        }

        private void CreateFolderStructure()
        {
            CreateFolder("Assets/Prefabs");
            CreateFolder("Assets/Prefabs/Fish");
            CreateFolder("Assets/Prefabs/Organs");
            CreateFolder("Assets/Prefabs/Food");
            CreateFolder("Assets/Prefabs/Environment");
            CreateFolder("Assets/Prefabs/Particles");
            CreateFolder("Assets/Materials");
            CreateFolder("Assets/Materials/Fish");
            CreateFolder("Assets/Materials/Organs");
        }

        private void CreateFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private void CreateMainFishPlaceholder()
        {
            // Ana balık gövdesi
            GameObject mainFish = new GameObject("MainFish_Placeholder");
            
            // Gövde
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(mainFish.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
            
            // Baş
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(mainFish.transform);
            head.transform.localPosition = new Vector3(0.5f, 0, 0);
            head.transform.localScale = new Vector3(0.35f, 0.3f, 0.3f);
            
            // Kuyruk
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(mainFish.transform);
            tail.transform.localPosition = new Vector3(-0.6f, 0, 0);
            tail.transform.localScale = new Vector3(0.1f, 0.4f, 0.3f);

            // Material
            Material fishMat = new Material(Shader.Find("Standard"));
            fishMat.color = new Color(0.2f, 0.6f, 0.9f); // Mavi
            AssetDatabase.CreateAsset(fishMat, "Assets/Materials/Fish/MainFish_Mat.mat");
            
            body.GetComponent<Renderer>().sharedMaterial = fishMat;
            head.GetComponent<Renderer>().sharedMaterial = fishMat;
            tail.GetComponent<Renderer>().sharedMaterial = fishMat;

            // Animator ekle
            Animator animator = mainFish.AddComponent<Animator>();
            
            // Prefab olarak kaydet
            PrefabUtility.SaveAsPrefabAsset(mainFish, "Assets/Prefabs/Fish/MainFish.prefab");
            DestroyImmediate(mainFish);
            
            Debug.Log("✅ Ana Balık placeholder'ı oluşturuldu: Assets/Prefabs/Fish/MainFish.prefab");
        }

        private void CreateSkeletonPlaceholder()
        {
            GameObject skeleton = new GameObject("FishSkeleton_Placeholder");
            
            // Omurga
            GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            spine.name = "Spine";
            spine.transform.SetParent(skeleton.transform);
            spine.transform.localRotation = Quaternion.Euler(0, 0, 90);
            spine.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
            
            // Kaburga kemikleri
            for (int i = 0; i < 5; i++)
            {
                GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rib.name = $"Rib_{i}";
                rib.transform.SetParent(skeleton.transform);
                rib.transform.localPosition = new Vector3(-0.3f + i * 0.15f, 0, 0);
                rib.transform.localRotation = Quaternion.Euler(0, 0, 0);
                rib.transform.localScale = new Vector3(0.02f, 0.15f, 0.02f);
            }

            // Material - Beyaz emissive
            Material boneMat = new Material(Shader.Find("Standard"));
            boneMat.color = Color.white;
            boneMat.EnableKeyword("_EMISSION");
            boneMat.SetColor("_EmissionColor", new Color(0.5f, 0.8f, 1f) * 0.3f);
            AssetDatabase.CreateAsset(boneMat, "Assets/Materials/Fish/Skeleton_Mat.mat");
            
            foreach (Renderer r in skeleton.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = boneMat;
            }

            skeleton.SetActive(false); // Başlangıçta kapalı
            PrefabUtility.SaveAsPrefabAsset(skeleton, "Assets/Prefabs/Fish/FishSkeleton.prefab");
            DestroyImmediate(skeleton);
            
            Debug.Log("✅ İskelet placeholder'ı oluşturuldu: Assets/Prefabs/Fish/FishSkeleton.prefab");
        }

        private void CreateOrganPlaceholders()
        {
            // Kalp
            CreateOrgan("Heart", new Color(0.9f, 0.1f, 0.1f), new Vector3(0.1f, 0.1f, 0.1f), "Kanı pompalar");
            
            // Solungaçlar
            CreateOrgan("Gills", new Color(1f, 0.5f, 0.5f), new Vector3(0.15f, 0.08f, 0.05f), "Oksijen alır");
            
            // Dorsal Fin
            CreateOrgan("DorsalFin", new Color(0.6f, 0.6f, 0.6f), new Vector3(0.2f, 0.3f, 0.05f), "Denge sağlar");
            
            // Mide
            CreateOrgan("Stomach", new Color(0.9f, 0.7f, 0.3f), new Vector3(0.12f, 0.15f, 0.1f), "Sindirim yapar");
            
            Debug.Log("✅ Organ placeholder'ları oluşturuldu: Assets/Prefabs/Organs/");
        }

        private void CreateOrgan(string organName, Color color, Vector3 scale, string description)
        {
            GameObject organ = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            organ.name = organName;
            organ.transform.localScale = scale;

            Material organMat = new Material(Shader.Find("Standard"));
            organMat.color = color;
            organMat.EnableKeyword("_EMISSION");
            organMat.SetColor("_EmissionColor", color * 0.2f);
            AssetDatabase.CreateAsset(organMat, $"Assets/Materials/Organs/{organName}_Mat.mat");
            
            organ.GetComponent<Renderer>().sharedMaterial = organMat;

            // HotspotNode component ekle
            var hotspot = organ.AddComponent<ARFishApp.Interaction.HotspotNode>();
            // Reflection ile private field'lara erişim (Unity serialization için)
            var serializedObject = new SerializedObject(hotspot);
            serializedObject.FindProperty("organName").stringValue = organName;
            serializedObject.FindProperty("infoDescription").stringValue = description;
            serializedObject.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(organ, $"Assets/Prefabs/Organs/{organName}.prefab");
            DestroyImmediate(organ);
        }

        private void CreateFoodPlaceholders()
        {
            // Et/Av balığı
            GameObject prey = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prey.name = "SmallPreyFish";
            prey.transform.localScale = new Vector3(0.1f, 0.15f, 0.1f);
            prey.transform.localRotation = Quaternion.Euler(0, 0, 90);
            
            Material preyMat = new Material(Shader.Find("Standard"));
            preyMat.color = new Color(0.8f, 0.4f, 0.2f);
            AssetDatabase.CreateAsset(preyMat, "Assets/Materials/PreyFish_Mat.mat");
            prey.GetComponent<Renderer>().sharedMaterial = preyMat;
            
            PrefabUtility.SaveAsPrefabAsset(prey, "Assets/Prefabs/Food/SmallPreyFish.prefab");
            DestroyImmediate(prey);

            // Bitki/Alg
            GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plant.name = "SeaweedFood";
            plant.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);
            
            Material plantMat = new Material(Shader.Find("Standard"));
            plantMat.color = new Color(0.2f, 0.7f, 0.3f);
            AssetDatabase.CreateAsset(plantMat, "Assets/Materials/Seaweed_Mat.mat");
            plant.GetComponent<Renderer>().sharedMaterial = plantMat;
            
            PrefabUtility.SaveAsPrefabAsset(plant, "Assets/Prefabs/Food/SeaweedFood.prefab");
            DestroyImmediate(plant);
            
            Debug.Log("✅ Yiyecek placeholder'ları oluşturuldu: Assets/Prefabs/Food/");
        }

        private void CreateSchoolFishPlaceholder()
        {
            GameObject schoolFish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            schoolFish.name = "SchoolingFish";
            schoolFish.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
            schoolFish.transform.localRotation = Quaternion.Euler(0, 0, 90);
            
            Material schoolMat = new Material(Shader.Find("Standard"));
            schoolMat.color = new Color(0.7f, 0.7f, 0.9f);
            AssetDatabase.CreateAsset(schoolMat, "Assets/Materials/SchoolFish_Mat.mat");
            schoolFish.GetComponent<Renderer>().sharedMaterial = schoolMat;
            
            // Collider kaldır (performans için)
            DestroyImmediate(schoolFish.GetComponent<Collider>());
            
            PrefabUtility.SaveAsPrefabAsset(schoolFish, "Assets/Prefabs/Fish/SchoolingFish.prefab");
            DestroyImmediate(schoolFish);
            
            Debug.Log("✅ Sürü balığı placeholder'ı oluşturuldu: Assets/Prefabs/Fish/SchoolingFish.prefab");
        }

        private void CreatePredatorPlaceholder()
        {
            GameObject predator = new GameObject("ApexPredator_Placeholder");
            
            // Gövde - Daha büyük ve tehditkar
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(predator.transform);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
            
            // Baş - Sivri
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(predator.transform);
            head.transform.localPosition = new Vector3(1.2f, 0, 0);
            head.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
            
            Material predatorMat = new Material(Shader.Find("Standard"));
            predatorMat.color = new Color(0.2f, 0.2f, 0.3f); // Koyu gri
            AssetDatabase.CreateAsset(predatorMat, "Assets/Materials/Predator_Mat.mat");
            
            body.GetComponent<Renderer>().sharedMaterial = predatorMat;
            head.GetComponent<Renderer>().sharedMaterial = predatorMat;
            
            PrefabUtility.SaveAsPrefabAsset(predator, "Assets/Prefabs/Fish/ApexPredator.prefab");
            DestroyImmediate(predator);
            
            Debug.Log("✅ Avcı balık placeholder'ı oluşturuldu: Assets/Prefabs/Fish/ApexPredator.prefab");
        }

        private void CreatePortalPlaceholder()
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "UnderwaterPortal";
            portal.transform.localScale = new Vector3(2f, 0.1f, 2f);
            
            Material portalMat = new Material(Shader.Find("Standard"));
            portalMat.color = new Color(0f, 0.8f, 1f, 0.5f);
            portalMat.EnableKeyword("_EMISSION");
            portalMat.SetColor("_EmissionColor", new Color(0f, 1f, 1f) * 2f);
            
            // Transparency
            portalMat.SetFloat("_Mode", 3); // Transparent mode
            portalMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            portalMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            portalMat.SetInt("_ZWrite", 0);
            portalMat.DisableKeyword("_ALPHATEST_ON");
            portalMat.EnableKeyword("_ALPHABLEND_ON");
            portalMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            portalMat.renderQueue = 3000;
            
            AssetDatabase.CreateAsset(portalMat, "Assets/Materials/Portal_Mat.mat");
            portal.GetComponent<Renderer>().sharedMaterial = portalMat;
            
            PrefabUtility.SaveAsPrefabAsset(portal, "Assets/Prefabs/Environment/UnderwaterPortal.prefab");
            DestroyImmediate(portal);
            
            Debug.Log("✅ Portal placeholder'ı oluşturuldu: Assets/Prefabs/Environment/UnderwaterPortal.prefab");
        }

        private void CreateParticlePlaceholders()
        {
            // Konfeti (Quiz - Doğru cevap)
            CreateParticleSystem("SuccessConfetti", new Color(1f, 0.8f, 0f), 50, 1f, 5f);
            
            // Hata efekti (Quiz - Yanlış cevap)
            CreateParticleSystem("ErrorBuzzer", new Color(1f, 0.2f, 0.2f), 15, 0.3f, 2f);
            
            // Kan efekti (Feeding - Carnivore)
            CreateParticleSystem("BloodMuzzle", new Color(0.8f, 0f, 0f), 25, 0.5f, 3f);
            
            // Alg parçaları (Feeding - Herbivore)
            CreateParticleSystem("AlgaeMuzzle", new Color(0.2f, 0.8f, 0.3f), 20, 0.5f, 2f);
            
            // Mürekkep bulutu (Predator Prey)
            CreateParticleSystem("InkCloud", new Color(0.1f, 0f, 0.2f), 100, 3.5f, 1f);
            
            Debug.Log("✅ Particle sistem placeholder'ları oluşturuldu: Assets/Prefabs/Particles/");
        }

        private void CreateParticleSystem(string name, Color color, int particleCount, float duration, float speed)
        {
            GameObject particleObj = new GameObject(name);
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startColor = color;
            main.startLifetime = duration;
            main.startSpeed = speed;
            main.maxParticles = particleCount;
            main.loop = false;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, particleCount) });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            
            PrefabUtility.SaveAsPrefabAsset(particleObj, $"Assets/Prefabs/Particles/{name}.prefab");
            DestroyImmediate(particleObj);
        }
    }
}
