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
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private readonly Dictionary<Renderer, Color> baseRendererColors = new Dictionary<Renderer, Color>();
        private Color defaultMainLightColor;
        private float defaultMainLightIntensity;
        private Color defaultAmbientLight;
        private Color defaultFogColor;
        private float defaultFogDensity;
        private bool defaultFogState;
        private bool defaultsCaptured;

        private void Start()
        {
            CacheRenderDefaults();

            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            }

            OnModuleDeactivated();
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
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Habitat;

        public void OnModuleActivated()
        {
            CacheRenderDefaults();

            HabitatVisualProfile activeProfile = GetActiveProfile();
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

        private void ApplyEnvironmentMaterialLook(HabitatVisualProfile profile)
        {
            if (environmentRenderers == null) return;

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
            dynamicWaterSurface = null;
            if (generatedEnvironmentRoot != null)
            {
                Destroy(generatedEnvironmentRoot);
                generatedEnvironmentRoot = null;
            }
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
