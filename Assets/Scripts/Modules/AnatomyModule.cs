using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ARFishApp.Core;

namespace ARFishApp.Modules
{
    public class AnatomyModule : MonoBehaviour, IModule
    {
        [Header("X-Ray Shader & Advanced Clipping")]
        public Renderer skinRenderer;
        public List<Renderer> internalOrgans;
        public GameObject skeletonModel;
        
        [Tooltip("If true, it passes data directly to the GPU Fragment Shader for procedural pixel clipping instead of fading alpha.")]
        public bool useComputeShaderClipping = true;
        public string clippingPlaneProperty = "_ClipHeightAxis_Y";

        [Header("Animation Parameters")]
        public float scanExecutionSpeed = 1.2f;

        private Coroutine activeScanRoutine;

        private void Start()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged += HandleStateChanged;
            OnModuleDeactivated();
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null) SystemStateManager.Instance.OnStateChanged -= HandleStateChanged;
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
            activeScanRoutine = StartCoroutine(ProceduralCrossSectionScan(true));
            
            if (skeletonModel != null)
            {
                skeletonModel.SetActive(true);
                StartCoroutine(ExecuteBonesSkeletalAssembly());
            }
        }

        public void OnModuleDeactivated()
        {
            if (activeScanRoutine != null) StopCoroutine(activeScanRoutine);
            if (gameObject.activeInHierarchy)
                activeScanRoutine = StartCoroutine(ProceduralCrossSectionScan(false));
                
            if (skeletonModel != null) skeletonModel.SetActive(false);
            
            foreach(var organ in internalOrgans) if (organ != null) organ.gameObject.SetActive(false);
        }

        private IEnumerator ProceduralCrossSectionScan(bool isScanning)
        {
            if (skinRenderer == null) yield break;

            Material mat = skinRenderer.material;
            // Define the bounding box bounds for the 3D slicing threshold
            float currentHeight = isScanning ? 1.5f : -1.5f;
            float targetHeight = isScanning ? -1.5f : 1.5f;

            if (isScanning)
            {
                // Pre-warm physical structures into memory before scan crosses them
                foreach(var organ in internalOrgans) if (organ != null) organ.gameObject.SetActive(true);
            }

            while (Mathf.Abs(currentHeight - targetHeight) > 0.05f)
            {
                currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * scanExecutionSpeed);
                
                if (useComputeShaderClipping)
                {
                    // Real-time GPU Instruction: discard pixels outside the bounded mathematical Y-Plane 
                    mat.SetFloat(clippingPlaneProperty, currentHeight);
                }
                else
                {
                    // Legacy Fallback (Standard Renderer)
                    Color c = mat.color;
                    c.a = Mathf.Clamp01(Mathf.InverseLerp(-1.5f, 1.5f, currentHeight));
                    mat.color = c;
                }
                
                yield return null;
            }
        }

        private IEnumerator ExecuteBonesSkeletalAssembly()
        {
            // Simulate advanced organic biological 3D printing of the spine and bones
            Vector3 targetScale = Vector3.one; 
            skeletonModel.transform.localScale = new Vector3(1f, 0.01f, 1f);
            
            float t = 0;
            while(t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                // Mathematical ease-out curve formula (Cubic Out) for biological momentum visualization
                float biologicalEaseOut = 1f - Mathf.Pow(1f - t, 3f); 
                skeletonModel.transform.localScale = Vector3.Lerp(new Vector3(1f, 0.01f, 1f), targetScale, biologicalEaseOut);
                yield return null;
            }
        }
    }
}
