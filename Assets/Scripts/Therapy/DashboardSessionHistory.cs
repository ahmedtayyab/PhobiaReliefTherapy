using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Text;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Bootstrap;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Shows recent session history on the dashboard (SRS §3.2.9).
    /// </summary>
    public class DashboardSessionHistory : MonoBehaviour
    {
        public TextMeshProUGUI historyText;
        [HideInInspector] public GameObject historyRoot;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != "DashboardScene")
            {
                enabled = false;
                return;
            }

            if (historyText == null)
                historyText = AutoBindHelper.FindComponentByName<TextMeshProUGUI>("SessionHistoryText");

            Phase2RuntimeUI.EnsureDashboardHistoryUI(this);

            StartCoroutine(LoadHistory());
            StartCoroutine(DatabaseManager.Instance.SyncPendingLocalSessions());
        }

        private void OnDestroy()
        {
            if (historyRoot != null)
                Destroy(historyRoot);
        }

        private IEnumerator LoadHistory()
        {
            if (historyText == null)
                yield break;

            historyText.text = "Loading session history...";

            bool done = false;
            TherapySession[] remoteSessions = new TherapySession[0];

            if (!string.IsNullOrEmpty(UserData.UserId))
            {
                yield return DatabaseManager.Instance.GetUserSessions(UserData.UserId, (sessions) =>
                {
                    remoteSessions = sessions ?? new TherapySession[0];
                    done = true;
                });

                while (!done)
                    yield return null;
            }

            var local = LocalSessionStore.GetSessionsForUser(UserData.UserId);
            var merged = DatabaseManager.MergeSessions(local, remoteSessions);
            var builder = new StringBuilder();
            builder.AppendLine("Recent Sessions");

            int count = 0;
            foreach (var s in merged)
            {
                builder.AppendLine(LocalSessionStore.FormatDetail(s, false));
                count++;
                if (count >= 5)
                    break;
            }

            if (count == 0)
                builder.AppendLine("No sessions recorded yet.");

            historyText.text = builder.ToString();
        }
    }
}
