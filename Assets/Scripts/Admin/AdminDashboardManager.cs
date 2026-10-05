using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Therapy;
using PhobiaReliefTherapy.UI;
using PhobiaReliefTherapy.Theme;

namespace PhobiaReliefTherapy.Admin
{
    /// <summary>
    /// Administrator hub (SRS §3.2.14, UC-12, UC-13) plus access to normal PRT sessions.
    /// </summary>
    public class AdminDashboardManager : MonoBehaviour
    {
        public TextMeshProUGUI metricsText;
        public Button backToLoginButton;

        private GameObject hubRoot;
        private GameObject viewRoot;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            LocalAccountStore.EnsureSeedAccounts();
            HideLegacyAdminUi();
            ShowHome();
            StartCoroutine(ProbeAndRefreshHome());
        }

        private IEnumerator ProbeAndRefreshHome()
        {
            yield return DatabaseManager.Instance.ProbeCloudHealth();
            yield return DatabaseManager.Instance.SyncPendingLocalSessions();
            if (hubRoot != null)
                ShowHome();
        }

        private void HideLegacyAdminUi()
        {
            if (metricsText != null)
                metricsText.gameObject.SetActive(false);
            if (backToLoginButton != null)
                backToLoginButton.gameObject.SetActive(false);

            var legacy = GameObject.Find("AdminMetricsText");
            if (legacy != null)
                legacy.SetActive(false);
            var back = GameObject.Find("BackToLoginButton");
            if (back != null)
                back.SetActive(false);
        }

        private void ClearView()
        {
            if (hubRoot != null)
                Destroy(hubRoot);
            if (viewRoot != null)
                Destroy(viewRoot);
            hubRoot = null;
            viewRoot = null;
        }

        private Transform CanvasParent()
        {
            return ClinicalUIFactory.OverlayRoot(ClinicalUIFactory.EnsureOverlayCanvas());
        }

        private void ShowHome()
        {
            ClearView();
            hubRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(980, 720), "AdminHubCard");
            var card = hubRoot.transform;

