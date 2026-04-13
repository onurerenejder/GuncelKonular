using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARFishApp.Core;

namespace ARFishApp.AR
{
    /// <summary>
    /// This core script bridges AR Foundation Camera tracking with our internal Business Logic.
    /// Detects the target image and sets up the World Anchor for the fish.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARMarkerHandler : MonoBehaviour
    {
        private ARTrackedImageManager trackedImageManager;
        
        [Tooltip("The parent GameObject containing the Fish body, Modules, and Audio sources.")]
        public GameObject fishEntityContainer;

        private void Awake()
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
            if (fishEntityContainer != null)
            {
                fishEntityContainer.SetActive(false); // Hide initially until AR marker is found
            }
        }

        private void OnEnable() => trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        private void OnDisable() => trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
                UpdateFishPosition(trackedImage);

            foreach (var trackedImage in eventArgs.updated)
                UpdateFishPosition(trackedImage);

            foreach (var trackedImage in eventArgs.removed)
            {
                if (fishEntityContainer != null)
                    fishEntityContainer.SetActive(false);
                
                SystemStateManager.Instance.ChangeState(ModuleType.None);
            }
        }

        private void UpdateFishPosition(ARTrackedImage trackedImage)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                if (fishEntityContainer != null && !fishEntityContainer.activeSelf)
                {
                    fishEntityContainer.SetActive(true);
                    
                    // Boot up the first module visually when target acquired
                    if (SystemStateManager.Instance.CurrentModule == ModuleType.None)
                    {
                        SystemStateManager.Instance.ChangeState(ModuleType.Anatomy);
                    }
                }
                
                // Keep the AR entity physically bound to the marker's 3D coordinates
                if (fishEntityContainer != null)
                {
                    fishEntityContainer.transform.position = trackedImage.transform.position;
                    fishEntityContainer.transform.rotation = trackedImage.transform.rotation;
                }
            }
        }
    }
}
