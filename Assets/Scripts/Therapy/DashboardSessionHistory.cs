using UnityEngine;
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

        private void Start()
        {
            if (historyText == null)
                historyText = AutoBindHelper.FindComponentByName<TextMeshProUGUI>("SessionHistoryText");

            Phase2RuntimeUI.EnsureDashboardHistoryUI(this);

            StartCoroutine(LoadHistory());
            StartCoroutine(DatabaseManager.Instance.SyncPendingLocalSessions());
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
            var builder = new StringBuilder();
            builder.AppendLine("Recent Sessions");

            int count = 0;
            for (int i = local.Count - 1; i >= 0 && count < 5; i--)
            {
                var s = local[i];
                builder.AppendLine($"- {s.DateCompleted}: {s.Phobia} ({s.Difficulty}) | Peak {s.PeakHeartRate} BPM | {s.ExposureTime:F0}s");
                count++;
            }

            if (remoteSessions.Length > 0 && count == 0)
            {
                for (int i = remoteSessions.Length - 1; i >= 0 && count < 5; i--)
                {
                    var s = remoteSessions[i];
                    builder.AppendLine($"- {s.DateCompleted}: {s.Phobia} ({s.Difficulty}) | Final {s.FinalHeartRate} BPM | {s.ExposureTime:F0}s");
                    count++;
                }
            }

            if (count == 0)
                builder.AppendLine("No sessions recorded yet.");

            historyText.text = builder.ToString();
        }
    }
}
