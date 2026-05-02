using UnityEngine;
using System;

namespace ARFishApp.Interaction
{
    [RequireComponent(typeof(LineRenderer))]
    public class HotspotNode : MonoBehaviour
    {
        public static event Action<HotspotNode> OnAnyHotspotTapped;

        public string organName;
        [TextArea] public string infoDescription;
        public Action<HotspotNode> OnHotspotTapped;

        [Header("UI Visuals")]
        public Transform uiPanelLocation; // Where the descriptive card pops up
        private LineRenderer lineRenderer;
        private bool isFocusActive = false;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2; // Line drawn between Organ -> UI Text Box
            lineRenderer.enabled = false;
        }

        private void OnMouseDown()
        {
            ApplyTapInteraction();
        }

        public void ApplyRemoteTap()
        {
            ApplyTapInteraction();
        }

        private void ApplyTapInteraction()
        {
            OnHotspotTapped?.Invoke(this);
            OnAnyHotspotTapped?.Invoke(this);
            ToggleInfoUIPanel();
        }

        private void ToggleInfoUIPanel()
        {
            isFocusActive = !isFocusActive;
            lineRenderer.enabled = isFocusActive && uiPanelLocation != null;
            
            if (isFocusActive) 
                Debug.Log($"[Hotspot] Opened 3D UI Panel for: {organName}. Displaying: {infoDescription}");
        }

        private void Update()
        {
            // Dynamic UI Line drawing between the physical organ and tracking floating text panel
            if (isFocusActive && uiPanelLocation != null)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, uiPanelLocation.position);
            }
        }
    }
}
