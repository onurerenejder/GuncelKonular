using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using ARFishApp.Core;
using ARFishApp.Data;

namespace ARFishApp.Modules
{
    public class FeedingModule : MonoBehaviour, IModule
    {
        private struct EnergyPulseData
        {
            public Transform pulseTransform;
            public int linkIndex;
            public float phaseOffset;
        }

        public enum DietType { Carnivore, Herbivore }

        [Header("Procedural Inverse Kinematics (IK Spine Bending)")]
        [Tooltip("The actual neck/spine bone of the 3D model. We procedurally control its local rotation to track the food.")]
        public Transform headBone;
        public float ikTrackingSpeed = 5.0f;
        public float maxProceduralBendAngle = 45f;

        [Header("Feeding Logic Engine")]
        public DietType fishDiet;
        public Animator jawAnimator;
        public Transform mouthSocket;

        [Header("Procedural Physics & Particle Interactions")]
        public GameObject meatPreyPrefab;
        public GameObject vegetationPrefab;
        public ParticleSystem hitBloodMuzzle;
        public ParticleSystem hitAlgaeMuzzle;

        [Header("Food Chain Visualization")]
        [Tooltip("Optional source of ecological data. If empty, the module will use fallback labels.")]
        public FishData fishData;
        [Tooltip("Container transform for spawning food chain visuals. Defaults to this object if empty.")]
        public Transform foodChainRoot;
        [Tooltip("Simple marker prefab for each level in the chain. If null, spheres are generated.")]
        public GameObject foodChainNodePrefab;
        public Material foodChainLineMaterial;
        public float chainNodeSpacing = 0.55f;
        public float chainVerticalAmplitude = 0.25f;
        public Vector3 chainLocalOffset = new Vector3(0f, 1.2f, 0f);
        [Tooltip("Energy transfer pulse speed on each food chain link.")]
        public float energyFlowSpeed = 0.8f;
        [Tooltip("Number of moving energy pulses per link.")]
        public int pulsesPerLink = 2;
        [Tooltip("Scale of directional arrowheads on each chain link.")]
        public float arrowHeadScale = 0.08f;
        [Tooltip("Highlight color for the current fish trophic node.")]
        public Color fishNodeHighlightColor = new Color(1f, 0.92f, 0.3f, 1f);

        private readonly List<GameObject> chainNodes = new List<GameObject>();
        private readonly List<Renderer> chainNodeRenderers = new List<Renderer>();
        private readonly List<LineRenderer> chainLines = new List<LineRenderer>();
        private readonly List<GameObject> chainArrowheads = new List<GameObject>();
        private readonly List<EnergyPulseData> energyPulses = new List<EnergyPulseData>();
        private int fishNodeIndex = -1;
        private GameObject currentFoodTarget;

        private SimplePool _meatPool;
        private SimplePool _vegetationPool;
        private SimplePool _bloodPool;
        private SimplePool _algaePool;

