using UnityEngine;

namespace ARFishApp.Modules
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DynamicWaterSurface : MonoBehaviour
    {
        [System.Serializable]
        public struct WaveLayer
        {
            public bool enabled;
            public float amplitude;
            public float frequency;
            public float speed;
            public float phaseOffset;
            [Range(0f, 2f)] public float sharpness;
            public Vector2 direction;
        }

        [Header("Mesh Layout")]
        [Min(2)] public int resolutionX = 32;
        [Min(2)] public int resolutionZ = 32;
        public Vector2 size = new Vector2(4.5f, 4.5f);

        [Header("Wave Layers")]
        public WaveLayer rippleLayer = new WaveLayer
        {
            enabled = true,
            amplitude = 0.035f,
            frequency = 2.7f,
            speed = 1.35f,
            direction = new Vector2(0.85f, 0.2f)
        };

        public WaveLayer swellLayer = new WaveLayer
        {
            enabled = true,
            amplitude = 0.09f,
            frequency = 1.1f,
            speed = 0.55f,
            direction = new Vector2(-0.3f, 1f)
        };

        public WaveLayer turbulenceLayer = new WaveLayer
        {
            enabled = true,
            amplitude = 0.02f,
            frequency = 5.4f,
            speed = 1.9f,
            direction = new Vector2(1f, -0.4f)
        };

        [Header("Current Flow")]
        public Vector2 currentDirection = new Vector2(1f, 0.2f);
        public float currentStrength = 0.6f;
        [Range(0f, 1f)] public float currentPulseStrength = 0.18f;
        [Range(0f, 4f)] public float currentPulseSpeed = 0.9f;

        [Header("Surface Shaping")]
        [Range(0f, 1f)] public float edgeDamping = 0.6f;
        [Range(0.1f, 8f)] public float edgeDampingFalloff = 2.2f;
        [Min(5)] public int simulationFPS = 30;

        [Header("Shader Bridge")]
        public bool animateMaterialProperties = true;
        public string timeProperty = "_WaveTime";
        public string currentProperty = "_CurrentDirection";
        public string intensityProperty = "_CurrentStrength";
        public string flowOffsetProperty = "_FlowOffset";
        public string waveMixProperty = "_WaveMix";

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh generatedMesh;
        private Vector3[] baseVertices;
        private Vector3[] displacedVertices;
        private Vector3[] normals;
        private Vector2[] uv;
        private float[] edgeAttenuation;
        private int[] triangles;
        private float lastSimulationTime = -1f;
        private Vector2 accumulatedFlowOffset;

        public Vector3 CurrentFlowVector
        {
            get
            {
                Vector2 dir = currentDirection.sqrMagnitude > 0.0001f ? currentDirection.normalized : Vector2.right;
                return new Vector3(dir.x, 0f, dir.y) * currentStrength;
            }
        }

        private void Awake()
        {
            CacheComponents();
            EnsureMesh();
        }

        private void OnEnable()
        {
            CacheComponents();
            EnsureMesh();
            ApplyMaterialProperties(Application.isPlaying ? Time.time : 0f);
        }

        private void OnValidate()
        {
            resolutionX = Mathf.Max(2, resolutionX);
            resolutionZ = Mathf.Max(2, resolutionZ);
            size.x = Mathf.Max(0.25f, size.x);
            size.y = Mathf.Max(0.25f, size.y);
            simulationFPS = Mathf.Max(5, simulationFPS);

            CacheComponents();
            EnsureMesh();
            UpdateSurface(Application.isPlaying ? Time.time : 0f);
        }

        private void Update()
        {
            float timeValue = Application.isPlaying ? Time.time : 0f;
            if (ShouldSkipFrame(timeValue)) return;
            UpdateSurface(timeValue);
        }

        public void Configure(Vector2 newSize, int newResolutionX, int newResolutionZ)
        {
            size = newSize;
            resolutionX = Mathf.Max(2, newResolutionX);
            resolutionZ = Mathf.Max(2, newResolutionZ);
            EnsureMesh();
        }

        public void ConfigureCurrent(Vector2 direction, float strength)
        {
            currentDirection = direction;
            currentStrength = strength;
            ApplyMaterialProperties(Application.isPlaying ? Time.time : 0f);
        }

        public void ConfigureWaveLayers(WaveLayer ripple, WaveLayer swell, WaveLayer turbulence)
        {
            rippleLayer = ripple;
            swellLayer = swell;
            turbulenceLayer = turbulence;
            UpdateSurface(Application.isPlaying ? Time.time : 0f);
        }

        private void CacheComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        private void EnsureMesh()
        {
            if (generatedMesh == null)
            {
                generatedMesh = new Mesh
                {
                    name = "DynamicWaterSurfaceMesh"
                };
                generatedMesh.MarkDynamic();
            }

            BuildMeshData();
            ApplyMeshData();
        }

        private void BuildMeshData()
        {
            int vertexCount = resolutionX * resolutionZ;
            baseVertices = new Vector3[vertexCount];
            displacedVertices = new Vector3[vertexCount];
            normals = new Vector3[vertexCount];
            uv = new Vector2[vertexCount];
            edgeAttenuation = new float[vertexCount];
            triangles = new int[(resolutionX - 1) * (resolutionZ - 1) * 6];

            float stepX = size.x / (resolutionX - 1);
            float stepZ = size.y / (resolutionZ - 1);
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            int vertexIndex = 0;
            for (int z = 0; z < resolutionZ; z++)
            {
                for (int x = 0; x < resolutionX; x++)
                {
                    float posX = (x * stepX) - halfX;
                    float posZ = (z * stepZ) - halfZ;
                    Vector3 vertex = new Vector3(posX, 0f, posZ);

                    baseVertices[vertexIndex] = vertex;
                    displacedVertices[vertexIndex] = vertex;
                    normals[vertexIndex] = Vector3.up;
                    uv[vertexIndex] = new Vector2((float)x / (resolutionX - 1), (float)z / (resolutionZ - 1));
                    edgeAttenuation[vertexIndex] = EvaluateEdgeAttenuation(uv[vertexIndex]);
                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < resolutionZ - 1; z++)
            {
                for (int x = 0; x < resolutionX - 1; x++)
                {
                    int root = (z * resolutionX) + x;

                    triangles[triangleIndex++] = root;
                    triangles[triangleIndex++] = root + resolutionX;
                    triangles[triangleIndex++] = root + 1;

                    triangles[triangleIndex++] = root + 1;
                    triangles[triangleIndex++] = root + resolutionX;
                    triangles[triangleIndex++] = root + resolutionX + 1;
                }
            }
        }

        private void ApplyMeshData()
        {
            generatedMesh.Clear();
            generatedMesh.vertices = displacedVertices;
            generatedMesh.normals = normals;
            generatedMesh.uv = uv;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateBounds();

            meshFilter.sharedMesh = generatedMesh;
        }

        private void UpdateSurface(float timeValue)
        {
            if (generatedMesh == null || baseVertices == null || displacedVertices == null) return;

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 baseVertex = baseVertices[i];
                float height = EvaluateCombinedHeight(baseVertex, timeValue) * edgeAttenuation[i];

                displacedVertices[i] = new Vector3(baseVertex.x, height, baseVertex.z);
                normals[i] = EstimateNormal(baseVertex, timeValue);
            }

            generatedMesh.vertices = displacedVertices;
            generatedMesh.normals = normals;
            generatedMesh.RecalculateBounds();

            ApplyMaterialProperties(timeValue);
        }

        private float EvaluateWave(Vector3 vertex, float timeValue, WaveLayer layer)
        {
            if (!layer.enabled || layer.amplitude <= 0f) return 0f;

            Vector2 direction = layer.direction.sqrMagnitude > 0.0001f ? layer.direction.normalized : Vector2.right;
            float projected = (vertex.x * direction.x) + (vertex.z * direction.y);
            float phase = (projected * layer.frequency) + (timeValue * layer.speed) + layer.phaseOffset;
            float wave = Mathf.Sin(phase);
            float sharpness = Mathf.Max(0f, layer.sharpness);
            if (sharpness > 0f)
            {
                wave = Mathf.Sign(wave) * Mathf.Pow(Mathf.Abs(wave), 1f + sharpness);
            }

            return wave * layer.amplitude;
        }

        private Vector3 EstimateNormal(Vector3 vertex, float timeValue)
        {
            const float sampleOffset = 0.05f;
            Vector3 offsetX = new Vector3(sampleOffset, 0f, 0f);
            Vector3 offsetZ = new Vector3(0f, 0f, sampleOffset);

            float center = EvaluateCombinedHeight(vertex, timeValue);
            float sampleX = EvaluateCombinedHeight(vertex + offsetX, timeValue);
            float sampleZ = EvaluateCombinedHeight(vertex + offsetZ, timeValue);

            Vector3 tangentX = new Vector3(sampleOffset, sampleX - center, 0f);
            Vector3 tangentZ = new Vector3(0f, sampleZ - center, sampleOffset);
            return Vector3.Cross(tangentZ, tangentX).normalized;
        }

        private float EvaluateCombinedHeight(Vector3 vertex, float timeValue)
        {
            float currentPulse = 1f + (Mathf.Sin(timeValue * currentPulseSpeed) * currentPulseStrength);
            return (EvaluateWave(vertex, timeValue, rippleLayer)
                + EvaluateWave(vertex, timeValue, swellLayer)
                + EvaluateWave(vertex, timeValue, turbulenceLayer)) * currentPulse;
        }

        private void ApplyMaterialProperties(float timeValue)
        {
            if (!animateMaterialProperties || meshRenderer == null || meshRenderer.sharedMaterial == null) return;

            Material material = meshRenderer.sharedMaterial;
            if (!string.IsNullOrEmpty(timeProperty)) material.SetFloat(timeProperty, timeValue);
            float currentMagnitude = GetAnimatedCurrentStrength(timeValue);
            if (!string.IsNullOrEmpty(currentProperty))
            {
                Vector2 dir = currentDirection.sqrMagnitude > 0.0001f ? currentDirection.normalized : Vector2.right;
                material.SetVector(currentProperty, new Vector4(dir.x, dir.y, 0f, 0f));
            }
            if (!string.IsNullOrEmpty(intensityProperty)) material.SetFloat(intensityProperty, currentMagnitude);
            if (!string.IsNullOrEmpty(flowOffsetProperty))
            {
                accumulatedFlowOffset += GetCurrentDirectionNormalized() * currentMagnitude * Time.deltaTime * 0.05f;
                material.SetVector(flowOffsetProperty, new Vector4(accumulatedFlowOffset.x, accumulatedFlowOffset.y, 0f, 0f));
            }
            if (!string.IsNullOrEmpty(waveMixProperty))
            {
                material.SetVector(
                    waveMixProperty,
                    new Vector4(rippleLayer.amplitude, swellLayer.amplitude, turbulenceLayer.amplitude, currentMagnitude));
            }
        }

        public float SampleHeight(Vector3 worldPosition, float timeOffset = 0f)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float timeValue = (Application.isPlaying ? Time.time : 0f) + timeOffset;
            return transform.position.y + (EvaluateCombinedHeight(local, timeValue) * EvaluateEdgeAttenuation(LocalToUV(local)));
        }

        private bool ShouldSkipFrame(float timeValue)
        {
            if (!Application.isPlaying) return false;

            float frameInterval = 1f / Mathf.Max(5, simulationFPS);
            if (lastSimulationTime >= 0f && (timeValue - lastSimulationTime) < frameInterval)
            {
                return true;
            }

            lastSimulationTime = timeValue;
            return false;
        }

        private float EvaluateEdgeAttenuation(Vector2 uvCoord)
        {
            float edgeDistance = Mathf.Min(
                Mathf.Min(uvCoord.x, 1f - uvCoord.x),
                Mathf.Min(uvCoord.y, 1f - uvCoord.y));

            float normalized = Mathf.Clamp01(edgeDistance * 2f);
            float damped = Mathf.Pow(normalized, edgeDampingFalloff);
            return Mathf.Lerp(1f - edgeDamping, 1f, damped);
        }

        private Vector2 LocalToUV(Vector3 localPosition)
        {
            float u = Mathf.InverseLerp(-size.x * 0.5f, size.x * 0.5f, localPosition.x);
            float v = Mathf.InverseLerp(-size.y * 0.5f, size.y * 0.5f, localPosition.z);
            return new Vector2(u, v);
        }

        private Vector2 GetCurrentDirectionNormalized()
        {
            return currentDirection.sqrMagnitude > 0.0001f ? currentDirection.normalized : Vector2.right;
        }

        private float GetAnimatedCurrentStrength(float timeValue)
        {
            float currentPulse = 1f + (Mathf.Sin(timeValue * currentPulseSpeed) * currentPulseStrength);
            return currentStrength * currentPulse;
        }
    }
}
