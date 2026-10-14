using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.UI;
using PhobiaReliefTherapy.Theme;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// User dashboard (SRS §3.2.10) with progress, profile, and notifications.
    /// </summary>
    public class DashboardManager : MonoBehaviour
    {
        private GameObject hubRoot;
        private TextMeshProUGUI welcomeText;
        private TextMeshProUGUI summaryText;
        private GameObject overlay;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            LocalAccountStore.EnsureSeedAccounts();
            HideLegacyPhobiaButtons();
            var selection = Object.FindObjectOfType<PhobiaSelectionManager>();
            if (selection != null && selection.gameObject != gameObject)
                selection.enabled = false;
            var history = Object.FindObjectOfType<DashboardSessionHistory>();
            if (history != null)
                history.gameObject.SetActive(false);
            BuildHub();
            Refresh();
            StartCoroutine(RefreshFromCloud());
        }

        private void HideLegacyPhobiaButtons()
        {
            string[] names = { "HeightButton", "DarknessButton", "CrowdButton", "InstructionText", "SelectedPhobiaText" };
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go != null)
                    go.SetActive(false);
            }
        }

        private void BuildHub()
        {
            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            var parent = ClinicalUIFactory.OverlayRoot(canvas);

            hubRoot = ClinicalUIFactory.CreateCard(parent, new Vector2(980, 760), "DashboardHubCard");
            var card = hubRoot.transform;

            welcomeText = ClinicalUIFactory.CreateText("DashWelcome", card, "Welcome", 34, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(welcomeText.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f));

            summaryText = ClinicalUIFactory.CreateText("DashSummary", card, "", 20, TextAlignmentOptions.TopLeft);
            ClinicalUIFactory.Anchor(summaryText.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.87f));

            var startBtn = ClinicalUIFactory.CreateButton("StartSessionButton", card, "Start New Session");
            ClinicalUIFactory.Anchor(startBtn.GetComponent<RectTransform>(), new Vector2(0.18f, 0.48f), new Vector2(0.48f, 0.48f), new Vector2(0, 58), new Vector2(0, 0));
            startBtn.onClick.AddListener(() => SceneLoader.Instance.LoadScene("PhobiaSelectionScene"));

            var progressBtn = ClinicalUIFactory.CreateButton("ViewProgressButton", card, "View Progress", false);
            ClinicalUIFactory.Anchor(progressBtn.GetComponent<RectTransform>(), new Vector2(0.52f, 0.48f), new Vector2(0.82f, 0.48f), new Vector2(0, 58));
            progressBtn.onClick.AddListener(ShowProgress);

            var profileBtn = ClinicalUIFactory.CreateButton("ProfileButton", card, "Profile", false);
            ClinicalUIFactory.Anchor(profileBtn.GetComponent<RectTransform>(), new Vector2(0.18f, 0.34f), new Vector2(0.48f, 0.34f), new Vector2(0, 58));
            profileBtn.onClick.AddListener(ShowProfile);

            var notifBtn = ClinicalUIFactory.CreateButton("NotificationsButton", card, "Notifications", false);
            ClinicalUIFactory.Anchor(notifBtn.GetComponent<RectTransform>(), new Vector2(0.52f, 0.34f), new Vector2(0.82f, 0.34f), new Vector2(0, 58));
            notifBtn.onClick.AddListener(ShowNotifications);

            if (UserData.IsAdmin)
            {
                var adminBtn = ClinicalUIFactory.CreateButton("AdminHubButton", card, "Admin Hub", false);
                ClinicalUIFactory.Anchor(adminBtn.GetComponent<RectTransform>(), new Vector2(0.18f, 0.18f), new Vector2(0.48f, 0.18f), new Vector2(0, 58));
                adminBtn.onClick.AddListener(() => SceneLoader.Instance.LoadScene("AdminScene"));
            }

            var logoutBtn = ClinicalUIFactory.CreateButton("LogoutButton", card, "Log out", false);
            ClinicalUIFactory.Anchor(logoutBtn.GetComponent<RectTransform>(), new Vector2(0.52f, 0.18f), new Vector2(0.82f, 0.18f), new Vector2(0, 58));
            logoutBtn.onClick.AddListener(Logout);

            var hint = ClinicalUIFactory.CreateText("DashHint", card,
                "Editor without VR: mouse to click. Polar H10: run tools/polar_hr_bridge.py then Reconnect.",
                16, TextAlignmentOptions.Center, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(hint.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f));
        }

        private void Refresh()
        {
            RefreshWithSessions(LocalSessionStore.GetSessionsForUser(UserData.UserId));
        }

        private IEnumerator RefreshFromCloud()
        {
            yield return DatabaseManager.Instance.ProbeCloudHealth();
            yield return DatabaseManager.Instance.SyncPendingLocalSessions();

            TherapySession[] remote = new TherapySession[0];
            bool done = false;
            yield return DatabaseManager.Instance.GetUserSessions(UserData.UserId, sessions =>
            {
                remote = sessions ?? new TherapySession[0];
                done = true;
            });
            while (!done)
                yield return null;

            if (welcomeText == null || summaryText == null)
                yield break;

            var merged = DatabaseManager.MergeSessions(LocalSessionStore.GetSessionsForUser(UserData.UserId), remote);
            RefreshWithSessions(merged);
        }

        private void RefreshWithSessions(List<LocalSessionStore.StoredSession> sessions)
        {
            if (welcomeText == null || summaryText == null)
                return;

            string name = string.IsNullOrEmpty(UserData.Username) ? "Guest" : UserData.Username;
            welcomeText.text = UserData.IsAdmin ? $"Welcome, {name} (Administrator)" : $"Welcome, {name}";

            int unread = LocalNotificationStore.UnreadCount(UserData.UserId);
            var builder = new StringBuilder();
            builder.AppendLine("Your dashboard");
            builder.AppendLine(UserData.AuthenticatedViaCloud ? DatabaseManager.StatusLabel : $"{DatabaseManager.StatusLabel} - local login");
            builder.AppendLine($"Sessions completed: {sessions.Count}");
            builder.AppendLine($"Unread notifications: {unread}");

            if (sessions.Count > 0)
            {
                var last = sessions[0];
                builder.AppendLine($"Latest: {last.Phobia} - {LocalSessionStore.FormatDifficulty(last.Difficulty)} - {LocalSessionStore.FormatDate(last.DateCompleted)}");
                builder.AppendLine($"HR peak {last.PeakHeartRate}, avg {last.AverageHeartRate:F0}, baseline {last.BaselineHeartRate} BPM");
                builder.AppendLine($"Panic events {last.PanicEventCount}, duration {last.ExposureTime:F0}s");
            }
            else
            {
                builder.AppendLine("No therapy sessions yet. Start a new session when you are ready.");
            }

            summaryText.text = builder.ToString();
        }

        private void ShowProgress()
        {
            OpenOverlay("Progress", BuildProgressText(LocalSessionStore.GetSessionsForUser(UserData.UserId)));
            StartCoroutine(RefreshProgressFromCloud());
        }

        private IEnumerator RefreshProgressFromCloud()
        {
            yield return DatabaseManager.Instance.SyncPendingLocalSessions();

            TherapySession[] remote = new TherapySession[0];
            bool done = false;
            yield return DatabaseManager.Instance.GetUserSessions(UserData.UserId, sessions =>
            {
                remote = sessions ?? new TherapySession[0];
                done = true;
            });
            while (!done)
                yield return null;

            if (overlay == null)
                yield break;

            var sessions = DatabaseManager.MergeSessions(LocalSessionStore.GetSessionsForUser(UserData.UserId), remote);
            OpenOverlay("Progress", BuildProgressText(sessions));
        }

        private static string BuildProgressText(List<LocalSessionStore.StoredSession> sessions)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Session history and progress");
            builder.AppendLine(DatabaseManager.StatusLabel);
            builder.AppendLine();

            if (sessions == null || sessions.Count == 0)
            {
                builder.AppendLine("No session data found.");
                builder.AppendLine("Finish a PRT session, then return here.");
                return builder.ToString();
            }

            float duration = 0f;
            int panic = 0;
            int peak = 0;
            int baseline = 0;
            float avgHr = 0f;
            float movement = 0f;
            float panicScore = 0f;
            int safety = 0;
            int height = 0, darkness = 0, crowd = 0;
            foreach (var s in sessions)
            {
                duration += s.ExposureTime;
                panic += s.PanicEventCount;
                panicScore += s.AveragePanicScore;
                safety += s.SafetyTriggerCount;
                baseline += s.BaselineHeartRate;
                avgHr += s.AverageHeartRate;
                movement += s.AverageHeadMovementScore;
                if (s.PeakHeartRate > peak) peak = s.PeakHeartRate;
                if (LocalSessionStore.IsHeight(s.Phobia)) height++;
                else if (LocalSessionStore.IsDarkness(s.Phobia)) darkness++;
                else if (LocalSessionStore.IsCrowd(s.Phobia)) crowd++;
            }

            int n = sessions.Count;
            var newest = sessions[0];
            var oldest = sessions[sessions.Count - 1];

            builder.AppendLine("SUMMARY");
            builder.AppendLine($"Total sessions             {n}");
            builder.AppendLine($"Average duration           {(duration / n):F0} s");
            builder.AppendLine($"Average baseline HR        {(baseline / n)} BPM");
            builder.AppendLine($"Average session HR         {(avgHr / n):F0} BPM");
            builder.AppendLine($"Highest peak HR            {peak} BPM");
            builder.AppendLine($"Average panic events       {(float)panic / n:F1}");
            builder.AppendLine($"Average panic score        {(panicScore / n):F1}");
            builder.AppendLine($"Safety stops               {safety}");
            builder.AppendLine($"Average head movement      {(movement / n):F1}");
            builder.AppendLine();
            builder.AppendLine("SESSIONS PER PHOBIA");
            builder.AppendLine($"Height     {height,3}  {LocalSessionStore.FormatBar(height, n)}");
            builder.AppendLine($"Darkness   {darkness,3}  {LocalSessionStore.FormatBar(darkness, n)}");
            builder.AppendLine($"Crowd      {crowd,3}  {LocalSessionStore.FormatBar(crowd, n)}");
            builder.AppendLine();
            builder.AppendLine("PROGRESS OVER TIME");
            builder.AppendLine($"Peak HR          first {oldest.PeakHeartRate} BPM   ->   latest {newest.PeakHeartRate} BPM");
            builder.AppendLine($"Panic events     first {oldest.PanicEventCount}   ->   latest {newest.PanicEventCount}");
            builder.AppendLine($"Duration         first {oldest.ExposureTime:F0}s   ->   latest {newest.ExposureTime:F0}s");
            builder.AppendLine();
            builder.AppendLine("YOUR SESSIONS");
            builder.AppendLine();
            int shown = 0;
            foreach (var s in sessions)
            {
                builder.AppendLine(LocalSessionStore.FormatDetail(s, false));
                shown++;
                if (shown >= 25)
                    break;
            }

            return builder.ToString();
        }

        private void ShowProfile()
        {
            CloseOverlay();
            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            overlay = ClinicalUIFactory.CreateFullScreenPanel(canvas.transform, "ProfileOverlay");

            var card = ClinicalUIFactory.CreateCard(overlay.transform, new Vector2(720, 620), "ProfileCard");
            var title = ClinicalUIFactory.CreateText("ProfileTitle", card.transform, "Manage profile", 32, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f));

            var nameInput = ClinicalUIFactory.CreateInput("ProfileName", card.transform, "Name");
            ClinicalUIFactory.Anchor(nameInput.GetComponent<RectTransform>(), new Vector2(0.18f, 0.72f), new Vector2(0.82f, 0.72f), new Vector2(0, 48));
            nameInput.text = UserData.Username;

            var emailInput = ClinicalUIFactory.CreateInput("ProfileEmail", card.transform, "Email");
            ClinicalUIFactory.Anchor(emailInput.GetComponent<RectTransform>(), new Vector2(0.18f, 0.58f), new Vector2(0.82f, 0.58f), new Vector2(0, 48));
            emailInput.text = UserData.Email;

            var passInput = ClinicalUIFactory.CreateInput("ProfilePassword", card.transform, "New password (optional)", true);
            ClinicalUIFactory.Anchor(passInput.GetComponent<RectTransform>(), new Vector2(0.18f, 0.44f), new Vector2(0.82f, 0.44f), new Vector2(0, 48));

            var status = ClinicalUIFactory.CreateText("ProfileStatus", card.transform, "", 18, TextAlignmentOptions.Center, UIElementType.ErrorText);
            ClinicalUIFactory.Anchor(status.rectTransform, new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.40f));

            var save = ClinicalUIFactory.CreateButton("SaveProfile", card.transform, "Save changes");
            ClinicalUIFactory.Anchor(save.GetComponent<RectTransform>(), new Vector2(0.22f, 0.20f), new Vector2(0.78f, 0.20f), new Vector2(0, 54));
            save.onClick.AddListener(() =>
            {
                StartCoroutine(SaveProfileCoroutine(nameInput.text.Trim(), emailInput.text.Trim(), passInput.text.Trim(), status));
            });

            var close = ClinicalUIFactory.CreateButton("CloseProfile", card.transform, "Close", false);
            ClinicalUIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.32f, 0.10f), new Vector2(0.68f, 0.10f), new Vector2(0, 44));
            close.onClick.AddListener(CloseOverlay);
        }

        private IEnumerator SaveProfileCoroutine(string username, string email, string newPassword, TextMeshProUGUI status)
        {
            string error;
            if (!LocalAccountStore.UpdateProfile(UserData.UserId, username, email, out error))
            {
                status.text = error;
                yield break;
            }

            if (!string.IsNullOrEmpty(newPassword) &&
                !LocalAccountStore.ChangePassword(UserData.UserId, newPassword, out error))
            {
                status.text = error;
                yield break;
            }

            UserData.Username = username;
            UserData.Email = email;

            bool cloudDone = false;
            bool cloudOk = !UserData.AuthenticatedViaCloud;
            yield return DatabaseManager.Instance.UpdateUserProfile(UserData.UserId, username, email, null, null, (ok, err) =>
            {
                cloudOk = ok || !UserData.AuthenticatedViaCloud;
                cloudDone = true;
            });
            while (!cloudDone)
                yield return null;

            if (!string.IsNullOrEmpty(newPassword) && UserData.AuthenticatedViaCloud)
            {
                bool passDone = false;
                yield return DatabaseManager.Instance.UpdateAuthPassword(newPassword, (ok, err) =>
                {
                    if (!ok && !string.IsNullOrEmpty(err))
                        error = err;
                    passDone = true;
                });
                while (!passDone)
                    yield return null;
            }

            LocalNotificationStore.Add(UserData.UserId, "Profile updated", "Your name, email, or password was changed.");
            status.color = new Color32(72, 187, 120, 255);
            status.text = cloudOk ? "Profile saved to Supabase." : "Profile saved locally. Cloud update pending.";
            Refresh();
        }

        private void ShowNotifications()
        {
            var items = LocalNotificationStore.GetForUser(UserData.UserId);
            LocalNotificationStore.MarkAllRead(UserData.UserId);
            var builder = new StringBuilder();
            builder.AppendLine("Notifications and alerts");
            builder.AppendLine();
            if (items.Count == 0)
                builder.AppendLine("No notifications yet.");
            else
            {
                foreach (var item in items)
                    builder.AppendLine($"- {item.CreatedAt}\n  {item.Title}\n  {item.Body}\n");
            }

            OpenOverlay("Notifications", builder.ToString());
            Refresh();
        }

        private void OpenOverlay(string title, string body)
        {
            CloseOverlay();
            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            overlay = ClinicalUIFactory.CreateFullScreenPanel(canvas.transform, title + "Overlay");
            var card = ClinicalUIFactory.CreateCard(overlay.transform, new Vector2(1000, 740), title + "Card");

            var heading = ClinicalUIFactory.CreateText(title + "Title", card.transform, title, 30, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(heading.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f));

            var scroll = ClinicalUIFactory.CreateScrollText(title + "Scroll", card.transform, out var text);
            ClinicalUIFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.86f));
            ClinicalUIFactory.ApplyScrollBody(text, body);

            var close = ClinicalUIFactory.CreateButton("CloseOverlay", card.transform, "Close", false);
            ClinicalUIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.32f, 0.10f), new Vector2(0.68f, 0.10f), new Vector2(0, 46));
            close.onClick.AddListener(CloseOverlay);
        }

        private void CloseOverlay()
        {
            if (overlay != null)
                Destroy(overlay);
            overlay = null;
        }

        private void Logout()
        {
            UserData.UserId = "";
            UserData.Username = "Guest";
            UserData.Email = "";
            UserData.IsAdmin = false;
            UserData.AuthenticatedViaCloud = false;
            PlayerPrefs.DeleteKey("auth_token");
            SceneLoader.Instance.LoadScene("LoginScene");
        }
    }
}