            string name = string.IsNullOrEmpty(UserData.Username) ? "Administrator" : UserData.Username;
            var title = ClinicalUIFactory.CreateText("AdminTitle", card, "Administrator Hub", 36, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f));

            var sub = ClinicalUIFactory.CreateText("AdminSub", card,
                $"Signed in as {name}. {DatabaseManager.StatusLabel}\nManage the system or run a therapy session like a user.",
                20, TextAlignmentOptions.Center);
            ClinicalUIFactory.Anchor(sub.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.85f));

            var stats = ClinicalUIFactory.CreateButton("ViewStatsButton", card, "View stats");
            ClinicalUIFactory.Anchor(stats.GetComponent<RectTransform>(), new Vector2(0.08f, 0.56f), new Vector2(0.48f, 0.56f), new Vector2(0, 64));
            stats.onClick.AddListener(() => StartCoroutine(ShowAnalyticsRoutine()));

            var accounts = ClinicalUIFactory.CreateButton("ManageAccountsButton", card, "Manage accounts");
            ClinicalUIFactory.Anchor(accounts.GetComponent<RectTransform>(), new Vector2(0.52f, 0.56f), new Vector2(0.92f, 0.56f), new Vector2(0, 64));
            accounts.onClick.AddListener(() => StartCoroutine(ShowAccountsRoutine()));

            var session = ClinicalUIFactory.CreateButton("StartPrtButton", card, "Start PRT session");
            ClinicalUIFactory.Anchor(session.GetComponent<RectTransform>(), new Vector2(0.08f, 0.38f), new Vector2(0.48f, 0.38f), new Vector2(0, 64));
            session.onClick.AddListener(() => SceneLoader.Instance.LoadScene("DashboardScene"));

            var audit = ClinicalUIFactory.CreateButton("AuditLogButton", card, "Audit log", false);
            ClinicalUIFactory.Anchor(audit.GetComponent<RectTransform>(), new Vector2(0.52f, 0.38f), new Vector2(0.92f, 0.38f), new Vector2(0, 64));
            audit.onClick.AddListener(() => StartCoroutine(ShowAuditLogRoutine()));

            var logout = ClinicalUIFactory.CreateButton("AdminLogout", card, "Log out", false);
            ClinicalUIFactory.Anchor(logout.GetComponent<RectTransform>(), new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.16f), new Vector2(0, 54));
            logout.onClick.AddListener(() =>
            {
                UserData.UserId = "";
                UserData.Username = "Guest";
                UserData.Email = "";
            UserData.IsAdmin = false;
            UserData.AuthenticatedViaCloud = false;
            PlayerPrefs.DeleteKey("auth_token");
            SceneLoader.Instance.LoadScene("LoginScene");
            });
        }

        private void ShowAnalytics()
        {
            StartCoroutine(ShowAnalyticsRoutine());
        }

        private IEnumerator ShowAnalyticsRoutine()
        {
            ClearView();
            viewRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(1000, 740), "AdminAnalyticsCard");
            var card = viewRoot.transform;

            var title = ClinicalUIFactory.CreateText("AnalyticsTitle", card, "View stats · Analyze performance", 30, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f));

            var scroll = ClinicalUIFactory.CreateScrollText("AnalyticsScroll", card, out var body);
            ClinicalUIFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.86f));
            body.text = BuildAnalyticsText(new TherapySession[0], new User[0]);
            AddBackHome(card);

            yield return DatabaseManager.Instance.SyncPendingLocalSessions();

            TherapySession[] remote = new TherapySession[0];
            User[] remoteUsers = new User[0];
            bool sessionsDone = false;
            bool usersDone = false;
            yield return DatabaseManager.Instance.GetAllSessions(sessions =>
            {
                remote = sessions ?? new TherapySession[0];
                sessionsDone = true;
            });
            while (!sessionsDone)
                yield return null;

            yield return DatabaseManager.Instance.GetAllUsers(users =>
            {
                remoteUsers = users ?? new User[0];
                usersDone = true;
            });
            while (!usersDone)
                yield return null;

            if (body != null)
                body.text = BuildAnalyticsText(remote, remoteUsers);
        }

        private string BuildAnalyticsText(TherapySession[] remoteSessions, User[] remoteUsers)
        {
            var sessions = DatabaseManager.MergeSessions(LocalSessionStore.GetAllSessions(), remoteSessions);
            int userCount = remoteUsers != null && remoteUsers.Length > 0
                ? remoteUsers.Length
                : LocalAccountStore.GetAllAccounts().Count;
            var builder = new StringBuilder();
            builder.AppendLine(DatabaseManager.StatusLabel);
            builder.AppendLine();

            if (sessions.Count == 0)
            {
                builder.AppendLine("No session data found.");
                builder.AppendLine("Complete a PRT session (admin can do this too) and return here.");
                return builder.ToString();
            }

            float duration = 0f;
            int panic = 0;
            float panicScore = 0f;
            int baselineSum = 0;
            int peakSum = 0;
            float avgHrSum = 0f;
            float movementSum = 0f;
            int aborted = 0;
            int height = 0, darkness = 0, crowd = 0;

            foreach (var s in sessions)
            {
                duration += s.ExposureTime;
                panic += s.PanicEventCount;
                panicScore += s.AveragePanicScore;
                baselineSum += s.BaselineHeartRate;
                peakSum += s.PeakHeartRate;
                avgHrSum += s.AverageHeartRate;
                movementSum += s.AverageHeadMovementScore;
                if (s.Aborted) aborted++;
                if (LocalSessionStore.IsHeight(s.Phobia)) height++;
                else if (LocalSessionStore.IsDarkness(s.Phobia)) darkness++;
                else if (LocalSessionStore.IsCrowd(s.Phobia)) crowd++;
            }

            builder.AppendLine("Anonymized system metrics");
            builder.AppendLine($"Registered users: {userCount}");
            builder.AppendLine($"Total sessions: {sessions.Count}");
            builder.AppendLine($"Ended early: {aborted}");
            builder.AppendLine($"Average duration: {(duration / sessions.Count):F0}s");
            builder.AppendLine($"Average baseline HR: {(baselineSum / sessions.Count)} BPM");
            builder.AppendLine($"Average session HR: {(avgHrSum / sessions.Count):F0} BPM");
            builder.AppendLine($"Average peak HR: {(peakSum / sessions.Count)} BPM");
            builder.AppendLine($"Average head movement: {(movementSum / sessions.Count):F1}");
            builder.AppendLine($"Total panic events: {panic}");
            builder.AppendLine($"Average panic events / session: {(float)panic / sessions.Count:F1}");
            builder.AppendLine($"Average panic score: {(panicScore / sessions.Count):F1}");
            builder.AppendLine();
            builder.AppendLine("Sessions per phobia");
            builder.AppendLine($"Height:   {height}  {Bar(height, sessions.Count)}");
            builder.AppendLine($"Darkness: {darkness}  {Bar(darkness, sessions.Count)}");
            builder.AppendLine($"Crowd:    {crowd}  {Bar(crowd, sessions.Count)}");
            builder.AppendLine();
            builder.AppendLine("Recent sessions");
            int shown = 0;
            foreach (var s in sessions)
            {
                builder.AppendLine(LocalSessionStore.FormatDetail(s, true));
                shown++;
                if (shown >= 10)
                    break;
            }

            builder.AppendLine();
            builder.AppendLine("Pending cloud sync: " + LocalSessionStore.GetUnsyncedSessions().Count);
            if (remoteSessions != null && remoteSessions.Length > 0)
                builder.AppendLine($"Sessions loaded from Supabase: {remoteSessions.Length}");
            else
                builder.AppendLine("Local session store is included even if cloud is offline.");
            return builder.ToString();
        }

        private static string Bar(int value, int total)
        {
            int blocks = total <= 0 ? 0 : Mathf.RoundToInt(20f * value / total);
            return new string('█', Mathf.Max(0, blocks));
        }

        private void ShowAccounts()
        {
            StartCoroutine(ShowAccountsRoutine());
        }

        private IEnumerator ShowAccountsRoutine()
        {
            ClearView();
            viewRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(1000, 740), "AdminAccountsCard");
            var card = viewRoot.transform;

            var title = ClinicalUIFactory.CreateText("AccountsTitle", card, "Manage accounts", 32, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.98f));

            var loading = ClinicalUIFactory.CreateText("AccountsLoading", card, "Loading accounts from Supabase...", 18, TextAlignmentOptions.Center);
            ClinicalUIFactory.Anchor(loading.rectTransform, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.86f));
            AddBackHome(card);

            User[] remote = new User[0];
            bool done = false;
            yield return DatabaseManager.Instance.GetAllUsers(users =>
            {
                remote = users ?? new User[0];
                done = true;
            });
            while (!done)
                yield return null;

            if (viewRoot == null)
                yield break;

            if (loading != null)
                Destroy(loading.gameObject);

            var accounts = MergeAccounts(remote);
            float y = 0.78f;
            int shown = 0;
            foreach (var account in accounts)
            {
                if (shown >= 6)
                    break;

                var snapshot = account;
                string label = $"{snapshot.Username}  ·  {snapshot.Email}\n{snapshot.AccountStatus}{(snapshot.IsAdmin ? "  ·  admin" : "")}";
                var btn = ClinicalUIFactory.CreateButton("User_" + snapshot.Id, card, label, false);
                ClinicalUIFactory.Anchor(btn.GetComponent<RectTransform>(), new Vector2(0.08f, y), new Vector2(0.92f, y), new Vector2(0, 70));
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 18;
                    tmp.alignment = TextAlignmentOptions.Left;
                }
                btn.onClick.AddListener(() => ShowAccountDetail(snapshot));
                y -= 0.11f;
                shown++;
            }

            if (accounts.Count == 0)
            {
                var empty = ClinicalUIFactory.CreateText("NoUsers", card, "No accounts found.", 20, TextAlignmentOptions.Center);
                ClinicalUIFactory.Anchor(empty.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.6f));
            }
        }

        private static List<LocalAccountStore.Account> MergeAccounts(User[] remote)
        {
            var merged = new List<LocalAccountStore.Account>();
            var seen = new HashSet<string>();

            if (remote != null)
            {
                foreach (var user in remote)
                {
                    if (user == null || string.IsNullOrEmpty(user.Id) || !seen.Add(user.Id.ToLowerInvariant()))
                        continue;
                    merged.Add(ToAccount(user));
                    LocalAccountStore.UpsertFromCloud(user.Id, user.Username, user.Email, user.IsAdmin);
                }
            }

            foreach (var local in LocalAccountStore.GetAllAccounts())
            {
                string key = !string.IsNullOrEmpty(local.Id) ? local.Id.ToLowerInvariant() : local.Email;
                if (string.IsNullOrEmpty(key) || !seen.Add(key))
                    continue;
                merged.Add(local);
            }

            return merged;
        }

        private static LocalAccountStore.Account ToAccount(User user)
        {
            return new LocalAccountStore.Account
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                AccountStatus = string.IsNullOrEmpty(user.AccountStatus) ? LocalAccountStore.ActiveStatus : user.AccountStatus,
                CreatedAt = user.CreatedAt
            };
        }

        private void ShowAccountDetail(LocalAccountStore.Account account)
        {
            ClearView();
            viewRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(920, 700), "AccountDetailCard");
            var card = viewRoot.transform;

            var title = ClinicalUIFactory.CreateText("DetailTitle", card, "User details", 30, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f));

            var info = ClinicalUIFactory.CreateText("DetailInfo", card,
                $"Name: {account.Username}\nEmail: {account.Email}\nStatus: {account.AccountStatus}\nRole: {(account.IsAdmin ? "Administrator" : "User")}\nCreated: {account.CreatedAt}\n{DatabaseManager.StatusLabel}",
                20, TextAlignmentOptions.TopLeft);
            ClinicalUIFactory.Anchor(info.rectTransform, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.86f));

            var status = ClinicalUIFactory.CreateText("DetailStatus", card, "", 18, TextAlignmentOptions.Center, UIElementType.ErrorText);
            ClinicalUIFactory.Anchor(status.rectTransform, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.56f));

            bool suspended = account.AccountStatus == LocalAccountStore.SuspendedStatus;
            var toggle = ClinicalUIFactory.CreateButton("ToggleStatus", card, suspended ? "Activate account" : "Suspend account");
            ClinicalUIFactory.Anchor(toggle.GetComponent<RectTransform>(), new Vector2(0.08f, 0.38f), new Vector2(0.48f, 0.38f), new Vector2(0, 54));
            toggle.onClick.AddListener(() =>
            {
                string next = suspended ? LocalAccountStore.ActiveStatus : LocalAccountStore.SuspendedStatus;
                StartCoroutine(SetStatusRoutine(account, next, status));
            });

            var delete = ClinicalUIFactory.CreateButton("DeleteAccount", card, "Delete account", false);
            ClinicalUIFactory.Anchor(delete.GetComponent<RectTransform>(), new Vector2(0.52f, 0.38f), new Vector2(0.92f, 0.38f), new Vector2(0, 54));
            delete.onClick.AddListener(() => StartCoroutine(ConfirmDeleteRoutine(account, status)));

            var nameInput = ClinicalUIFactory.CreateInput("EditName", card, "Name");
            ClinicalUIFactory.Anchor(nameInput.GetComponent<RectTransform>(), new Vector2(0.08f, 0.24f), new Vector2(0.48f, 0.24f), new Vector2(0, 46));
            nameInput.text = account.Username;

            var emailInput = ClinicalUIFactory.CreateInput("EditEmail", card, "Email");
            ClinicalUIFactory.Anchor(emailInput.GetComponent<RectTransform>(), new Vector2(0.52f, 0.24f), new Vector2(0.92f, 0.24f), new Vector2(0, 46));
            emailInput.text = account.Email;

            var save = ClinicalUIFactory.CreateButton("SaveUserEdits", card, "Save details", false);
            ClinicalUIFactory.Anchor(save.GetComponent<RectTransform>(), new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.12f), new Vector2(0, 46));
            save.onClick.AddListener(() =>
                StartCoroutine(SaveAccountEditsRoutine(account, nameInput.text.Trim(), emailInput.text.Trim(), status)));

            var back = ClinicalUIFactory.CreateButton("BackToList", card, "Back", false);
            ClinicalUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.03f), new Vector2(0, 40));
            back.onClick.AddListener(() => StartCoroutine(ShowAccountsRoutine()));
        }

        private IEnumerator SetStatusRoutine(LocalAccountStore.Account account, string next, TextMeshProUGUI status)
        {
            if (!LocalAccountStore.SetStatus(UserData.UserId, account.Id, next, out string error))
            {
                status.text = error;
                yield break;
            }

            bool done = false;
            yield return DatabaseManager.Instance.UpdateUserProfile(account.Id, null, null, next, null, (_, __) => { done = true; });
            while (!done)
                yield return null;

            yield return DatabaseManager.Instance.InsertAuditLog(UserData.UserId, next == LocalAccountStore.SuspendedStatus ? "Suspend" : "Activate", account.Id, $"status={next}");
            StartCoroutine(ShowAccountsRoutine());
        }

        private IEnumerator SaveAccountEditsRoutine(LocalAccountStore.Account account, string username, string email, TextMeshProUGUI status)
        {
            if (!LocalAccountStore.UpdateProfile(account.Id, username, email, out string error))
            {
                status.text = error;
                yield break;
            }

            LocalAccountStore.LogAdminAction(UserData.UserId, "Edit", account.Id, "Updated name/email");
            bool done = false;
            yield return DatabaseManager.Instance.UpdateUserProfile(account.Id, username, email, null, null, (_, __) => { done = true; });
            while (!done)
                yield return null;
            yield return DatabaseManager.Instance.InsertAuditLog(UserData.UserId, "Edit", account.Id, "Updated name/email");
            StartCoroutine(ShowAccountsRoutine());
        }

        private IEnumerator ConfirmDeleteRoutine(LocalAccountStore.Account account, TextMeshProUGUI status)
        {
            if (!LocalAccountStore.DeleteAccount(UserData.UserId, account.Id, out string error))
            {
                status.text = error;
                yield break;
            }

            bool done = false;
            yield return DatabaseManager.Instance.DeleteUserProfile(account.Id, (_, __) => { done = true; });
            while (!done)
                yield return null;
            yield return DatabaseManager.Instance.InsertAuditLog(UserData.UserId, "Delete", account.Id, account.Email);
            StartCoroutine(ShowAccountsRoutine());
        }

        private void ShowAuditLog()
        {
            StartCoroutine(ShowAuditLogRoutine());
        }

        private IEnumerator ShowAuditLogRoutine()
        {
            ClearView();
            viewRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(1000, 740), "AuditCard");
            var card = viewRoot.transform;

            var title = ClinicalUIFactory.CreateText("AuditTitle", card, "Admin audit log", 32, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.97f));

            var scroll = ClinicalUIFactory.CreateScrollText("AuditScroll", card, out var body);
            ClinicalUIFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.86f));
            body.text = "Loading audit log from Supabase...";
            AddBackHome(card);

            AdminAuditEntry[] remote = new AdminAuditEntry[0];
            bool done = false;
            yield return DatabaseManager.Instance.GetAuditLog(entries =>
            {
                remote = entries ?? new AdminAuditEntry[0];
                done = true;
            });
            while (!done)
                yield return null;

            var builder = new StringBuilder();
            builder.AppendLine(DatabaseManager.StatusLabel);
            builder.AppendLine();
            if (remote.Length > 0)
            {
                foreach (var entry in remote)
                    builder.AppendLine($"{entry.CreatedAt}  {entry.Action}  target={entry.TargetUserId}\n  {entry.Details}\n");
            }

            var local = LocalAccountStore.GetAuditLog();
            if (local.Count > 0)
            {
                builder.AppendLine("Local audit entries");
                foreach (var entry in local)
                    builder.AppendLine($"{entry.Timestamp}  {entry.Action}  target={entry.TargetUserId}\n  {entry.Details}\n");
            }

            if (remote.Length == 0 && local.Count == 0)
                body.text = "No admin actions recorded yet.";
            else
                body.text = builder.ToString();
        }

        private void AddBackHome(Transform card)
        {
            var back = ClinicalUIFactory.CreateButton("BackHome", card, "Back to hub", false);
            ClinicalUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.05f), new Vector2(0, 46));
            back.onClick.AddListener(ShowHome);
        }
    }
}
