using System;

namespace ARFishApp.Data
{
    [Serializable]
    public class FishQuizItem
    {
        public string question;
        public string answer;
    }

    [Serializable]
    public class FishJsonData
    {
        public string id;
        public string displayName;
        public string scientificName;
        public string general;
        public string anatomy;
        public string habitat;
        public string feeding;
        public string relations;
        public string predatorPrey;
        public FishQuizItem[] quiz;
    }
}
