using UnityEngine;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Therapy;

namespace PhobiaReliefTherapy.Testing
{
    /// <summary>
    /// Headless checks for SRS formulas and account rules. Runs in the Editor without a headset.
    /// Menu: Tools / PRT / Run SRS Logic Tests
    /// </summary>
    public static class SrsLogicTests
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/PRT/Run SRS Logic Tests")]
        public static void RunFromMenu()
        {
            string report = RunAll();
            Debug.Log(report);
            UnityEditor.EditorUtility.DisplayDialog("SRS Logic Tests", report, "OK");
        }
#endif

        public static string RunAll()
        {
            int passed = 0;
            int failed = 0;
            var log = new System.Text.StringBuilder();

            void Check(string name, bool ok, string detail = "")
            {
                if (ok)
                {
                    passed++;
                    log.AppendLine("PASS  " + name);
                }
                else
                {
                    failed++;
                    log.AppendLine("FAIL  " + name + "  " + detail);
                }
            }

            UserData.BaselineHeartRate = 75;
            SessionMetrics.Reset();
            SessionMetrics.BaselineHeadMovementScore = 10f;

            var calm = PanicDetectionService.Evaluate(76, 10f, 0f);
            Check("UC-10 calm stress < 10", calm.PanicScore < 10f && calm.Classification == "Calm Stress",
                $"score={calm.PanicScore:F2} class={calm.Classification}");

            var panic = PanicDetectionService.Evaluate(110, 80f, 12f);
            Check("UC-10 panic band 10-18 or higher", panic.PanicScore >= 10f,
                $"score={panic.PanicScore:F2} class={panic.Classification}");

            var severe = PanicDetectionService.Evaluate(116, 10f, 0f);
            Check("UC-10 ΔHR > 36 with a real rise can force Safe Room", severe.PanicScore >= 18f && severe.TriggerSafetyMechanism,
                $"score={severe.PanicScore:F2} rec={severe.AiRecommendation}");

            UserData.SelectedDifficulty = "High";
            ExposureStageAdvisor.AssignStageFromBaseline(95, 40f);
            Check("UC-08 AI will not start Advanced on elevated baseline", UserData.CurrentStage == 1,
                $"stage={UserData.CurrentStage}");

            UserData.SelectedDifficulty = "High";
            ExposureStageAdvisor.AssignStageFromBaseline(70, 2f);
            Check("UC-08 calm baseline can keep a high request", UserData.CurrentStage == 3,
                $"stage={UserData.CurrentStage}");

            UserData.SelectedDifficulty = "High";
            ExposureStageAdvisor.AssignStageFromBaseline(88, 6f);
            Check("UC-08 live Polar rest 88 BPM is still Advanced", UserData.CurrentStage == 3,
                $"stage={UserData.CurrentStage}");

            Check("UC-03 height description exists", PhobiaCatalog.Description("Height").Contains("balcony"));
            Check("UC-04/05/06 difficulty mapping",
                PhobiaCatalog.StageFromDifficulty("Low") == 1 &&
                PhobiaCatalog.StageFromDifficulty("Medium") == 2 &&
                PhobiaCatalog.StageFromDifficulty("High") == 3);

            LocalAccountStore.EnsureSeedAccounts();
            var admin = LocalAccountStore.TryLogin("admin@phobia.local", "admin123", out string adminErr);
            Check("UC-01 admin login", admin != null && admin.IsAdmin, adminErr);

            var user = LocalAccountStore.TryLogin("user@phobia.local", "user1234", out string userErr);
            Check("UC-01 user login", user != null && !user.IsAdmin, userErr);

            var bad = LocalAccountStore.TryLogin("user@phobia.local", "wrongpass", out string badErr);
            Check("UC-01 invalid password rejected", bad == null && !string.IsNullOrEmpty(badErr));

            string dupError;
            LocalAccountStore.Register("demo_user", "user@phobia.local", "user1234", false, out dupError);
            Check("UC-02 duplicate email rejected", dupError == "Email already registered.");

            string unique = "test-" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + "@phobia.local";
            var temp = LocalAccountStore.Register("temp_user", unique, "temp1234", false, out string regErr);
            Check("UC-02 register new user", temp != null, regErr);
            if (admin != null && temp != null)
            {
                bool suspended = LocalAccountStore.SetStatus(admin.Id, temp.Id, LocalAccountStore.SuspendedStatus, out string susErr);
                Check("UC-13 suspend account", suspended, susErr);
                var blocked = LocalAccountStore.TryLogin(unique, "temp1234", out string blockedErr);
                Check("UC-13 suspended user cannot login", blocked == null, blockedErr);
                LocalAccountStore.DeleteAccount(admin.Id, temp.Id, out _);
            }

            log.Insert(0, $"SRS logic tests: {passed} passed, {failed} failed\n\n");
            return log.ToString();
        }
    }
}