        private void Start()
        {
            if (fishData == null)
            {
                FishEntityController entityController = GetComponentInParent<FishEntityController>();
                if (entityController != null) fishData = entityController.fishDataConfig;
            }

            InitializePools();
            FishSelectionManager.OnFishSelected += HandleFishSelected;
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (_meatPool != null) _meatPool.Dispose();
            if (_vegetationPool != null) _vegetationPool.Dispose();
            if (_bloodPool != null) _bloodPool.Dispose();
            if (_algaePool != null) _algaePool.Dispose();

            ClearFoodChainVisualization();
            FishSelectionManager.OnFishSelected -= HandleFishSelected;
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleFishSelected(SelectableFish selectedFish)
        {
            fishData = selectedFish != null ? selectedFish.fishData : null;
        }

        private void HandleStateChanged(ModuleType newType)
        {
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Feeding;

        public void OnModuleActivated()
        {
            StopAllCoroutines();
            BuildFoodChainVisualization();
            StartCoroutine(FeedingSequenceController());
        }

        public void OnModuleDeactivated()
        {
            ReturnFoodToPool();
            if (jawAnimator != null) jawAnimator.SetBool("IsBiting", false);

            // Release IK overrides mathematically
            if (headBone != null) headBone.localRotation = Quaternion.identity;

            ClearFoodChainVisualization();
        }

        /// <summary>
        /// LateUpdate is strictly required to override animation system transforms.
        /// Here we calculate an IK Lookat constraint manually towards the food.
        /// </summary>
        private void LateUpdate()
        {
            if (SystemStateManager.Instance != null && SystemStateManager.Instance.CurrentModule == GetModuleType() && currentFoodTarget != null && headBone != null)
            {
                // Vector geometry to point spine/head bone at the food
                Vector3 directionToFood = (currentFoodTarget.transform.position - headBone.position).normalized;
                Quaternion targetLookRotation = Quaternion.LookRotation(directionToFood, transform.up);

                // Clamping the spherical rotation to strictly avoid breaking the spine's topology
                float deviationAngle = Quaternion.Angle(transform.rotation, targetLookRotation);
                if (deviationAngle <= maxProceduralBendAngle)
                {
                    headBone.rotation = Quaternion.Slerp(headBone.rotation, targetLookRotation, Time.deltaTime * ikTrackingSpeed);
                }
            }

            UpdateEnergyFlowPulses();
        }

        private IEnumerator FeedingSequenceController()
        {
            // Calculate a completely randomized spawn zone within the fish's vision
            Vector3 spawnPoint = transform.position + (transform.forward * 2.0f) + (transform.right * Random.Range(-0.8f, 0.8f));

            if (fishDiet == DietType.Carnivore && _meatPool != null)
                currentFoodTarget = _meatPool.Get(spawnPoint, Quaternion.identity);
            else if (fishDiet == DietType.Herbivore && _vegetationPool != null)
                currentFoodTarget = _vegetationPool.Get(spawnPoint, Quaternion.identity);

            // Wait 1.5 seconds, specifically allowing the IK math to bend the fish toward the food
            yield return new WaitForSeconds(1.5f);

            if (jawAnimator != null) jawAnimator.SetTrigger("BiteTrigger");

            // Execute Burst Translation
            float executionTimeRemaining = 0.25f;
            Vector3 defaultAnchorPos = transform.position;

            while (executionTimeRemaining > 0)
            {
                transform.position = Vector3.Lerp(transform.position, spawnPoint, 0.15f);
                executionTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            // Cleanup & Digestion phase
            if (currentFoodTarget != null)
            {
                ReturnFoodToPool();

                if (mouthSocket != null)
                {
                    if (fishDiet == DietType.Carnivore && _bloodPool != null)
                    {
                        GameObject bloodGo = _bloodPool.Get(mouthSocket.position, Quaternion.identity);
                        ParticleSystem bps = bloodGo.GetComponent<ParticleSystem>();
                        if (bps != null) bps.Play();
                        StartCoroutine(ReturnParticleToPool(bloodGo, _bloodPool));
                    }
                    else if (fishDiet == DietType.Herbivore && _algaePool != null)
                    {
                        GameObject algaeGo = _algaePool.Get(mouthSocket.position, Quaternion.identity);
                        ParticleSystem aps = algaeGo.GetComponent<ParticleSystem>();
                        if (aps != null) aps.Play();
                        StartCoroutine(ReturnParticleToPool(algaeGo, _algaePool));
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Recovery algorithm back to center anchor
            float returnTimeLimit = 0.6f;
            while (returnTimeLimit > 0)
            {
                float polynomialEase = 1f - (returnTimeLimit / 0.6f);
                transform.position = Vector3.Lerp(transform.position, defaultAnchorPos, polynomialEase * 0.1f);
                returnTimeLimit -= Time.deltaTime;
                yield return null;
            }
        }

        private void BuildFoodChainVisualization()
        {
            ClearFoodChainVisualization();

            string[] chainLabels = ResolveFoodChain();
            if (chainLabels.Length < 2) return;

            Transform root = foodChainRoot != null ? foodChainRoot : transform;

            for (int i = 0; i < chainLabels.Length; i++)
            {
                GameObject node = CreateChainNode(root, i, chainLabels[i]);
                chainNodes.Add(node);
                CacheNodeRenderer(node);

                if (i > 0)
                {
                    LineRenderer line = CreateChainLink(root, chainNodes[i - 1].transform.position, node.transform.position);
                    chainLines.Add(line);
                }
            }

            fishNodeIndex = ResolveFishNodeIndex(chainLabels);
            ApplyFishNodeHighlight();
            BuildLinkArrowheads(root);
            BuildEnergyFlowPulses(root);
        }

        private string[] ResolveFoodChain()
        {
            if (fishData != null && fishData.FoodChain != null && fishData.FoodChain.Length >= 2)
            {
                List<string> sanitized = new List<string>();
                for (int i = 0; i < fishData.FoodChain.Length; i++)
                {
                    string level = fishData.FoodChain[i];
                    if (!string.IsNullOrWhiteSpace(level))
                    {
                        sanitized.Add(level.Trim());
                    }
                }

                if (sanitized.Count >= 2) return sanitized.ToArray();
            }

            if (fishDiet == DietType.Herbivore)
            {
                return new[] { "Fitoplankton", "Alg", "Balık", "Yırtıcı Balık" };
            }

            return new[] { "Zooplankton", "Küçük Balık", "Balık", "Apex Yırtıcı" };
        }

        private GameObject CreateChainNode(Transform root, int index, string levelLabel)
        {
            Vector3 localPosition = chainLocalOffset +
                                    (transform.right * (index * chainNodeSpacing)) +
                                    (transform.up * Mathf.Sin(index * 0.8f) * chainVerticalAmplitude);

            GameObject node = foodChainNodePrefab != null
                ? Instantiate(foodChainNodePrefab, root)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            node.name = $"FoodChainNode_{index}_{levelLabel}";
            node.transform.SetParent(root, false);
            node.transform.localPosition = localPosition;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one * 0.14f;

            if (foodChainNodePrefab == null)
            {
                Renderer renderer = node.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.Lerp(new Color(0.2f, 0.9f, 0.7f), new Color(1f, 0.6f, 0.2f), index * 0.25f);
                }
            }

            CreateWorldSpaceLabel(node.transform, levelLabel, index);

            return node;
        }

        private void CacheNodeRenderer(GameObject node)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null) chainNodeRenderers.Add(renderer);
        }

        private void CreateWorldSpaceLabel(Transform parentNode, string levelLabel, int chainIndex)
        {
            GameObject canvasObject = new GameObject("LabelCanvas");
            canvasObject.transform.SetParent(parentNode, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.0035f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 12f;
            canvasObject.AddComponent<GraphicRaycaster>();
            WorldSpaceBillboard billboard = canvasObject.AddComponent<WorldSpaceBillboard>();
            billboard.targetCamera = Camera.main;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(280f, 90f);

            GameObject textObject = new GameObject("LabelText");
            textObject.transform.SetParent(canvasObject.transform, false);

            Text text = textObject.AddComponent<Text>();
            text.text = levelLabel;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 30;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Compact trophic-level hint to make energy hierarchy explicit.
            string levelPrefix = chainIndex == 0 ? "Üretici/Temel" : $"Trofik {chainIndex}";
            string ecoHint = ResolveLevelDescription(chainIndex);
            text.text = $"{levelPrefix}\n{levelLabel}\n{ecoHint}";

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private string ResolveLevelDescription(int chainIndex)
        {
            if (chainIndex == 0) return "Enerji girişi";
            if (fishNodeIndex == chainIndex) return "Bu türün konumu";
            if (chainIndex == fishNodeIndex + 1) return "Üst avcı baskısı";
            if (chainIndex < fishNodeIndex) return "Besin kaynağı";
            return "Enerji aktarımı";
        }

        private LineRenderer CreateChainLink(Transform root, Vector3 startPosition, Vector3 endPosition)
        {
            GameObject lineObject = new GameObject("FoodChainLink");
            lineObject.transform.SetParent(root, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, startPosition);
            line.SetPosition(1, endPosition);
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;

            if (foodChainLineMaterial != null)
            {
                line.material = foodChainLineMaterial;
            }

            line.startColor = new Color(0.4f, 0.95f, 0.8f, 0.9f);
            line.endColor = new Color(1f, 0.75f, 0.4f, 0.9f);
            return line;
        }

        private void BuildLinkArrowheads(Transform root)
        {
            for (int i = 0; i < chainLines.Count; i++)
            {
                LineRenderer line = chainLines[i];
                if (line == null) continue;

                Vector3 start = line.GetPosition(0);
                Vector3 end = line.GetPosition(1);
                Vector3 direction = (end - start).normalized;
                if (direction.sqrMagnitude < 0.0001f) continue;

                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arrow.name = $"FoodChainArrow_{i}";
                arrow.transform.SetParent(root, false);
                arrow.transform.position = Vector3.Lerp(start, end, 0.88f);
                arrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                arrow.transform.localScale = new Vector3(arrowHeadScale * 0.4f, arrowHeadScale, arrowHeadScale * 0.4f);

                Collider col = arrow.GetComponent<Collider>();
                if (col != null) Destroy(col);
                Renderer renderer = arrow.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(1f, 0.75f, 0.4f, 0.95f);

                chainArrowheads.Add(arrow);
            }
        }

        private int ResolveFishNodeIndex(string[] chainLabels)
        {
            if (chainLabels == null || chainLabels.Length == 0) return -1;

            if (fishData != null && !string.IsNullOrWhiteSpace(fishData.FishName))
            {
                for (int i = 0; i < chainLabels.Length; i++)
                {
                    if (chainLabels[i].ToLowerInvariant().Contains(fishData.FishName.ToLowerInvariant()))
                        return i;
                }
            }

            for (int i = 0; i < chainLabels.Length; i++)
            {
                string normalized = chainLabels[i].ToLowerInvariant();
                if (normalized.Contains("balık") || normalized.Contains("fish")) return i;
            }

            return Mathf.Clamp(chainLabels.Length / 2, 0, chainLabels.Length - 1);
        }

        private void ApplyFishNodeHighlight()
        {
            if (fishNodeIndex < 0 || fishNodeIndex >= chainNodes.Count) return;

            GameObject node = chainNodes[fishNodeIndex];
            if (node != null) node.transform.localScale = Vector3.one * 0.19f;

            if (fishNodeIndex < chainNodeRenderers.Count && chainNodeRenderers[fishNodeIndex] != null)
            {
                chainNodeRenderers[fishNodeIndex].material.color = fishNodeHighlightColor;
            }
        }

        private void BuildEnergyFlowPulses(Transform root)
        {
            for (int i = 0; i < chainLines.Count; i++)
            {
                LineRenderer link = chainLines[i];
                if (link == null) continue;

                int pulseCount = Mathf.Max(1, pulsesPerLink);
                for (int p = 0; p < pulseCount; p++)
                {
                    GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pulse.name = $"EnergyPulse_{i}_{p}";
                    pulse.transform.SetParent(root, false);
                    pulse.transform.localScale = Vector3.one * 0.05f;

                    Collider pulseCollider = pulse.GetComponent<Collider>();
                    if (pulseCollider != null) Destroy(pulseCollider);

                    Renderer pulseRenderer = pulse.GetComponent<Renderer>();
                    if (pulseRenderer != null) pulseRenderer.material.color = new Color(1f, 0.95f, 0.4f, 0.95f);

                    // Encode link index and phase on localPosition for lightweight update bookkeeping.
                    EnergyPulseData pulseData = new EnergyPulseData
                    {
                        pulseTransform = pulse.transform,
                        linkIndex = i,
                        phaseOffset = p / (float)pulseCount
                    };
                    energyPulses.Add(pulseData);
                }
            }
        }

        private void UpdateEnergyFlowPulses()
        {
            if (chainLines.Count == 0 || energyPulses.Count == 0) return;

            float now = Time.time * Mathf.Max(0.01f, energyFlowSpeed);

            for (int i = 0; i < energyPulses.Count; i++)
            {
                EnergyPulseData pulseData = energyPulses[i];
                Transform pulse = pulseData.pulseTransform;
                if (pulse == null) continue;

                int linkIndex = pulseData.linkIndex;
                if (linkIndex < 0 || linkIndex >= chainLines.Count || chainLines[linkIndex] == null) continue;

                LineRenderer link = chainLines[linkIndex];
                float phase = pulseData.phaseOffset;
                float t = Mathf.Repeat(now + phase, 1f);

                Vector3 start = link.GetPosition(0);
                Vector3 end = link.GetPosition(1);
                pulse.position = Vector3.Lerp(start, end, t);
            }
        }

        private void InitializePools()
        {
            if (meatPreyPrefab != null) _meatPool = new SimplePool(meatPreyPrefab, transform);
            if (vegetationPrefab != null) _vegetationPool = new SimplePool(vegetationPrefab, transform);
            if (hitBloodMuzzle != null) _bloodPool = new SimplePool(hitBloodMuzzle.gameObject, transform, 2);
            if (hitAlgaeMuzzle != null) _algaePool = new SimplePool(hitAlgaeMuzzle.gameObject, transform, 2);
        }

        private void ReturnFoodToPool()
        {
            if (currentFoodTarget == null) return;
            if (fishDiet == DietType.Carnivore && _meatPool != null)
                _meatPool.Return(currentFoodTarget);
            else if (fishDiet == DietType.Herbivore && _vegetationPool != null)
                _vegetationPool.Return(currentFoodTarget);
            else
                Destroy(currentFoodTarget);
            currentFoodTarget = null;
        }

        private IEnumerator ReturnParticleToPool(GameObject particleGo, SimplePool pool)
        {
            ParticleSystem ps = particleGo.GetComponent<ParticleSystem>();
            if (ps != null) yield return new WaitWhile(() => ps.IsAlive(true));
            pool.Return(particleGo);
        }

        private void ClearFoodChainVisualization()
        {
            for (int i = 0; i < chainNodes.Count; i++)
            {
                if (chainNodes[i] != null) Destroy(chainNodes[i]);
            }

            for (int i = 0; i < chainLines.Count; i++)
            {
                if (chainLines[i] != null) Destroy(chainLines[i].gameObject);
            }

            for (int i = 0; i < energyPulses.Count; i++)
            {
                if (energyPulses[i].pulseTransform != null) Destroy(energyPulses[i].pulseTransform.gameObject);
            }

            for (int i = 0; i < chainArrowheads.Count; i++)
            {
                if (chainArrowheads[i] != null) Destroy(chainArrowheads[i]);
            }

            chainNodes.Clear();
            chainNodeRenderers.Clear();
            chainLines.Clear();
            chainArrowheads.Clear();
            energyPulses.Clear();
            fishNodeIndex = -1;
        }

        private sealed class SimplePool
        {
            private readonly Stack<GameObject> _stack = new Stack<GameObject>();
            private readonly GameObject _prefab;
            private readonly Transform _parent;

            public SimplePool(GameObject prefab, Transform parent, int warmCount = 1)
            {
                _prefab = prefab;
                _parent = parent;
                for (int i = 0; i < warmCount; i++)
                    _stack.Push(CreateInstance());
            }

            public GameObject Get(Vector3 pos, Quaternion rot)
            {
                GameObject go = _stack.Count > 0 ? _stack.Pop() : CreateInstance();
                go.transform.SetPositionAndRotation(pos, rot);
                go.SetActive(true);
                return go;
            }

            public void Return(GameObject go)
            {
                if (go == null) return;
                go.SetActive(false);
                go.transform.SetParent(_parent, false);
                _stack.Push(go);
            }

            public void Dispose()
            {
                while (_stack.Count > 0)
                {
                    GameObject go = _stack.Pop();
                    if (go != null) Object.Destroy(go);
                }
            }

            private GameObject CreateInstance()
            {
                GameObject go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                return go;
            }
        }
    }
}
