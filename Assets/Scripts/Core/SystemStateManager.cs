using System;
using UnityEngine;

namespace ARFishApp.Core
{
    public enum ModuleType
    {
        None,
        Anatomy,
        Habitat,
        Feeding,
        InterspeciesRelations,
        PredatorPrey,
        Quiz,
        Portal
    }

    /// <summary>
    /// Core Engine - State Management. 
    /// Manages the transition between different educational modules independently of the AR rendering loop.
    /// </summary>
    public class SystemStateManager : MonoBehaviour
    {
        public static SystemStateManager Instance { get; private set; }

        public event Action<ModuleType> OnStateChanged;

        [SerializeField] private ModuleType currentModule = ModuleType.None;
        
        public ModuleType CurrentModule => currentModule;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // Uncomment if keeping across massive scene loads
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ChangeState(ModuleType newModule)
        {
            if (currentModule == newModule) return;

            Debug.Log($"[State Manager] Transitioning from {currentModule} to {newModule}");
            currentModule = newModule;
            OnStateChanged?.Invoke(currentModule);
        }
    }
}
