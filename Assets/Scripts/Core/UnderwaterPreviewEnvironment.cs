using UnityEngine;

namespace ARFishApp.Core
{
    public class UnderwaterPreviewEnvironment : MonoBehaviour
    {
        public Color backgroundColor = new Color(0.02f, 0.2f, 0.34f);
        public Color fogColor = new Color(0.02f, 0.16f, 0.26f);
        [Range(0f, 0.12f)] public float fogDensity = 0.035f;
        public Color ambientLight = new Color(0.12f, 0.35f, 0.48f);
        public bool createBubbles = false;
        public Vector3 bubbleLocalOffset = new Vector3(0f, -0.45f, 0.15f);
        public bool createSeabedProps = false;
        public bool createTwoDimensionalBackdrop = true;
        public Vector3 backdropLocalOffset = new Vector3(0f, -0.05f, 1.05f);
        public Vector2 backdropSize = new Vector2(5.4f, 3.2f);
        public Texture2D backdropTexture;
        public string resourcesBackdropPath = "Backgrounds/underwater_cartoon_backdrop";

        private bool capturedDefaults;
        private bool defaultFog;
        private Color defaultFogColor;
        private float defaultFogDensity;
        private Color defaultAmbientLight;

        private void Awake()
        {
            CaptureDefaults();
            ApplyEnvironment();
            if (createBubbles)
            {
                EnsureBubbleParticles();
            }

            if (createSeabedProps)
            {
                EnsureSeabedProps();
            }

            if (createTwoDimensionalBackdrop)
            {
                EnsureTwoDimensionalBackdrop();
            }
        }

        private void OnEnable()
        {
            ApplyEnvironment();
        }

        private void OnDestroy()
        {
            if (!capturedDefaults) return;

            RenderSettings.fog = defaultFog;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogDensity = defaultFogDensity;
            RenderSettings.ambientLight = defaultAmbientLight;
        }

        private void CaptureDefaults()
        {
            if (capturedDefaults) return;

            defaultFog = RenderSettings.fog;
            defaultFogColor = RenderSettings.fogColor;
            defaultFogDensity = RenderSettings.fogDensity;
            defaultAmbientLight = RenderSettings.ambientLight;
            capturedDefaults = true;
        }

        private void ApplyEnvironment()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backgroundColor;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientLight = ambientLight;
        }

        private void EnsureBubbleParticles()
        {
            if (transform.Find("Preview_Bubbles") != null) return;

            GameObject bubblesObject = new GameObject("Preview_Bubbles");
            bubblesObject.transform.SetParent(transform, false);
            bubblesObject.transform.localPosition = bubbleLocalOffset;

            ParticleSystem bubbles = bubblesObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = bubbles.main;
            main.startLifetime = 3.2f;
            main.startSpeed = 0.35f;
            main.startSize = 0.035f;
            main.startColor = new Color(0.75f, 0.95f, 1f, 0.55f);
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = bubbles.emission;
            emission.rateOverTime = 14f;

            ParticleSystem.ShapeModule shape = bubbles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.8f, 0.1f, 0.35f);

            ParticleSystem.VelocityOverLifetimeModule velocity = bubbles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.38f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystemRenderer renderer = bubbles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        private void EnsureSeabedProps()
        {
            if (transform.Find("Preview_Seabed") != null) return;

            GameObject seabed = new GameObject("Preview_Seabed");
            seabed.transform.SetParent(transform, false);
            seabed.transform.localPosition = new Vector3(0f, -0.9f, 0.65f);

            Material sandMaterial = CreateMaterial("Preview_Sand", new Color(0.53f, 0.48f, 0.35f));
            Material rockMaterial = CreateMaterial("Preview_Rock", new Color(0.28f, 0.32f, 0.34f));
            Material coralPink = CreateMaterial("Preview_Coral_Pink", new Color(0.95f, 0.35f, 0.42f));
            Material coralOrange = CreateMaterial("Preview_Coral_Orange", new Color(1f, 0.52f, 0.18f));
            Material coralPurple = CreateMaterial("Preview_Coral_Purple", new Color(0.56f, 0.35f, 0.85f));
            Material seaweedMaterial = CreateMaterial("Preview_Seaweed", new Color(0.12f, 0.55f, 0.32f));

            GameObject sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sand.name = "SandFloor";
            sand.transform.SetParent(seabed.transform, false);
            sand.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            sand.transform.localScale = new Vector3(4.8f, 0.08f, 1.2f);
            ApplyMaterial(sand, sandMaterial);
            RemoveCollider(sand);

            CreateRock(seabed.transform, new Vector3(-1.25f, -0.05f, 0.05f), new Vector3(0.62f, 0.28f, 0.38f), rockMaterial);
            CreateRock(seabed.transform, new Vector3(1.35f, -0.04f, -0.08f), new Vector3(0.72f, 0.34f, 0.44f), rockMaterial);
            CreateRock(seabed.transform, new Vector3(0.35f, -0.06f, 0.18f), new Vector3(0.44f, 0.2f, 0.32f), rockMaterial);

            CreateCoralCluster(seabed.transform, new Vector3(-0.9f, 0.04f, -0.05f), coralPink);
            CreateCoralCluster(seabed.transform, new Vector3(0.95f, 0.03f, 0.08f), coralOrange);
            CreateCoralCluster(seabed.transform, new Vector3(1.75f, 0.02f, -0.02f), coralPurple);

            CreateSeaweed(seabed.transform, new Vector3(-1.85f, 0.02f, 0.02f), seaweedMaterial);
            CreateSeaweed(seabed.transform, new Vector3(-0.25f, 0.0f, -0.06f), seaweedMaterial);
            CreateSeaweed(seabed.transform, new Vector3(1.95f, 0.01f, 0.04f), seaweedMaterial);
        }

