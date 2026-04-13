using UnityEngine;

namespace ARFishApp.Core
{
    /// <summary>
    /// Base interface ensuring Low Coupling. 
    /// Any new module (e.g., Anatomy, Feeding) must implement this.
    /// </summary>
    public interface IModule
    {
        ModuleType GetModuleType();
        void OnModuleActivated();
        void OnModuleDeactivated();
    }
}
