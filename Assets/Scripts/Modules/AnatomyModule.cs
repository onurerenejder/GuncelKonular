using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    [System.Serializable]
    public class OrganSystem
    {
        public string systemName;
        public Renderer organRenderer;
        public bool isPulsating;
        public float pulseRate = 1.0f;
        public float pulseMagnitude = 0.05f;
        [HideInInspector] public Vector3 originalScale;
    }

    public class AnatomyModule : MonoBehaviour, IModule
    {
        [Header("X-Ray Compute Architecture")]
        public Renderer skinRenderer;
        public GameObject skeletonModel;
        public List<OrganSystem> biologicalSystems;
        
        [Tooltip("Direct GPU Instruction pass for boolean occlusion.")]
        public bool useComputeShaderClipping = true;
        public string clippingPlaneProperty = "_ClipHeightAxis_Y";

        [Header("Runtime Simulation Physics")]
        public float scanExecutionSpeed = 1.2f;
        private Coroutine activeScanRoutine;
        private List<Coroutine> pulseCoroutines = new List<Coroutine>();

        private void Start()
        {
            foreach (var organ in biologicalSystems)
            {
                if (organ.organRenderer != null)
                {
                    organ.originalScale = organ.organRenderer.transform.localScale;
                }
            }

            if (SystemStateManager.Instance != null)
                SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
                
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
                SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ModuleType newType)
        {
            if (newType == GetModuleType()) OnModuleActivated();
            else OnModuleDeactivated();
        }

        public ModuleType GetModuleType() => ModuleType.Anatomy;

        public void OnModuleActivated()
        {
            if (activeScanRoutine != null) StopCoroutine(activeScanRoutine);
            activeScanRoutine = StartCoroutine(ProceduralBiologicalScan(true));
            
            if (skeletonModel != null)
            {
                skeletonModel.SetActive(true);
                StartCoroutine(ExecuteBonesSkeletalAssembly());
            }

            // Engage Biological rhythms
            foreach(var organ in biologicalSystems)
            {
                if (organ.isPulsating && organ.organRenderer != null)
                {
                    pulseCoroutines.Add(StartCoroutine(BiologicalPulseSimulation(organ)));
                }
            }
        }

        public void OnModuleDeactivated()
        {
            if (activeScanRoutine != null) StopCoroutine(activeScanRoutine);
            if (gameObject.activeInHierarchy) activeScanRoutine = StartCoroutine(ProceduralBiologicalScan(false));
                
            if (skeletonModel != null) skeletonModel.SetActive(false);
            
            foreach (var c in pulseCoroutines) if (c != null) StopCoroutine(c);
            pulseCoroutines.Clear();

            foreach(var organ in biologicalSystems) 
            {
                if (organ.organRenderer != null) 
                {
                    organ.organRenderer.gameObject.SetActive(false);
                    organ.organRenderer.transform.localScale = organ.originalScale;
                }
            }
        }

        /// <summary>
        /// Highly threaded coroutine to simulate Heartbeats or breathing Gills using sine-wave oscillations.
        /// </summary>
        private IEnumerator BiologicalPulseSimulation(OrganSystem organ)
        {
            while (true)
            {
                // Sine wave based continuous smooth expansion and contraction
                float dynamicScale = 1.0f + Mathf.Sin(Time.time * Mathf.PI * 2f * organ.pulseRate) * organ.pulseMagnitude;
                organ.organRenderer.transform.localScale = organ.originalScale * dynamicScale;
                yield return (null);
            }
        }

        private IEnumerator ProceduralBiologicalScan(bool isScanning)
        {
            if (skinRenderer == null) yield break;

            Material mat = skinRenderer.material;
            float currentHeight = isScanning ? 1.5f : -1.5f;
            float targetHeight = isScanning ? -1.5f : 1.5f;

            if (isScanning)
            {
                foreach(var organ in biologicalSystems) 
                    if (organ.organRenderer != null) organ.organRenderer.gameObject.SetActive(true);
            }

            while (Mathf.Abs(currentHeight - targetHeight) > 0.05f)
            {
                currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * scanExecutionSpeed);
                
                if (useComputeShaderClipping)
                {
                    mat.SetFloat(clippingPlaneProperty, currentHeight);
                }
                else
                {
                    Color c = mat.color;
                    c.a = Mathf.Clamp01(Mathf.InverseLerp(-1.5f, 1.5f, currentHeight));
                    mat.color = c;
                }
                yield return null;
            }
        }

        private IEnumerator ExecuteBonesSkeletalAssembly()
        {
            Vector3 targetScale = Vector3.one; 
            skeletonModel.transform.localScale = new Vector3(1f, 0.01f, 1f);
            
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                float easeOut = 1f - Mathf.Pow(1f - t, 3f); 
                skeletonModel.transform.localScale = Vector3.Lerp(new Vector3(1f, 0.01f, 1f), targetScale, easeOut);
                yield return null;
            }
        }
    }
}
