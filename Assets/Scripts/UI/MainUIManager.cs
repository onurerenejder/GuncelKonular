using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.UI
{
    /// <summary>
    /// Connects UI Buttons to the Core Engine State Manager.
    /// Completely isolated from 3D logic.
    /// </summary>
    public class MainUIManager : MonoBehaviour
    {
        public void OnAnatomyButtonClicked()
        {
            ChangeStateIfReady(ModuleType.Anatomy);
        }

        public void OnHabitatButtonClicked()
        {
            ChangeStateIfReady(ModuleType.Habitat);
        }

        public void OnFeedingButtonClicked()
        {
            ChangeStateIfReady(ModuleType.Feeding);
        }

        public void OnInterspeciesButtonClicked()
        {
            ChangeStateIfReady(ModuleType.InterspeciesRelations);
        }

        public void OnPredatorPreyButtonClicked()
        {
            ChangeStateIfReady(ModuleType.PredatorPrey);
        }

        public void OnQuizButtonClicked()
        {
            ChangeStateIfReady(ModuleType.Quiz);
        }

        public void OnPortalButtonClicked()
        {
            ChangeStateIfReady(ModuleType.Portal);
        }

        private void ChangeStateIfReady(ModuleType moduleType)
        {
            if (SystemStateManager.Instance == null)
            {
                Debug.LogWarning($"[MainUIManager] Cannot switch to {moduleType}: SystemStateManager is missing.");
                return;
            }

            SystemStateManager.Instance.ChangeState(moduleType);
        }
    }
}
