using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Bootstrap;

namespace PhobiaReliefTherapy.Admin
{
    /// <summary>
    /// Administrator dashboard (SRS §3.2.14, UC-12).
    /// </summary>
    public class AdminDashboardManager : MonoBehaviour
    {
        public TextMeshProUGUI metricsText;
        public Button backToLoginButton;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            AutoBind();
            Phase2RuntimeUI.EnsureAdminUI(this);

            if (backToLoginButton != null)
            {
                backToLoginButton.onClick.RemoveAllListeners();
                backToLoginButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("LoginScene"));
            }

            StartCoroutine(LoadMetrics());
        }

        private void AutoBind()
        {
            if (metricsText == null)
                metricsText = AutoBindHelper.FindComponentByName<TextMeshProUGUI>("AdminMetricsText");
            if (backToLoginButton == null)
                backToLoginButton = AutoBindHelper.FindComponentByName<Button>("BackToLoginButton");
        }

        private IEnumerator LoadMetrics()
        {
            if (metricsText == null)
                yield break;

            metricsText.text = "Loading anonymized metrics...";

            var allLocal = LocalSessionStore.GetAllSessions();
            int totalSessions = allLocal.Count;
            float totalDuration = 0f;
            int totalPanic = 0;

            foreach (var s in allLocal)
            {
                totalDuration += s.ExposureTime;
                totalPanic += s.PanicEventCount;
            }

            float avgDuration = totalSessions > 0 ? totalDuration / totalSessions : 0f;
            float avgPanic = totalSessions > 0 ? (float)totalPanic / totalSessions : 0f;

            var builder = new StringBuilder();
            builder.AppendLine("Administrator Dashboard");
            builder.AppendLine($"Total sessions (local cache): {totalSessions}");
            builder.AppendLine($"Average duration: {avgDuration:F0}s");
            builder.AppendLine($"Average panic events/session: {avgPanic:F1}");
            builder.AppendLine($"Pending cloud sync: {LocalSessionStore.GetUnsyncedSessions().Count}");
            builder.AppendLine("\nUse Manage Users in a future web companion for full account control.");

            metricsText.text = builder.ToString();
            yield return null;
        }
    }
}