        private static void CreateRock(Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = localPosition;
            rock.transform.localScale = localScale;
            rock.transform.localRotation = Quaternion.Euler(0f, Random.Range(-12f, 12f), Random.Range(-8f, 8f));
            ApplyMaterial(rock, material);
            RemoveCollider(rock);
        }

        private static void CreateCoralCluster(Transform parent, Vector3 localPosition, Material material)
        {
            GameObject root = new GameObject("CoralCluster");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            for (int i = 0; i < 5; i++)
            {
                GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "CoralBranch";
                branch.transform.SetParent(root.transform, false);
                branch.transform.localPosition = new Vector3((i - 2) * 0.055f, 0.08f + i * 0.015f, 0f);
                branch.transform.localRotation = Quaternion.Euler(Random.Range(-22f, 22f), 0f, Random.Range(-24f, 24f));
                branch.transform.localScale = new Vector3(0.025f, 0.16f + i * 0.018f, 0.025f);
                ApplyMaterial(branch, material);
                RemoveCollider(branch);
            }
        }

        private static void CreateSeaweed(Transform parent, Vector3 localPosition, Material material)
        {
            GameObject root = new GameObject("SeaweedCluster");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "SeaweedBlade";
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3((i - 1.5f) * 0.045f, 0.12f + i * 0.015f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-16f, 16f));
                blade.transform.localScale = new Vector3(0.025f, 0.32f + i * 0.04f, 0.018f);
                ApplyMaterial(blade, material);
                RemoveCollider(blade);
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Diffuse"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void EnsureTwoDimensionalBackdrop()
        {
            if (transform.Find("Preview_2D_Underwater_Backdrop") != null) return;

            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Preview_2D_Underwater_Backdrop";
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.localPosition = backdropLocalOffset;
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = new Vector3(backdropSize.x, backdropSize.y, 1f);
            RemoveCollider(backdrop);

            Texture2D texture = backdropTexture != null
                ? backdropTexture
                : Resources.Load<Texture2D>(resourcesBackdropPath);

            if (texture == null)
            {
                texture = BuildBackdropTexture(1024, 512);
            }

            Material material = CreateUnlitTextureMaterial(texture);
            Renderer renderer = backdrop.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.sortingOrder = -50;
            }
        }

        private static Texture2D BuildBackdropTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated_Underwater_Backdrop";

            Color top = new Color(0.02f, 0.42f, 0.62f, 1f);
            Color middle = new Color(0.02f, 0.25f, 0.42f, 1f);
            Color bottom = new Color(0.01f, 0.12f, 0.24f, 1f);

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color rowColor = t < 0.62f
                    ? Color.Lerp(bottom, middle, t / 0.62f)
                    : Color.Lerp(middle, top, (t - 0.62f) / 0.38f);

                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, rowColor);
                }
            }

            DrawSunRays(texture, width, height);
            DrawSeabed(texture, width, height);
            DrawRocks(texture, width, height);
            DrawCorals(texture, width, height);
            DrawSeaweed(texture, width, height);
            DrawBubbles(texture, width, height);

            texture.Apply();
            return texture;
        }

        private static void DrawSunRays(Texture2D texture, int width, int height)
        {
            Color rayColor = new Color(0.65f, 0.92f, 1f, 0.13f);
            for (int ray = 0; ray < 5; ray++)
            {
                int startX = 120 + ray * 190;
                int widthAtTop = 34 + ray * 5;

                for (int y = height - 1; y > height * 0.22f; y--)
                {
                    float depth = (height - y) / (float)height;
                    int center = startX + Mathf.RoundToInt(depth * (ray % 2 == 0 ? 120f : -90f));
                    int halfWidth = Mathf.RoundToInt(widthAtTop + depth * 110f);

                    for (int x = center - halfWidth; x <= center + halfWidth; x++)
                    {
                        if (x < 0 || x >= width) continue;
                        Color current = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, Color.Lerp(current, rayColor, 0.18f));
                    }
                }
            }
        }

        private static void DrawSeabed(Texture2D texture, int width, int height)
        {
            Color sand = new Color(0.48f, 0.43f, 0.3f, 1f);
            Color shadow = new Color(0.26f, 0.25f, 0.2f, 1f);
            int baseY = Mathf.RoundToInt(height * 0.13f);

            for (int y = 0; y < baseY + 30; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float wave = Mathf.Sin(x * 0.018f) * 12f + Mathf.Sin(x * 0.006f + 1.7f) * 16f;
                    int limit = baseY + Mathf.RoundToInt(wave);
                    if (y <= limit)
                    {
                        float t = y / (float)Mathf.Max(1, limit);
                        texture.SetPixel(x, y, Color.Lerp(shadow, sand, t));
                    }
                }
            }
        }

        private static void DrawRocks(Texture2D texture, int width, int height)
        {
            DrawEllipse(texture, Mathf.RoundToInt(width * 0.18f), Mathf.RoundToInt(height * 0.14f), 95, 42, new Color(0.22f, 0.27f, 0.29f, 1f));
            DrawEllipse(texture, Mathf.RoundToInt(width * 0.78f), Mathf.RoundToInt(height * 0.13f), 120, 48, new Color(0.2f, 0.24f, 0.25f, 1f));
            DrawEllipse(texture, Mathf.RoundToInt(width * 0.57f), Mathf.RoundToInt(height * 0.11f), 65, 28, new Color(0.24f, 0.28f, 0.3f, 1f));
        }

        private static void DrawCorals(Texture2D texture, int width, int height)
        {
            DrawCoral(texture, Mathf.RoundToInt(width * 0.28f), Mathf.RoundToInt(height * 0.13f), new Color(0.95f, 0.3f, 0.38f, 1f));
            DrawCoral(texture, Mathf.RoundToInt(width * 0.68f), Mathf.RoundToInt(height * 0.12f), new Color(1f, 0.52f, 0.16f, 1f));
            DrawCoral(texture, Mathf.RoundToInt(width * 0.86f), Mathf.RoundToInt(height * 0.12f), new Color(0.58f, 0.36f, 0.88f, 1f));
        }

        private static void DrawSeaweed(Texture2D texture, int width, int height)
        {
            DrawSeaweedCluster(texture, Mathf.RoundToInt(width * 0.1f), Mathf.RoundToInt(height * 0.11f));
            DrawSeaweedCluster(texture, Mathf.RoundToInt(width * 0.44f), Mathf.RoundToInt(height * 0.1f));
            DrawSeaweedCluster(texture, Mathf.RoundToInt(width * 0.92f), Mathf.RoundToInt(height * 0.1f));
        }

        private static void DrawBubbles(Texture2D texture, int width, int height)
        {
            Color bubble = new Color(0.8f, 0.96f, 1f, 0.7f);
            DrawCircle(texture, Mathf.RoundToInt(width * 0.38f), Mathf.RoundToInt(height * 0.58f), 9, bubble);
            DrawCircle(texture, Mathf.RoundToInt(width * 0.43f), Mathf.RoundToInt(height * 0.68f), 6, bubble);
            DrawCircle(texture, Mathf.RoundToInt(width * 0.62f), Mathf.RoundToInt(height * 0.5f), 7, bubble);
            DrawCircle(texture, Mathf.RoundToInt(width * 0.75f), Mathf.RoundToInt(height * 0.65f), 5, bubble);
        }

        private static void DrawCoral(Texture2D texture, int rootX, int rootY, Color color)
        {
            DrawLine(texture, rootX, rootY, rootX, rootY + 74, 8, color);
            DrawLine(texture, rootX, rootY + 34, rootX - 32, rootY + 62, 7, color);
            DrawLine(texture, rootX, rootY + 42, rootX + 34, rootY + 76, 7, color);
            DrawLine(texture, rootX - 10, rootY + 16, rootX - 42, rootY + 36, 6, color);
            DrawLine(texture, rootX + 9, rootY + 22, rootX + 45, rootY + 40, 6, color);
        }

        private static void DrawSeaweedCluster(Texture2D texture, int rootX, int rootY)
        {
            Color seaweed = new Color(0.08f, 0.55f, 0.32f, 1f);
            DrawLine(texture, rootX, rootY, rootX - 22, rootY + 100, 7, seaweed);
            DrawLine(texture, rootX + 22, rootY, rootX + 10, rootY + 86, 7, seaweed);
            DrawLine(texture, rootX - 18, rootY, rootX - 4, rootY + 78, 6, seaweed);
            DrawLine(texture, rootX + 42, rootY, rootX + 62, rootY + 92, 6, seaweed);
        }

        private static void DrawEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
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

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            DrawEllipse(texture, centerX, centerY, radius, radius, color);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCircle(texture, x0, y0, thickness, color);
                if (x0 == x1 && y0 == y1) break;

                int e2 = err * 2;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static Material CreateUnlitTextureMaterial(Texture texture)
        {
            Shader shader = Shader.Find("Unlit/Texture");
            Material material = shader != null ? new Material(shader) : CreateMaterial("Generated_Backdrop_Fallback", Color.white);
            material.name = "Generated_Underwater_Backdrop_Material";
            material.mainTexture = texture;
            return material;
        }
    }
}
