namespace PhobiaReliefTherapy.Therapy
{
    public static class PhobiaCatalog
    {
        public static string Description(string phobia)
        {
            switch (phobia)
            {
                case "Height":
                    return "Acrophobia track. Environments: low balcony (Beginner), high bridge (Intermediate), mountain cliff (Advanced).";
                case "Darkness":
                    return "Nyctophobia track. Controlled darkness with gradual lighting reduction and ambient audio cues.";
                case "Crowd":
                    return "Ochlophobia track. Small group (Beginner), medium crowd (Intermediate), large audience (Advanced).";
                default:
                    return "Select a therapy module to see its description and recommended starting level.";
            }
        }

        public static string RecommendedStart(string phobia)
        {
            return "AI selects Beginner, Intermediate, or Advanced from baseline heart rate and head movement. You do not pick the stage.";
        }

        public static string PreSessionInstructions()
        {
            return "Before you continue:\n" +
                   "1. Wear the Polar H10 strap snugly (wet the electrodes).\n" +
                   "2. Keep the headset tracking your head.\n" +
                   "3. During exposure, press A (right) or X (left) for Emergency / Safe Room.\n" +
                   "4. You can stay in the Safe Room if you feel unready.";
        }

        public static string ToDifficultyLabel(string stored)
        {
            switch (stored)
            {
                case "Medium":
                    return "Intermediate";
                case "High":
                    return "Advanced";
                default:
                    return "Beginner";
            }
        }

        public static string FromDifficultyLabel(string label)
        {
            switch (label)
            {
                case "Intermediate":
                    return "Medium";
                case "Advanced":
                    return "High";
                default:
                    return "Low";
            }
        }

        public static int StageFromDifficulty(string stored)
        {
            switch (stored)
            {
                case "Medium":
                    return 2;
                case "High":
                    return 3;
                default:
                    return 1;
            }
        }

        public static string DifficultyFromStage(int stage)
        {
            if (stage >= 3) return "High";
            if (stage == 2) return "Medium";
            return "Low";
        }
    }
}
