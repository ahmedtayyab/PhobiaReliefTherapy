using UnityEngine;
using TMPro;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.UI;
using PhobiaReliefTherapy.Theme;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Live AI overlay during exposure (UC-08 / UC-10 / §3.2.7).
    /// Panic score from HR + head movement/jerk decides: continue, lower stage, or return to Safe Room.
    /// </summary>
    public class AdaptiveExposureController : MonoBehaviour
    {
        public static AdaptiveExposureController Instance { get; private set; }
        public static event System.Action<int> StageChanged;

        private TextMeshProUGUI recText;
        private CanvasGroup recGroup;
        private float calmSeconds;
        private float lastStageChangeTime;
        private bool lowering;
        private bool returningToSafe;

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
                new GameObject("AdaptiveExposureController").AddComponent<AdaptiveExposureController>();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            ExposureSessionMonitor.EvaluationReady += OnEvaluation;
        }

        private void OnDisable()
        {
            ExposureSessionMonitor.EvaluationReady -= OnEvaluation;
        }

        public void BeginSessionOverlay()
        {
            returningToSafe = false;
            lowering = false;
            calmSeconds = 0f;
            lastStageChangeTime = Time.time;
            BuildHud();
        }

        public void EndSessionOverlay()
        {
            if (recText != null)
                Destroy(recText.gameObject);
            recText = null;
            recGroup = null;
        }

        private void BuildHud()
        {
            EndSessionOverlay();
            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            recText = ClinicalUIFactory.CreateText("AiRecText", canvas.transform, "", 22, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(recText.rectTransform, new Vector2(0.22f, 0.86f), new Vector2(0.78f, 0.96f));
            recGroup = recText.gameObject.AddComponent<CanvasGroup>();
            recGroup.alpha = 0f;
            recText.raycastTarget = false;
        }

        private void OnEvaluation(PanicDetectionService.PanicEvaluation evaluation)
        {
            UserData.AiStageRecommendation = evaluation.AiRecommendation;

            if (evaluation.PanicScore < 10f)
            {
                calmSeconds += 1f;
                if (calmSeconds >= 20f && UserData.CurrentStage < 3 && Time.time - lastStageChangeTime > 12f)
                {
                    ShowRecommendation("Calm and stable. AI recommends advancing to the next stage after this session.");
                    calmSeconds = 0f;
                }
                return;
            }

            calmSeconds = 0f;

            if (evaluation.Classification == "Panic Detected" && !lowering && Time.time - lastStageChangeTime > 8f)
            {
                LowerStage();
            }
        }

        private void LowerStage()
        {
            lowering = true;
            int previous = UserData.CurrentStage;
            int next = Mathf.Max(1, previous - 1);
            UserData.CurrentStage = next;
            UserData.SelectedDifficulty = PhobiaCatalog.DifficultyFromStage(next);
            lastStageChangeTime = Time.time;
            SessionMetrics.LastAiRecommendation = $"Repeat / reduce to Stage {next}";
            ShowRecommendation($"Stress rising. AI lowered intensity to Stage {next}.");
            TherapySessionHelper.ApplyDifficultyToSkyboxMaterial(RenderSettings.skybox);
            StageChanged?.Invoke(next);
            lowering = false;
        }

        public void HandleSeverePanic(PanicDetectionService.PanicEvaluation evaluation)
        {
            if (returningToSafe)
                return;

            returningToSafe = true;
            ShowRecommendation("Severe panic detected. Returning you slowly to the Safe Room.");
            SessionMetrics.LastAiRecommendation = evaluation.AiRecommendation;
            StartCoroutine(SlowReturn());
        }

        private System.Collections.IEnumerator SlowReturn()
        {
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            TherapySessionHelper.AbortToSafeRoom(true);
            EndSessionOverlay();
        }

        private void ShowRecommendation(string message)
        {
            if (recText == null)
                return;

            recText.text = message;
            recGroup.alpha = 1f;
            StopCoroutine(nameof(FadeRecommendation));
            StartCoroutine(FadeRecommendation());
        }

        private System.Collections.IEnumerator FadeRecommendation()
        {
            yield return new WaitForSeconds(3.5f);
            float t = 0f;
            while (t < 1f && recGroup != null)
            {
                t += Time.deltaTime;
                recGroup.alpha = 1f - t;
                yield return null;
            }
        }

    }
}
