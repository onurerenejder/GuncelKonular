using System.Collections;
using System.Collections.Generic;
using ARFishApp.Core;
using UnityEngine;

namespace ARFishApp.Modules
{
    public enum EnvironmentType
    {
        CoralReef,
        DeepOcean
    }

    [System.Serializable]
    public class HabitatWaterProfile
    {
        public Color shallowColor = new Color(0.16f, 0.82f, 0.92f, 0.72f);
        public Color deepColor = new Color(0.03f, 0.16f, 0.32f, 0.88f);
        [Range(0f, 1f)] public float metallic = 0.04f;
        [Range(0f, 1f)] public float smoothness = 0.82f;
        [Range(0f, 3f)] public float emissionStrength = 0.25f;
        public Vector2 flowDirection = new Vector2(1f, 0.2f);
        [Range(0f, 2f)] public float currentStrength = 0.65f;
        [Range(0f, 1f)] public float currentPulseStrength = 0.18f;
        [Range(0f, 4f)] public float currentPulseSpeed = 0.9f;
    }

    [System.Serializable]
    public class HabitatWaveProfile
    {
        public DynamicWaterSurface.WaveLayer rippleLayer;
        public DynamicWaterSurface.WaveLayer swellLayer;
        public DynamicWaterSurface.WaveLayer turbulenceLayer;
    }

    [System.Serializable]
    public class HabitatVisualProfile
    {
        public EnvironmentType habitatType;
        public Color targetLightColor = Color.white;
        [Range(0f, 3f)] public float targetLightIntensity = 1f;
        public Color ambientLightColor = new Color(0.14f, 0.32f, 0.4f);
        public Color fogColor = new Color(0.05f, 0.18f, 0.28f);
        [Range(0f, 0.25f)] public float fogDensity = 0.025f;
        [Range(0f, 1f)] public float environmentTintStrength = 0.35f;
        [Range(0f, 1f)] public float environmentSmoothness = 0.65f;
        public GameObject[] propPrefabs;
        public HabitatWaterProfile waterProfile = new HabitatWaterProfile();
        public HabitatWaveProfile waveProfile = new HabitatWaveProfile();
        [Range(5, 60)] public int waterSimulationFPS = 30;
        [Range(0f, 1f)] public float edgeDamping = 0.6f;
        [Range(0.1f, 8f)] public float edgeDampingFalloff = 2.2f;
    }

    public class HabitatModule : MonoBehaviour, IModule
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        [Header("Procedural Biome Engine")]
        public EnvironmentType currentHabitat;
        [Min(1)] public int objectDensity = 25;
        [Min(0.5f)] public float algorithmicScatterRadius = 4f;

        [Header("Habitat Profiles")]
        public HabitatVisualProfile[] habitatProfiles;

        [Header("Procedural Habitat Dressing")]
        public bool generateUnderwaterBackdrop = true;
        public Vector2 underwaterBackdropSize = new Vector2(9.5f, 5.5f);
        public Vector3 underwaterBackdropOffset = new Vector3(0f, 0.65f, 0f);
        [Min(0.5f)] public float underwaterBackdropDistance = 3f;
        public float underwaterFloorOffset = -0.58f;
        public bool generateProceduralHabitatDressing = true;
        public Vector3 habitatCompositionOffset = new Vector3(0f, 0.75f, 0f);
        public GameObject[] mossPrefabs;
        public GameObject[] rockPrefabs;
        public Vector2 mossScaleRange = new Vector2(0.35f, 0.8f);
        public Vector2 rockScaleRange = new Vector2(0.4f, 1.15f);
        [Min(0.05f)] public float importedMossMaxWorldSize = 0.42f;
        [Min(0.05f)] public float importedRockMaxWorldSize = 0.65f;
        [Min(0.1f)] public float decorationLateralRadius = 1.35f;
        [Min(0.05f)] public float decorationForwardMin = 0.55f;
        [Min(0.1f)] public float decorationForwardMax = 2.25f;
        [Min(0)] public int proceduralRockCount = 10;
        [Min(0)] public int proceduralCoralCount = 14;
        [Min(0)] public int proceduralSeaweedCount = 18;
        [Min(0)] public int proceduralBubbleCount = 24;

        [Header("Habitat Fish")]
        public bool useProceduralHabitatFish = true;
        public GameObject[] habitatFishPrefabs;
        [Min(0)] public int habitatFishCount = 8;
        [Min(0.25f)] public float habitatFishSwimRadius = 1.8f;
        [Min(0.1f)] public float habitatFishSwimSpeed = 0.35f;
        [Min(0f)] public float habitatFishHeightRange = 0.65f;
        [Min(0f)] public float habitatFishForwardOffset = 1.55f;
        [Min(0.05f)] public float habitatFishTargetSize = 1.2f;

        [Header("AR World Tracking Integrations")]
        [Tooltip("Layer mask assigned to AR Foundation generated physical planes (floor/tables).")]
        public LayerMask arFloorMeshLayer;

        [Header("Water Surface")]
        public bool spawnWaterSurface = true;
        public Material waterSurfaceMaterial;
        public Vector3 waterSurfaceLocalOffset = new Vector3(0f, 0.1f, 0f);
        public Vector2 waterSurfaceSize = new Vector2(4.5f, 4.5f);
        [Min(2)] public int waterResolutionX = 36;
        [Min(2)] public int waterResolutionZ = 36;

