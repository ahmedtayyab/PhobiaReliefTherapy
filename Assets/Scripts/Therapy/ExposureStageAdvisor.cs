using UnityEngine;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers.HeartRate;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Selects exposure stage from baseline vitals and the user's requested difficulty (SRS §3.2.7 / UC-08).
    /// Live Polar rest is typically 70–95 BPM; stage gates use that range, not a low-70s lab rest.
    /// </summary>
    public static class ExposureStageAdvisor
    {
        public static void AssignStageFromBaseline(int baselineHeartRate, float headMovementScore)
        {
            int aiStage;
            bool lowMovement = headMovementScore <= 16f;
            bool moderateMovement = headMovementScore <= 28f;

            if (baselineHeartRate <= HeartRateBleConstants.StageAdvancedMaxBpm && lowMovement)
                aiStage = 3;
            else if (baselineHeartRate <= HeartRateBleConstants.StageIntermediateMaxBpm && moderateMovement)
                aiStage = 2;
            else
                aiStage = 1;

            int requested = PhobiaCatalog.StageFromDifficulty(UserData.SelectedDifficulty);
            int finalStage = aiStage;
            if (UserData.AllowManualStageSelection)
                finalStage = Mathf.Min(aiStage, Mathf.Max(1, requested));

            UserData.CurrentStage = finalStage;
            UserData.SelectedDifficulty = PhobiaCatalog.DifficultyFromStage(finalStage);

            string safetyNote = UserData.AllowManualStageSelection && finalStage < requested
                ? " Baseline vitals were higher than the requested level, so AI reduced intensity."
                : " Chosen from baseline heart rate and head movement.";

            UserData.AiStageRecommendation =
                $"AI selected Stage {finalStage} ({PhobiaCatalog.ToDifficultyLabel(UserData.SelectedDifficulty)})." + safetyNote;

            Debug.Log($"ExposureStageAdvisor: Stage {UserData.CurrentStage} ({UserData.SelectedDifficulty}) — {UserData.AiStageRecommendation}");
        }
    }
}
