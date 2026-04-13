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
            SystemStateManager.Instance.ChangeState(ModuleType.Anatomy);
        }

        public void OnHabitatButtonClicked()
        {
            SystemStateManager.Instance.ChangeState(ModuleType.Habitat);
        }

        public void OnFeedingButtonClicked()
        {
            SystemStateManager.Instance.ChangeState(ModuleType.Feeding);
        }

        public void OnEcosystemButtonClicked()
        {
            SystemStateManager.Instance.ChangeState(ModuleType.Ecosystem);
        }
    }
}