        [Header("Environment Rendering")]
        public Renderer[] environmentRenderers;
        public Light mainDirectionalLight;
        public ParticleSystem ambientFloatingParticles;
        public ParticleSystem currentDriftParticles;
        public float transitionSpeed = 1.4f;

        private GameObject generatedEnvironmentRoot;
        private DynamicWaterSurface dynamicWaterSurface;
        private Coroutine visualTransitionRoutine;
        private MaterialPropertyBlock propertyBlock;
        private Material proceduralFishBodyMaterial;
        private Material proceduralFishFinMaterial;
        private Material proceduralFishEyeMaterial;
        private Material proceduralRockMaterial;
        private Material proceduralCoralMaterial;
        private Material proceduralSeaweedMaterial;
        private Material proceduralBubbleMaterial;
        private Material underwaterBackdropMaterial;
        private Material underwaterSandMaterial;
        private Material underwaterLightShaftMaterial;
        private readonly Dictionary<Renderer, Color> baseRendererColors = new Dictionary<Renderer, Color>();
        private readonly List<Transform> activeHabitatFish = new List<Transform>();
        private readonly List<Vector3> habitatFishVelocities = new List<Vector3>();
        private Color defaultMainLightColor;
        private float defaultMainLightIntensity;
        private Color defaultAmbientLight;
        private Color defaultFogColor;
        private float defaultFogDensity;
        private bool defaultFogState;
        private bool defaultsCaptured;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            Debug.Log("[Habitat Module] Start called. Active in hierarchy: " + gameObject.activeInHierarchy);
            CacheRenderDefaults();

            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
                Debug.Log("[Habitat Module] Subscribed to state manager. Current state: " + SystemStateManager.Instance.CurrentModule);
                HandleStateChanged(SystemStateManager.Instance.CurrentModule);
            }
            else
            {
                Debug.LogWarning("[Habitat Module] SystemStateManager.Instance is null. Habitat cannot receive state changes.");
                OnModuleDeactivated();
            }
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(ModuleType newType)
        {
            Debug.Log("[Habitat Module] State received: " + newType);
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Habitat;

        public void OnModuleActivated()
        {
            Debug.Log("[Habitat Module] Habitat activated");
            CacheRenderDefaults();

            HabitatVisualProfile activeProfile = GetActiveProfile();
            EnsureProfileHasVisibleDefaults(activeProfile);
            Debug.Log("[Habitat Module] Active profile: " + (activeProfile != null ? activeProfile.habitatType.ToString() : "null"));
            if (activeProfile == null)
            {
                Debug.LogWarning("[Habitat Module] No habitat profile configured.");
                return;
            }

            Debug.Log("[Habitat Module] Engaged: Building dynamic underwater habitat.");

            CleanEnvironment();
            generatedEnvironmentRoot = new GameObject("Procedural_Habitat_Cluster");
            generatedEnvironmentRoot.transform.SetParent(transform, false);

            ScatterObjectsProcedurally(activeProfile.propPrefabs);
            BuildUnderwaterBackdrop();
            BuildProceduralHabitatDressing();
            SpawnHabitatFish();
            BuildDynamicWaterSurface(activeProfile);
            ApplyEnvironmentMaterialLook(activeProfile);
            SyncParticleSystems(activeProfile, true);

            StopAllCoroutines();
            visualTransitionRoutine = StartCoroutine(TransitionEnvironment(activeProfile, true));
        }

        public void OnModuleDeactivated()
        {
            StopAllCoroutines();
            CleanEnvironment();
            SyncParticleSystems(null, false);
            RestoreEnvironmentMaterialLook();

            if (defaultsCaptured)
            {
                if (mainDirectionalLight != null)
                {
                    mainDirectionalLight.color = defaultMainLightColor;
                    mainDirectionalLight.intensity = defaultMainLightIntensity;
                }

                RenderSettings.ambientLight = defaultAmbientLight;
                RenderSettings.fog = defaultFogState;
                RenderSettings.fogColor = defaultFogColor;
                RenderSettings.fogDensity = defaultFogDensity;
            }
        }

        private void Update()
        {
            UpdateHabitatFish();
        }

        private HabitatVisualProfile GetActiveProfile()
        {
            if (habitatProfiles == null) return null;

            for (int i = 0; i < habitatProfiles.Length; i++)
            {
                if (habitatProfiles[i] != null && habitatProfiles[i].habitatType == currentHabitat)
                {
                    return habitatProfiles[i];
                }
            }

            return habitatProfiles.Length > 0 ? habitatProfiles[0] : null;
        }

        private void BuildDynamicWaterSurface(HabitatVisualProfile profile)
        {
            if (!spawnWaterSurface) return;

            GameObject waterRoot = new GameObject("Dynamic_Water_Surface");
            waterRoot.transform.SetParent(generatedEnvironmentRoot.transform, false);
            waterRoot.transform.localPosition = waterSurfaceLocalOffset;

            MeshRenderer renderer = waterRoot.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (waterSurfaceMaterial != null)
            {
                renderer.sharedMaterial = waterSurfaceMaterial;
            }

            DynamicWaterSurface waterSurface = waterRoot.AddComponent<DynamicWaterSurface>();
            waterSurface.Configure(waterSurfaceSize, waterResolutionX, waterResolutionZ);
            DynamicWaterSurface.WaveLayer rippleLayer = ResolveWaveLayer(
                profile.waveProfile.rippleLayer,
                DynamicWaterSurfaceDefaults.CreateRippleLayer());
            DynamicWaterSurface.WaveLayer swellLayer = ResolveWaveLayer(
                profile.waveProfile.swellLayer,
                DynamicWaterSurfaceDefaults.CreateSwellLayer());
            DynamicWaterSurface.WaveLayer turbulenceLayer = ResolveWaveLayer(
                profile.waveProfile.turbulenceLayer,
                DynamicWaterSurfaceDefaults.CreateTurbulenceLayer());
            waterSurface.ConfigureWaveLayers(
                rippleLayer,
                swellLayer,
                turbulenceLayer);
            waterSurface.currentPulseStrength = profile.waterProfile.currentPulseStrength;
            waterSurface.currentPulseSpeed = profile.waterProfile.currentPulseSpeed;
            waterSurface.simulationFPS = profile.waterSimulationFPS;
            waterSurface.edgeDamping = profile.edgeDamping;
            waterSurface.edgeDampingFalloff = profile.edgeDampingFalloff;
            waterSurface.ConfigureCurrent(
                profile.waterProfile.flowDirection,
                profile.waterProfile.currentStrength);

            dynamicWaterSurface = waterSurface;
            ApplyWaterMaterialLook(profile);
        }

        private void ApplyWaterMaterialLook(HabitatVisualProfile profile)
        {
            if (dynamicWaterSurface == null) return;

            MeshRenderer waterRenderer = dynamicWaterSurface.GetComponent<MeshRenderer>();
            if (waterRenderer == null || waterRenderer.sharedMaterial == null) return;

            Material waterMaterial = waterRenderer.sharedMaterial;
            Color surfaceColor = Color.Lerp(profile.waterProfile.deepColor, profile.waterProfile.shallowColor, 0.65f);

            if (waterMaterial.HasProperty(BaseColorId)) waterMaterial.SetColor(BaseColorId, surfaceColor);
            if (waterMaterial.HasProperty(ColorId)) waterMaterial.SetColor(ColorId, surfaceColor);
            if (waterMaterial.HasProperty(SmoothnessId)) waterMaterial.SetFloat(SmoothnessId, profile.waterProfile.smoothness);
            if (waterMaterial.HasProperty(MetallicId)) waterMaterial.SetFloat(MetallicId, profile.waterProfile.metallic);
            if (waterMaterial.HasProperty(EmissionColorId))
            {
                Color emission = Color.Lerp(profile.waterProfile.deepColor, profile.waterProfile.shallowColor, 0.5f)
                    * profile.waterProfile.emissionStrength;
                waterMaterial.SetColor(EmissionColorId, emission);
                waterMaterial.EnableKeyword("_EMISSION");
            }
        }

        private void ScatterObjectsProcedurally(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return;

            for (int i = 0; i < objectDensity; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * algorithmicScatterRadius;
                Vector3 originPoint = transform.position + new Vector3(randomCircle.x, 3f, randomCircle.y);

                if (Physics.Raycast(originPoint, Vector3.down, out RaycastHit hit, 10f, arFloorMeshLayer))
                {
                    GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Length)];
                    Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    GameObject prop = Instantiate(
                        prefabToSpawn,
                        hit.point,
                        slopeRotation * Quaternion.Euler(0f, Random.Range(0, 360), 0f));

                    prop.transform.SetParent(generatedEnvironmentRoot.transform);
                    prop.transform.localScale *= Random.Range(0.5f, 1.6f);
                }
            }
        }

        private void SpawnHabitatFish()
        {
            Debug.Log("[Habitat Module] SpawnFish called");
            Debug.Log("[Habitat Module] Prefab count: " + (habitatFishPrefabs != null ? habitatFishPrefabs.Length : 0));
            Debug.Log("[Habitat Module] Procedural fish enabled: " + useProceduralHabitatFish);
            Debug.Log("[Habitat Module] Fish Count: " + habitatFishCount);

            ClearHabitatFish();

            if (generatedEnvironmentRoot == null
                || (!useProceduralHabitatFish && (habitatFishPrefabs == null || habitatFishPrefabs.Length == 0)))
            {
                Debug.LogWarning("[Habitat Module] Fish spawn skipped. Root exists: "
                    + (generatedEnvironmentRoot != null)
                    + ", prefab list assigned: "
                    + (habitatFishPrefabs != null)
                    + ", prefab count: "
                    + (habitatFishPrefabs != null ? habitatFishPrefabs.Length : 0));
                return;
            }

            for (int i = 0; i < habitatFishCount; i++)
            {
                Vector3 velocity = Random.onUnitSphere;
                velocity.y *= 0.2f;
                if (velocity.sqrMagnitude < 0.0001f) velocity = Vector3.forward;
                velocity = velocity.normalized * habitatFishSwimSpeed;

                GameObject fish;
                Vector3 spawnPosition = GetFishSwimCenter() + GetRandomFishOffset();
                Quaternion spawnRotation = Quaternion.LookRotation(velocity.normalized);

                if (useProceduralHabitatFish)
                {
                    fish = CreateProceduralHabitatFish(i);
                    fish.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                }
                else
                {
                    GameObject prefab = habitatFishPrefabs[Random.Range(0, habitatFishPrefabs.Length)];
                    Debug.Log("[Habitat Module] Creating fish " + i + " using prefab: " + (prefab != null ? prefab.name : "null"));
                    if (prefab == null)
                    {
                        Debug.LogWarning("[Habitat Module] Fish prefab at selected index is null. Skipping fish " + i);
                        continue;
                    }

                    fish = Instantiate(prefab, spawnPosition, spawnRotation);
                    PrepareSpawnedFishForVisibility(fish);
                }

                fish.SetActive(true);
                fish.transform.SetParent(generatedEnvironmentRoot.transform, true);
                Debug.Log($"[Habitat] Fish spawned: {fish.name} at position {fish.transform.position}");
                activeHabitatFish.Add(fish.transform);
                habitatFishVelocities.Add(velocity);
            }
        }

        private void UpdateHabitatFish()
        {
            if (activeHabitatFish.Count == 0) return;

            Vector3 center = GetFishSwimCenter();

            for (int i = activeHabitatFish.Count - 1; i >= 0; i--)
            {
                Transform fish = activeHabitatFish[i];
                if (fish == null)
                {
                    activeHabitatFish.RemoveAt(i);
                    habitatFishVelocities.RemoveAt(i);
                    continue;
                }

                Vector3 velocity = habitatFishVelocities[i];
                Vector3 offsetFromCenter = fish.position - center;

                if (offsetFromCenter.magnitude > habitatFishSwimRadius)
                {
                    Vector3 returnDirection = -offsetFromCenter.normalized;
                    velocity = Vector3.Lerp(velocity, returnDirection * habitatFishSwimSpeed, Time.deltaTime * 1.6f);
                }

                velocity += new Vector3(
                    Mathf.Sin(Time.time * 1.3f + i) * 0.15f,
                    Mathf.Sin(Time.time * 0.9f + i * 0.7f) * 0.08f,
                    Mathf.Cos(Time.time * 1.1f + i) * 0.15f) * Time.deltaTime;

                if (habitatFishHeightRange > 0f)
                {
                    float verticalOffset = fish.position.y - center.y;
                    if (Mathf.Abs(verticalOffset) > habitatFishHeightRange)
                    {
                        velocity.y = Mathf.Lerp(
                            velocity.y,
                            -Mathf.Sign(verticalOffset) * habitatFishSwimSpeed * 0.35f,
                            Time.deltaTime * 2f);
                    }
                }

                velocity = velocity.normalized * habitatFishSwimSpeed;
                fish.position += velocity * Time.deltaTime;

                if (velocity.sqrMagnitude > 0.0001f)
                {
                    fish.rotation = Quaternion.Slerp(fish.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * 4f);
                }

                habitatFishVelocities[i] = velocity;
            }
        }

        private Vector3 GetRandomFishOffset()
        {
            Vector2 horizontal = Random.insideUnitCircle * habitatFishSwimRadius;
            float vertical = habitatFishHeightRange > 0f ? Random.Range(-habitatFishHeightRange, habitatFishHeightRange) : 0f;
            return transform.right * horizontal.x + transform.up * vertical + transform.forward * horizontal.y;
        }

        private Vector3 GetFishSwimCenter()
        {
            return transform.position
                + transform.TransformVector(habitatCompositionOffset)
                + transform.up * waterSurfaceLocalOffset.y
                + transform.forward * habitatFishForwardOffset;
        }

        private GameObject CreateProceduralHabitatFish(int index)
        {
            EnsureProceduralFishMaterials();

            GameObject fishRoot = new GameObject("Procedural_Fish_" + index);
            fishRoot.transform.localScale = Vector3.one * habitatFishTargetSize;

            Color bodyColor = Color.HSVToRGB(Mathf.Repeat(index * 0.17f, 1f), 0.62f, 1f);
            Material bodyMaterial = new Material(proceduralFishBodyMaterial);
            ApplyMaterialColor(bodyMaterial, bodyColor, bodyColor * 0.75f);

            GameObject body = CreateFishPart("Body", PrimitiveType.Sphere, fishRoot.transform, bodyMaterial);
            body.transform.localScale = new Vector3(0.34f, 0.2f, 0.58f);

            GameObject head = CreateFishPart("Head", PrimitiveType.Sphere, fishRoot.transform, bodyMaterial);
            head.transform.localPosition = new Vector3(0f, 0.01f, 0.34f);
            head.transform.localScale = new Vector3(0.25f, 0.18f, 0.22f);

            GameObject tailA = CreateFishPart("Tail_Top", PrimitiveType.Cube, fishRoot.transform, proceduralFishFinMaterial);
            tailA.transform.localPosition = new Vector3(0f, 0.06f, -0.46f);
            tailA.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            tailA.transform.localScale = new Vector3(0.08f, 0.28f, 0.04f);

            GameObject tailB = CreateFishPart("Tail_Bottom", PrimitiveType.Cube, fishRoot.transform, proceduralFishFinMaterial);
            tailB.transform.localPosition = new Vector3(0f, -0.06f, -0.46f);
            tailB.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            tailB.transform.localScale = new Vector3(0.08f, 0.28f, 0.04f);

            GameObject dorsalFin = CreateFishPart("Dorsal_Fin", PrimitiveType.Cube, fishRoot.transform, proceduralFishFinMaterial);
            dorsalFin.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
            dorsalFin.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            dorsalFin.transform.localScale = new Vector3(0.06f, 0.22f, 0.22f);

            GameObject leftFin = CreateFishPart("Left_Fin", PrimitiveType.Cube, fishRoot.transform, proceduralFishFinMaterial);
            leftFin.transform.localPosition = new Vector3(-0.22f, -0.02f, 0.12f);
            leftFin.transform.localRotation = Quaternion.Euler(0f, 25f, 22f);
            leftFin.transform.localScale = new Vector3(0.04f, 0.12f, 0.22f);

            GameObject rightFin = CreateFishPart("Right_Fin", PrimitiveType.Cube, fishRoot.transform, proceduralFishFinMaterial);
            rightFin.transform.localPosition = new Vector3(0.22f, -0.02f, 0.12f);
            rightFin.transform.localRotation = Quaternion.Euler(0f, -25f, -22f);
            rightFin.transform.localScale = new Vector3(0.04f, 0.12f, 0.22f);

            GameObject leftEye = CreateFishPart("Left_Eye", PrimitiveType.Sphere, fishRoot.transform, proceduralFishEyeMaterial);
            leftEye.transform.localPosition = new Vector3(-0.09f, 0.09f, 0.48f);
            leftEye.transform.localScale = Vector3.one * 0.045f;

            GameObject rightEye = CreateFishPart("Right_Eye", PrimitiveType.Sphere, fishRoot.transform, proceduralFishEyeMaterial);
            rightEye.transform.localPosition = new Vector3(0.09f, 0.09f, 0.48f);
            rightEye.transform.localScale = Vector3.one * 0.045f;

            return fishRoot;
        }

        private GameObject CreateFishPart(string name, PrimitiveType primitiveType, Transform parent, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private void EnsureProceduralFishMaterials()
        {
            if (proceduralFishBodyMaterial != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            proceduralFishBodyMaterial = new Material(shader) { name = "Procedural Fish Body" };
            proceduralFishFinMaterial = new Material(shader) { name = "Procedural Fish Fins" };
            proceduralFishEyeMaterial = new Material(shader) { name = "Procedural Fish Eyes" };

            ApplyMaterialColor(proceduralFishBodyMaterial, new Color(0.1f, 0.95f, 1f, 1f), new Color(0.08f, 0.45f, 0.7f, 1f));
            ApplyMaterialColor(proceduralFishFinMaterial, new Color(1f, 0.78f, 0.16f, 1f), new Color(0.75f, 0.35f, 0.04f, 1f));
            ApplyMaterialColor(proceduralFishEyeMaterial, Color.white, Color.white * 0.5f);
        }

        private void EnsureProceduralHabitatMaterials()
        {
            if (proceduralRockMaterial != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            proceduralRockMaterial = new Material(shader) { name = "Procedural Reef Rock" };
            proceduralCoralMaterial = new Material(shader) { name = "Procedural Coral" };
            proceduralSeaweedMaterial = new Material(shader) { name = "Procedural Seaweed" };
            proceduralBubbleMaterial = new Material(shader) { name = "Procedural Bubbles" };
            underwaterBackdropMaterial = new Material(shader) { name = "Underwater Backdrop" };
            underwaterSandMaterial = new Material(shader) { name = "Underwater Floor" };
            underwaterLightShaftMaterial = new Material(shader) { name = "Underwater Light Shafts" };

            ApplyMaterialColor(proceduralRockMaterial, new Color(0.18f, 0.2f, 0.21f, 1f), new Color(0.01f, 0.03f, 0.04f, 1f));
            ApplyMaterialColor(proceduralCoralMaterial, new Color(0.86f, 0.28f, 0.24f, 1f), new Color(0.22f, 0.05f, 0.04f, 1f));
            ApplyMaterialColor(proceduralSeaweedMaterial, new Color(0.03f, 0.36f, 0.18f, 1f), new Color(0.005f, 0.035f, 0.015f, 1f));
            ApplyMaterialColor(proceduralBubbleMaterial, new Color(0.55f, 0.88f, 1f, 0.38f), new Color(0.06f, 0.18f, 0.24f, 1f));
            ApplyMaterialColor(underwaterBackdropMaterial, new Color(0.02f, 0.32f, 0.48f, 1f), new Color(0.02f, 0.16f, 0.24f, 1f));
            ApplyMaterialColor(underwaterSandMaterial, new Color(0.16f, 0.18f, 0.16f, 1f), new Color(0.02f, 0.025f, 0.018f, 1f));
            ApplyMaterialColor(underwaterLightShaftMaterial, new Color(0.35f, 0.8f, 0.9f, 1f), new Color(0.08f, 0.28f, 0.32f, 1f));
        }

        private void ApplyMaterialColor(Material material, Color color, Color emission)
        {
            if (material == null) return;

            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
            if (material.HasProperty(EmissionColorId))
            {
                material.SetColor(EmissionColorId, emission);
                material.EnableKeyword("_EMISSION");
            }
        }

        private void BuildUnderwaterBackdrop()
        {
            if (!generateUnderwaterBackdrop || generatedEnvironmentRoot == null) return;

            EnsureProceduralHabitatMaterials();

            Vector3 center = GetFishSwimCenter();
            Vector3 backdropCenter = center
                + transform.TransformVector(underwaterBackdropOffset)
                + transform.forward * underwaterBackdropDistance
                + transform.up * 0.25f;

            GameObject backdrop = CreateQuad("Underwater_Backdrop", underwaterBackdropMaterial);
            backdrop.transform.SetParent(generatedEnvironmentRoot.transform, true);
            backdrop.transform.position = backdropCenter;
            backdrop.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
            backdrop.transform.localScale = new Vector3(underwaterBackdropSize.x, underwaterBackdropSize.y, 1f);

            GameObject floor = CreateQuad("Underwater_Floor", underwaterSandMaterial);
            floor.transform.SetParent(generatedEnvironmentRoot.transform, true);
            floor.transform.position = center
                + transform.up * underwaterFloorOffset
                + transform.forward * (underwaterBackdropDistance * 0.5f);
            floor.transform.rotation = Quaternion.LookRotation(transform.up, -transform.forward);
            floor.transform.localScale = new Vector3(underwaterBackdropSize.x, underwaterBackdropDistance * 1.2f, 1f);

            for (int i = 0; i < 5; i++)
            {
                GameObject shaft = CreateQuad("Underwater_LightShaft_" + i, underwaterLightShaftMaterial);
                shaft.transform.SetParent(generatedEnvironmentRoot.transform, true);
                shaft.transform.position = backdropCenter
                    + transform.right * Mathf.Lerp(-underwaterBackdropSize.x * 0.38f, underwaterBackdropSize.x * 0.38f, i / 4f)
                    + transform.up * Random.Range(-0.6f, 1.6f)
                    - transform.forward * 0.08f;
                shaft.transform.rotation = Quaternion.LookRotation(-transform.forward, transform.up)
                    * Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
                shaft.transform.localScale = new Vector3(Random.Range(0.08f, 0.18f), Random.Range(1.4f, 2.5f), 1f);
            }
        }

        private GameObject CreateQuad(string name, Material material)
        {
            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                2, 3, 1,
                0, 1, 2,
                2, 1, 3
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.RecalculateNormals();

            GameObject quad = new GameObject(name);
            MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return quad;
        }

        private void BuildProceduralHabitatDressing()
        {
            if (!generateProceduralHabitatDressing || generatedEnvironmentRoot == null) return;

            EnsureProceduralHabitatMaterials();

            if (rockPrefabs != null && rockPrefabs.Length > 0)
            {
                SpawnPrefabCluster(rockPrefabs, "Imported_Rock_", proceduralRockCount, -0.44f, rockScaleRange, importedRockMaxWorldSize, true);
            }
            else
            {
                for (int i = 0; i < proceduralRockCount; i++)
                {
                    Vector3 position = GetProceduralDressingPosition(-0.42f);
                    GameObject rock = CreateDressingPart("Habitat_Rock_" + i, PrimitiveType.Sphere, proceduralRockMaterial);
                    rock.transform.position = position;
                    rock.transform.rotation = Random.rotation;
                    rock.transform.localScale = new Vector3(
                        Random.Range(0.22f, 0.52f),
                        Random.Range(0.08f, 0.2f),
                        Random.Range(0.2f, 0.48f));
                }
            }

            for (int i = 0; i < proceduralCoralCount; i++)
            {
                Vector3 position = GetProceduralDressingPosition(-0.36f);
                GameObject coral = new GameObject("Habitat_Coral_" + i);
                coral.transform.SetParent(generatedEnvironmentRoot.transform, true);
                coral.transform.position = position;
                coral.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                int branches = Random.Range(3, 6);
                for (int b = 0; b < branches; b++)
                {
                    GameObject branch = CreateDressingPart("Coral_Branch", PrimitiveType.Cylinder, proceduralCoralMaterial);
                    branch.transform.SetParent(coral.transform, false);
                    branch.transform.localPosition = new Vector3(
                        Random.Range(-0.08f, 0.08f),
                        0.08f + b * 0.035f,
                        Random.Range(-0.08f, 0.08f));
                    branch.transform.localRotation = Quaternion.Euler(
                        Random.Range(-28f, 28f),
                        Random.Range(0f, 360f),
                        Random.Range(-28f, 28f));
                    branch.transform.localScale = new Vector3(
                        Random.Range(0.025f, 0.05f),
                        Random.Range(0.08f, 0.18f),
                        Random.Range(0.025f, 0.05f));
                }
            }

            if (mossPrefabs != null && mossPrefabs.Length > 0)
            {
                SpawnPrefabCluster(mossPrefabs, "Imported_Moss_", proceduralSeaweedCount, -0.3f, mossScaleRange, importedMossMaxWorldSize, false);
            }
            else
            {
                for (int i = 0; i < proceduralSeaweedCount; i++)
                {
                    Vector3 position = GetProceduralDressingPosition(-0.28f);
                    GameObject seaweed = CreateSeaweedBlade("Habitat_Seaweed_" + i, Random.Range(0.45f, 0.9f), Random.Range(0.06f, 0.12f));
                    seaweed.transform.SetParent(generatedEnvironmentRoot.transform, true);
                    seaweed.transform.position = position;
                    seaweed.transform.rotation = Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f);
                }
            }

            for (int i = 0; i < proceduralBubbleCount; i++)
            {
                Vector3 position = GetProceduralDressingPosition(Random.Range(-0.05f, 0.95f));
                GameObject bubble = CreateDressingPart("Habitat_Bubble_" + i, PrimitiveType.Sphere, proceduralBubbleMaterial);
                bubble.transform.position = position;
                bubble.transform.localScale = Vector3.one * Random.Range(0.025f, 0.065f);
            }
        }

        private Vector3 GetProceduralDressingPosition(float verticalOffset)
        {
            float lateralRadius = Mathf.Max(0.1f, decorationLateralRadius);
            float forwardMin = Mathf.Max(0.05f, decorationForwardMin);
            float forwardMax = Mathf.Max(forwardMin + 0.05f, decorationForwardMax);
            float lateral = Random.Range(-lateralRadius, lateralRadius);
            float forward = Random.Range(forwardMin, forwardMax);

            return GetFishSwimCenter()
                + transform.right * lateral
                + transform.forward * forward
                + transform.up * verticalOffset;
        }

        private void SpawnPrefabCluster(
            GameObject[] prefabs,
            string namePrefix,
            int count,
            float verticalOffset,
            Vector2 scaleRange,
            float maxWorldSize,
            bool sinkIntoFloor)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;

                Vector3 position = GetProceduralDressingPosition(verticalOffset);
                Quaternion rotation = Quaternion.Euler(
                    sinkIntoFloor ? Random.Range(-8f, 8f) : 0f,
                    Random.Range(0f, 360f),
                    sinkIntoFloor ? Random.Range(-8f, 8f) : Random.Range(-4f, 4f));

                GameObject instance = Instantiate(prefab, position, rotation, generatedEnvironmentRoot.transform);
                instance.name = namePrefix + i + "_" + prefab.name;

                float scale = Random.Range(scaleRange.x, scaleRange.y);
                instance.transform.localScale = Vector3.one * scale;
                PrepareSpawnedDecoration(instance);
                ClampDecorationWorldSize(instance, maxWorldSize);
            }
        }

        private void ClampDecorationWorldSize(GameObject decoration, float maxWorldSize)
        {
            if (decoration == null || maxWorldSize <= 0f) return;

            Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float largestAxis = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestAxis <= maxWorldSize || largestAxis <= 0.0001f) return;

            float scaleMultiplier = maxWorldSize / largestAxis;
            decoration.transform.localScale *= scaleMultiplier;
        }

        private void PrepareSpawnedDecoration(GameObject decoration)
        {
            Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }

            Collider[] colliders = decoration.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private GameObject CreateDressingPart(string name, PrimitiveType primitiveType, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(generatedEnvironmentRoot.transform, true);

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private GameObject CreateSeaweedBlade(string name, float height, float width)
        {
            int segmentCount = 5;
            Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segmentCount * 6];
            float sway = Random.Range(0.04f, 0.12f);

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                float taper = Mathf.Lerp(1f, 0.28f, t);
                float xOffset = Mathf.Sin(t * Mathf.PI * 1.35f + Random.Range(-0.25f, 0.25f)) * sway * t;
                float halfWidth = width * taper * 0.5f;
                vertices[i * 2] = new Vector3(xOffset - halfWidth, height * t, 0f);
                vertices[i * 2 + 1] = new Vector3(xOffset + halfWidth, height * t, 0f);
                uvs[i * 2] = new Vector2(0f, t);
                uvs[i * 2 + 1] = new Vector2(1f, t);

                if (i < segmentCount)
                {
                    int triangleIndex = i * 6;
                    int vertexIndex = i * 2;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 2;
                    triangles[triangleIndex + 2] = vertexIndex + 1;
                    triangles[triangleIndex + 3] = vertexIndex + 1;
                    triangles[triangleIndex + 4] = vertexIndex + 2;
                    triangles[triangleIndex + 5] = vertexIndex + 3;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            GameObject blade = new GameObject(name);
            MeshFilter meshFilter = blade.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer meshRenderer = blade.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = proceduralSeaweedMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return blade;
        }

        private void PrepareSpawnedFishForVisibility(GameObject fish)
        {
            Renderer[] renderers = fish.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning("[Habitat Module] Spawned fish has no visible Renderer components: " + fish.name);
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
            }

            if (habitatFishTargetSize <= 0f) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float largestAxis = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestAxis > 0.0001f)
            {
                float scaleMultiplier = habitatFishTargetSize / largestAxis;
                fish.transform.localScale *= scaleMultiplier;
            }
        }

        private void EnsureProfileHasVisibleDefaults(HabitatVisualProfile profile)
        {
            if (profile == null) return;

            if (profile.targetLightIntensity <= 0.01f)
            {
                profile.targetLightIntensity = 1.25f;
            }

            if (IsInvisibleColor(profile.targetLightColor))
            {
                profile.targetLightColor = new Color(0.72f, 0.94f, 1f, 1f);
            }

            if (IsInvisibleColor(profile.ambientLightColor))
            {
                profile.ambientLightColor = new Color(0.18f, 0.38f, 0.44f, 1f);
            }

            if (IsInvisibleColor(profile.fogColor))
            {
                profile.fogColor = new Color(0.04f, 0.18f, 0.28f, 1f);
            }

            if (profile.fogDensity <= 0f)
            {
                profile.fogDensity = 0.018f;
            }
        }

        private bool IsInvisibleColor(Color color)
        {
            return color.maxColorComponent <= 0.01f && color.a <= 0.01f;
        }

        private void ApplyEnvironmentMaterialLook(HabitatVisualProfile profile)
        {
            if (environmentRenderers == null) return;
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < environmentRenderers.Length; i++)
            {
                Renderer targetRenderer = environmentRenderers[i];
                if (targetRenderer == null) continue;

                Color baseColor = GetRendererBaseColor(targetRenderer);
                Color blendedColor = Color.Lerp(baseColor, profile.fogColor, profile.environmentTintStrength);
                Color emission = Color.Lerp(profile.fogColor, profile.ambientLightColor, 0.45f) * 0.1f;

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, blendedColor);
                propertyBlock.SetColor(ColorId, blendedColor);
                propertyBlock.SetColor(EmissionColorId, emission);
                propertyBlock.SetFloat(SmoothnessId, profile.environmentSmoothness);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void RestoreEnvironmentMaterialLook()
        {
            if (environmentRenderers == null) return;

            for (int i = 0; i < environmentRenderers.Length; i++)
            {
                Renderer targetRenderer = environmentRenderers[i];
                if (targetRenderer == null) continue;
                targetRenderer.SetPropertyBlock(null);
            }
        }

        private Color GetRendererBaseColor(Renderer targetRenderer)
        {
            if (baseRendererColors.TryGetValue(targetRenderer, out Color cachedColor))
            {
                return cachedColor;
            }

            Material sharedMaterial = targetRenderer.sharedMaterial;
            Color discoveredColor = Color.white;

            if (sharedMaterial != null)
            {
                if (sharedMaterial.HasProperty(BaseColorId)) discoveredColor = sharedMaterial.GetColor(BaseColorId);
                else if (sharedMaterial.HasProperty(ColorId)) discoveredColor = sharedMaterial.GetColor(ColorId);
            }

            baseRendererColors[targetRenderer] = discoveredColor;
            return discoveredColor;
        }

        private void SyncParticleSystems(HabitatVisualProfile profile, bool play)
        {
            if (ambientFloatingParticles != null)
            {
                if (play) ambientFloatingParticles.Play();
                else ambientFloatingParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (currentDriftParticles != null)
            {
                if (play && profile != null)
                {
                    var velocity = currentDriftParticles.velocityOverLifetime;
                    velocity.enabled = true;
                    Vector3 currentFlow = profile.waterProfile.flowDirection.normalized * profile.waterProfile.currentStrength * 0.3f;
                    velocity.x = new ParticleSystem.MinMaxCurve(currentFlow.x);
                    velocity.z = new ParticleSystem.MinMaxCurve(currentFlow.y);
                    currentDriftParticles.Play();
                }
                else
                {
                    currentDriftParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private IEnumerator TransitionEnvironment(HabitatVisualProfile profile, bool activating)
        {
            if (mainDirectionalLight == null)
            {
                yield break;
            }

            Color startLightColor = mainDirectionalLight.color;
            float startLightIntensity = mainDirectionalLight.intensity;
            Color startAmbientColor = RenderSettings.ambientLight;
            Color startFogColor = RenderSettings.fogColor;
            float startFogDensity = RenderSettings.fogDensity;
            float t = 0f;

            RenderSettings.fog = true;

            while (t < 1f)
            {
                t += Time.deltaTime * transitionSpeed;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

                mainDirectionalLight.color = Color.Lerp(startLightColor, profile.targetLightColor, eased);
                mainDirectionalLight.intensity = Mathf.Lerp(startLightIntensity, profile.targetLightIntensity, eased);
                RenderSettings.ambientLight = Color.Lerp(startAmbientColor, profile.ambientLightColor, eased);
                RenderSettings.fogColor = Color.Lerp(startFogColor, profile.fogColor, eased);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, profile.fogDensity, eased);

                yield return null;
            }

            visualTransitionRoutine = null;
        }

        private void CacheRenderDefaults()
        {
            if (defaultsCaptured) return;

            if (mainDirectionalLight != null)
            {
                defaultMainLightColor = mainDirectionalLight.color;
                defaultMainLightIntensity = mainDirectionalLight.intensity;
            }

            defaultAmbientLight = RenderSettings.ambientLight;
            defaultFogColor = RenderSettings.fogColor;
            defaultFogDensity = RenderSettings.fogDensity;
            defaultFogState = RenderSettings.fog;
            defaultsCaptured = true;
        }

        private DynamicWaterSurface.WaveLayer ResolveWaveLayer(
            DynamicWaterSurface.WaveLayer candidate,
            DynamicWaterSurface.WaveLayer fallback)
        {
            if (!candidate.enabled && candidate.amplitude <= 0f && candidate.frequency <= 0f && candidate.speed <= 0f)
            {
                return fallback;
            }

            return candidate;
        }

        private void CleanEnvironment()
        {
            ClearHabitatFish();
            dynamicWaterSurface = null;
            if (generatedEnvironmentRoot != null)
            {
                Destroy(generatedEnvironmentRoot);
                generatedEnvironmentRoot = null;
            }
        }

        private void ClearHabitatFish()
        {
            for (int i = 0; i < activeHabitatFish.Count; i++)
            {
                if (activeHabitatFish[i] != null)
                {
                    Destroy(activeHabitatFish[i].gameObject);
                }
            }

            activeHabitatFish.Clear();
            habitatFishVelocities.Clear();
        }
    }

    internal static class DynamicWaterSurfaceDefaults
    {
        public static DynamicWaterSurface.WaveLayer CreateRippleLayer()
        {
            return new DynamicWaterSurface.WaveLayer
            {
                enabled = true,
                amplitude = 0.035f,
                frequency = 2.7f,
                speed = 1.35f,
                phaseOffset = 0.35f,
                sharpness = 0.2f,
                direction = new Vector2(0.85f, 0.2f)
            };
        }

        public static DynamicWaterSurface.WaveLayer CreateSwellLayer()
        {
            return new DynamicWaterSurface.WaveLayer
            {
                enabled = true,
                amplitude = 0.09f,
                frequency = 1.1f,
                speed = 0.55f,
                phaseOffset = 1.25f,
                sharpness = 0.05f,
                direction = new Vector2(-0.3f, 1f)
            };
        }

        public static DynamicWaterSurface.WaveLayer CreateTurbulenceLayer()
        {
            return new DynamicWaterSurface.WaveLayer
            {
                enabled = true,
                amplitude = 0.02f,
                frequency = 5.4f,
                speed = 1.9f,
                phaseOffset = 2.1f,
                sharpness = 0.35f,
                direction = new Vector2(1f, -0.4f)
            };
        }
    }
}
