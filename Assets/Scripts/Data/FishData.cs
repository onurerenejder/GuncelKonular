using UnityEngine;

namespace ARFishApp.Data
{
    /// <summary>
    /// The Single Source of Truth for biological data.
    /// Used to configure UI and 3D visual parameters dynamically.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFishData", menuName = "ARFishApp/Fish Data", order = 1)]
    public class FishData : ScriptableObject
    {
        [Header("Identity")]
        public string FishName;
        public string ScientificName;
        [TextArea] public string GeneralDescription;

        [Header("Anatomy")]
        public bool HasBones = true;
        [TextArea] public string AnatomyDescription;

        [Header("Habitat")]
        public string HabitatType; // e.g. Coral Reef, Deep Sea
        public Color EnvironmentalLightColor = Color.white;

        [Header("Feeding & Ecosystem")]
        [TextArea] public string DietDescription;
        public string[] Predators;
        
        [Header("Audio")]
        public AudioClip NarrationAudioClip;
    }
}
