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
        private TextMeshProUGUI analyticsBody;
        private UiSparkline stressGraph;
        private UiSparkline hrGraph;
        private UiSparkline moveGraph;
        private TextMeshProUGUI stressValue;
        private TextMeshProUGUI hrValue;
        private TextMeshProUGUI moveValue;
        private TherapySession[] lastRemoteSessions = new TherapySession[0];
        private User[] lastRemoteUsers = new User[0];
        private string phobiaFilter = "";

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
            analyticsBody = null;
            stressGraph = null;
            hrGraph = null;
            moveGraph = null;
            stressValue = null;
            hrValue = null;
            moveValue = null;
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

            var stats = ClinicalUIFactory.CreateButton("ViewStatsButton", card, "View stats", false);
            ClinicalUIFactory.Anchor(stats.GetComponent<RectTransform>(), new Vector2(0.18f, 0.56f), new Vector2(0.48f, 0.56f), new Vector2(0, 64));
            stats.onClick.AddListener(() => StartCoroutine(ShowAnalyticsRoutine()));

            var accounts = ClinicalUIFactory.CreateButton("ManageAccountsButton", card, "Manage accounts", false);
            ClinicalUIFactory.Anchor(accounts.GetComponent<RectTransform>(), new Vector2(0.52f, 0.56f), new Vector2(0.82f, 0.56f), new Vector2(0, 64));
            accounts.onClick.AddListener(() => StartCoroutine(ShowAccountsRoutine()));

            var session = ClinicalUIFactory.CreateButton("StartPrtButton", card, "Start PRT session");
            ClinicalUIFactory.Anchor(session.GetComponent<RectTransform>(), new Vector2(0.18f, 0.38f), new Vector2(0.48f, 0.38f), new Vector2(0, 64));
            session.onClick.AddListener(() => SceneLoader.Instance.LoadScene("DashboardScene"));

            var audit = ClinicalUIFactory.CreateButton("AuditLogButton", card, "Audit log", false);
            ClinicalUIFactory.Anchor(audit.GetComponent<RectTransform>(), new Vector2(0.52f, 0.38f), new Vector2(0.82f, 0.38f), new Vector2(0, 64));
            audit.onClick.AddListener(() => StartCoroutine(ShowAuditLogRoutine()));

            var logout = ClinicalUIFactory.CreateButton("AdminLogout", card, "Log out", false);
            ClinicalUIFactory.Anchor(logout.GetComponent<RectTransform>(), new Vector2(0.30f, 0.16f), new Vector2(0.70f, 0.16f), new Vector2(0, 54));
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
            viewRoot = ClinicalUIFactory.CreateCard(CanvasParent(), new Vector2(1040, 780), "AdminAnalyticsCard");
            var card = viewRoot.transform;

            var title = ClinicalUIFactory.CreateText("AnalyticsTitle", card, "Analyze performance", 28, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.91f), new Vector2(0.92f, 0.98f));

            PlaceFilterButton(card, "FilterAll", "All", 0.18f, 0.325f, "");
            PlaceFilterButton(card, "FilterHeight", "Height", 0.345f, 0.49f, "Height");
            PlaceFilterButton(card, "FilterDarkness", "Darkness", 0.51f, 0.655f, "Darkness");
            PlaceFilterButton(card, "FilterCrowd", "Crowd", 0.675f, 0.82f, "Crowd");

            Color accent = new Color32(255, 87, 34, 255);
            Color calm = new Color32(56, 161, 105, 255);
            Color moveColor = new Color32(220, 225, 235, 255);

            stressGraph = CreateAnalyticsGraph(card, "StressGraph", "Avg stress",
                "panic score - gold = peak",
                new Vector2(0.08f, 0.48f), new Vector2(0.36f, 0.80f),
                calm, 0f, 30f, false, new[] { 10f, 18f }, out stressValue);
            hrGraph = CreateAnalyticsGraph(card, "HrGraph", "Avg heart rate",
                "BPM - gold = peak",
                new Vector2(0.37f, 0.48f), new Vector2(0.63f, 0.80f),
                accent, 50f, 140f, true, null, out hrValue);
            moveGraph = CreateAnalyticsGraph(card, "MoveGraph", "Avg movement",
                "head movement per session",
                new Vector2(0.64f, 0.48f), new Vector2(0.92f, 0.80f),
                moveColor, 0f, 80f, true, null, out moveValue);

            var scroll = ClinicalUIFactory.CreateScrollText("AnalyticsScroll", card, out analyticsBody);
            ClinicalUIFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.10f, 0.16f), new Vector2(0.90f, 0.46f));
            analyticsBody.text = "Loading anonymized session analytics...";
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

            if (analyticsBody != null)
                ApplyAnalytics(remote, remoteUsers);
        }

        private void PlaceFilterButton(Transform card, string name, string label, float xMin, float xMax, string filter)
        {
            var btn = ClinicalUIFactory.CreateButton(name, card, label, false);
            ClinicalUIFactory.Anchor(btn.GetComponent<RectTransform>(), new Vector2(xMin, 0.84f), new Vector2(xMax, 0.84f), new Vector2(0, 40));
            btn.onClick.AddListener(() =>
            {
                phobiaFilter = filter;
                ApplyAnalytics(lastRemoteSessions, lastRemoteUsers);
            });
        }

        private UiSparkline CreateAnalyticsGraph(Transform parent, string name, string title, string subtitle, Vector2 min, Vector2 max, Color line, float vmin, float vmax, bool autoScale, float[] thresholds, out TextMeshProUGUI valueLabel)
        {
            var card = ClinicalUIFactory.CreateCard(parent, Vector2.zero, name);
            ClinicalUIFactory.Anchor(card.GetComponent<RectTransform>(), min, max);
            var image = card.GetComponent<Image>();
            image.raycastTarget = false;
            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, 0.88f);

            var heading = ClinicalUIFactory.CreateText(name + "Title", card.transform, title, 15, TextAlignmentOptions.TopLeft, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(heading.rectTransform, new Vector2(0.07f, 0.78f), new Vector2(0.93f, 0.96f));
            heading.raycastTarget = false;

            valueLabel = ClinicalUIFactory.CreateText(name + "Value", card.transform, "--", 18, TextAlignmentOptions.TopLeft, UIElementType.SubheadingText);
            ClinicalUIFactory.Anchor(valueLabel.rectTransform, new Vector2(0.07f, 0.58f), new Vector2(0.93f, 0.80f));
            valueLabel.enableWordWrapping = false;
            valueLabel.raycastTarget = false;

            var sub = ClinicalUIFactory.CreateText(name + "Sub", card.transform, subtitle, 12, TextAlignmentOptions.TopLeft, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(sub.rectTransform, new Vector2(0.07f, 0.46f), new Vector2(0.93f, 0.60f));
            sub.raycastTarget = false;

            var plot = new GameObject(name + "Plot", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiSparkline));
            plot.transform.SetParent(card.transform, false);
            ClinicalUIFactory.Anchor(plot.GetComponent<RectTransform>(), new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.46f));

            var spark = plot.GetComponent<UiSparkline>();
            spark.raycastTarget = false;
            spark.MinValue = vmin;
            spark.MaxValue = vmax;
            spark.AutoScale = autoScale;
            spark.LineThickness = 2.2f;
            spark.LineColor = line;
            spark.FillColor = new Color(line.r, line.g, line.b, 0.22f);
            spark.SecondaryColor = new Color(1f, 0.82f, 0.45f, 0.9f);
            spark.Thresholds = thresholds;
            return spark;
        }

        private void ApplyAnalytics(TherapySession[] remoteSessions, User[] remoteUsers)
        {
            lastRemoteSessions = remoteSessions ?? new TherapySession[0];
            lastRemoteUsers = remoteUsers ?? new User[0];
            var sessions = FilterSessions(DatabaseManager.MergeSessions(LocalSessionStore.GetAllSessions(), lastRemoteSessions));

            if (analyticsBody != null)
                ClinicalUIFactory.ApplyScrollBody(analyticsBody, BuildAnalyticsText(sessions, lastRemoteUsers));

            UpdateAnalyticsGraphs(sessions);
        }

        private void UpdateAnalyticsGraphs(List<LocalSessionStore.StoredSession> sessions)
        {
            var stress = new List<float>();
            var stressPeak = new List<float>();
            var hrs = new List<float>();
            var hrPeak = new List<float>();
            var moves = new List<float>();

            if (sessions != null)
            {
                for (int i = sessions.Count - 1; i >= 0; i--)
                {
                    var s = sessions[i];
                    stress.Add(s.AveragePanicScore);
                    stressPeak.Add(s.HighestPanicScore);
                    hrs.Add(s.AverageHeartRate);
                    hrPeak.Add(s.PeakHeartRate);
                    moves.Add(s.AverageHeadMovementScore);
                }
            }

            PadSeries(stress);
            PadSeries(stressPeak);
            PadSeries(hrs);
            PadSeries(hrPeak);
            PadSeries(moves);

            if (stressGraph != null)
                stressGraph.SetSeries(stress, stressPeak);
            if (hrGraph != null)
                hrGraph.SetSeries(hrs, hrPeak);
            if (moveGraph != null)
                moveGraph.SetSeries(moves);

            bool empty = sessions == null || sessions.Count == 0;
            if (stressValue != null)
                stressValue.text = empty ? "No data" : $"{MeanOf(sessions, s => s.AveragePanicScore):F1} avg";
            if (hrValue != null)
                hrValue.text = empty ? "No data" : $"{MeanOf(sessions, s => s.AverageHeartRate):F0} BPM";
            if (moveValue != null)
                moveValue.text = empty ? "No data" : $"{MeanOf(sessions, s => s.AverageHeadMovementScore):F1}";
        }

        private static void PadSeries(List<float> series)
        {
            if (series.Count == 1)
                series.Add(series[0]);
        }

        private static float MeanOf(List<LocalSessionStore.StoredSession> sessions, System.Func<LocalSessionStore.StoredSession, float> pick)
        {
            if (sessions == null || sessions.Count == 0)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < sessions.Count; i++)
                sum += pick(sessions[i]);
            return sum / sessions.Count;
        }

        private string BuildAnalyticsText(List<LocalSessionStore.StoredSession> sessions, User[] remoteUsers)
        {
            int userCount = remoteUsers != null && remoteUsers.Length > 0
                ? remoteUsers.Length
                : LocalAccountStore.GetAllAccounts().Count;
            var builder = new StringBuilder();
            builder.AppendLine("Anonymized system analytics  (SRS UC-12)");
            builder.AppendLine(DatabaseManager.StatusLabel);
            if (!string.IsNullOrEmpty(phobiaFilter))
                builder.AppendLine("Filter: " + phobiaFilter + "   (press All to clear)");
            builder.AppendLine();

            if (sessions.Count == 0)
            {
                builder.AppendLine("No session data found.");
                builder.AppendLine("Complete a PRT session, then return here.");
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

            int n = sessions.Count;
            builder.AppendLine("SYSTEM METRICS");
            builder.AppendLine($"Registered users              {userCount}");
            builder.AppendLine($"Total sessions                {n}");
            builder.AppendLine($"Ended early (safety / abort)  {aborted}");
            builder.AppendLine($"Average duration              {(duration / n):F0} s");
            builder.AppendLine($"Average baseline HR           {(baselineSum / n)} BPM");
            builder.AppendLine($"Average session HR            {(avgHrSum / n):F0} BPM");
            builder.AppendLine($"Average peak HR               {(peakSum / n)} BPM");
            builder.AppendLine($"Average head movement         {(movementSum / n):F1}");
            builder.AppendLine($"Total panic events            {panic}");
            builder.AppendLine($"Average panic events / session {(float)panic / n:F1}");
            builder.AppendLine($"Average panic score           {(panicScore / n):F1}");
            builder.AppendLine();
            builder.AppendLine("SESSIONS PER PHOBIA");
            builder.AppendLine($"Height     {height,3}  ({PhobiaPct(height, n)})");
            builder.AppendLine($"Darkness   {darkness,3}  ({PhobiaPct(darkness, n)})");
            builder.AppendLine($"Crowd      {crowd,3}  ({PhobiaPct(crowd, n)})");
            builder.AppendLine();
            builder.AppendLine($"Graphs above plot all {n} sessions, oldest to newest.");
            builder.AppendLine();
            builder.AppendLine($"ALL SESSIONS  ({n}, anonymized, no names)");
            builder.AppendLine();
            foreach (var s in sessions)
                builder.AppendLine(LocalSessionStore.FormatCompact(s, false));

            builder.AppendLine("Pending cloud sync: " + LocalSessionStore.GetUnsyncedSessions().Count);
            if (lastRemoteSessions.Length > 0)
                builder.AppendLine("Sessions loaded from Supabase: " + lastRemoteSessions.Length);
            else
                builder.AppendLine("Local session store is included even if cloud is offline.");
            return builder.ToString();
        }

        private static string PhobiaPct(int part, int total)
        {
            if (total <= 0)
                return "0%";
            return Mathf.RoundToInt(100f * part / total) + "%";
        }

        private List<LocalSessionStore.StoredSession> FilterSessions(List<LocalSessionStore.StoredSession> sessions)
        {
            if (string.IsNullOrEmpty(phobiaFilter) || sessions == null)
                return sessions ?? new List<LocalSessionStore.StoredSession>();

            return sessions.FindAll(s =>
            {
                if (phobiaFilter == "Height") return LocalSessionStore.IsHeight(s.Phobia);
                if (phobiaFilter == "Darkness") return LocalSessionStore.IsDarkness(s.Phobia);
                if (phobiaFilter == "Crowd") return LocalSessionStore.IsCrowd(s.Phobia);
                return true;
            });
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
                string label = $"{snapshot.Username} - {snapshot.Email}\n{snapshot.AccountStatus}{(snapshot.IsAdmin ? " - admin" : "")}";
                var btn = ClinicalUIFactory.CreateButton("User_" + snapshot.Id, card, label, false);
                ClinicalUIFactory.Anchor(btn.GetComponent<RectTransform>(), new Vector2(0.18f, y), new Vector2(0.82f, y), new Vector2(0, 70));
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
            ClinicalUIFactory.Anchor(toggle.GetComponent<RectTransform>(), new Vector2(0.18f, 0.40f), new Vector2(0.48f, 0.40f), new Vector2(0, 54));
            toggle.onClick.AddListener(() =>
            {
                string next = suspended ? LocalAccountStore.ActiveStatus : LocalAccountStore.SuspendedStatus;
                StartCoroutine(SetStatusRoutine(account, next, status));
            });

            var delete = ClinicalUIFactory.CreateButton("DeleteAccount", card, "Delete account", false);
            ClinicalUIFactory.Anchor(delete.GetComponent<RectTransform>(), new Vector2(0.52f, 0.40f), new Vector2(0.82f, 0.40f), new Vector2(0, 54));
            delete.onClick.AddListener(() => StartCoroutine(ConfirmDeleteRoutine(account, status)));

            var nameInput = ClinicalUIFactory.CreateInput("EditName", card, "Name");
            ClinicalUIFactory.Anchor(nameInput.GetComponent<RectTransform>(), new Vector2(0.18f, 0.26f), new Vector2(0.48f, 0.26f), new Vector2(0, 46));
            nameInput.text = account.Username;

            var emailInput = ClinicalUIFactory.CreateInput("EditEmail", card, "Email");
            ClinicalUIFactory.Anchor(emailInput.GetComponent<RectTransform>(), new Vector2(0.52f, 0.26f), new Vector2(0.82f, 0.26f), new Vector2(0, 46));
            emailInput.text = account.Email;

            var save = ClinicalUIFactory.CreateButton("SaveUserEdits", card, "Save details", false);
            ClinicalUIFactory.Anchor(save.GetComponent<RectTransform>(), new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.16f), new Vector2(0, 46));
            save.onClick.AddListener(() =>
                StartCoroutine(SaveAccountEditsRoutine(account, nameInput.text.Trim(), emailInput.text.Trim(), status)));

            var back = ClinicalUIFactory.CreateButton("BackToList", card, "Back", false);
            ClinicalUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.08f), new Vector2(0, 40));
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
            ClinicalUIFactory.Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.86f));
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

            if (body == null)
                yield break;

            if (remote.Length == 0 && local.Count == 0)
                ClinicalUIFactory.ApplyScrollBody(body, "No admin actions recorded yet.");
            else
                ClinicalUIFactory.ApplyScrollBody(body, builder.ToString());
        }

        private void AddBackHome(Transform card)
        {
            var back = ClinicalUIFactory.CreateButton("BackHome", card, "Back to hub", false);
            ClinicalUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.32f, 0.10f), new Vector2(0.68f, 0.10f), new Vector2(0, 46));
            back.onClick.AddListener(ShowHome);
        }
    }
}
