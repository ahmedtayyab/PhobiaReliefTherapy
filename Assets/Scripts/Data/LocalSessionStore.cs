using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// Local offline session cache (SRS §5D). Uses JSON file at persistentDataPath.
    /// Synced to Supabase when online via DatabaseManager.
    /// </summary>
    public static class LocalSessionStore
    {
        public const string GuestUserId = "local-guest";

        [Serializable]
        public class StoredSession
        {
            public string Id;
            public string UserId;
            public string Username;
            public string Phobia;
            public string Difficulty;
            public int Stage;
            public int BaselineHeartRate;
            public int FinalHeartRate;
            public int PeakHeartRate;
            public float AverageHeartRate;
            public float ExposureTime;
            public int PanicEventCount;
            public int SafetyTriggerCount;
            public float HighestPanicScore;
            public float AveragePanicScore;
            public float BaselineHeadMovementScore;
            public float AverageHeadMovementScore;
            public float PeakHeadMovementScore;
            public string AiRecommendation;
            public string Feedback;
            public string DateCompleted;
            public bool Aborted;
            public bool Synced;
        }

        [Serializable]
        private class StoredSessionList
        {
            public List<StoredSession> sessions = new List<StoredSession>();
        }

        private static string openSessionId;
        private static string StorePath => Path.Combine(Application.persistentDataPath, "PhobiaReliefTherapy_sessions.json");

        public static void ClearOpenSession()
        {
            openSessionId = null;
        }

        public static StoredSession PersistFromCurrentMetrics()
        {
            var session = BuildFromCurrentMetrics();
            if (!string.IsNullOrEmpty(openSessionId))
            {
                session.Id = openSessionId;
                var existing = FindById(openSessionId);
                if (existing != null && !string.IsNullOrEmpty(existing.DateCompleted))
                    session.DateCompleted = existing.DateCompleted;
            }

            Upsert(session);
            openSessionId = session.Id;
            return session;
        }

        private static StoredSession FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return LoadAllInternal().sessions.Find(s => s.Id == id);
        }

        public static StoredSession UpdateOpenFeedback(string feedback)
        {
            var session = PersistFromCurrentMetrics();
            session.Feedback = feedback ?? "";
            session.Synced = false;
            Upsert(session);
            return session;
        }

        public static void SaveSession(StoredSession session)
        {
            if (session == null)
                return;

            if (string.IsNullOrEmpty(session.Id))
                session.Id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(session.UserId))
                session.UserId = ResolveUserId();
            if (string.IsNullOrEmpty(session.Username))
                session.Username = string.IsNullOrEmpty(UserData.Username) ? "Guest" : UserData.Username;
            if (string.IsNullOrEmpty(session.DateCompleted))
                session.DateCompleted = DateTime.UtcNow.ToString("o");

            session.Synced = false;
            Upsert(session);
            openSessionId = session.Id;
        }

        public static List<StoredSession> GetUnsyncedSessions()
        {
            return LoadAllInternal().sessions.FindAll(s => !s.Synced);
        }

        public static void MarkSynced(StoredSession session)
        {
            if (session == null)
                return;

            var list = LoadAllInternal();
            for (int i = 0; i < list.sessions.Count; i++)
            {
                if (IsSameRecord(list.sessions[i], session))
                {
                    list.sessions[i].Synced = true;
                    WriteAll(list);
                    return;
                }
            }
        }

        public static List<StoredSession> GetSessionsForUser(string userId)
        {
            var all = LoadAllInternal().sessions;
            if (all.Count == 0)
                return new List<StoredSession>();

            if (string.IsNullOrEmpty(userId))
                return new List<StoredSession>(all);

            var matched = all.FindAll(s => BelongsToUser(s, userId));
            matched.Sort((a, b) => string.CompareOrdinal(NormalizeSortKey(b.DateCompleted), NormalizeSortKey(a.DateCompleted)));
            return matched;
        }

        public static List<StoredSession> GetAllSessions()
        {
            var all = LoadAllInternal().sessions;
            all.Sort((a, b) => string.CompareOrdinal(NormalizeSortKey(b.DateCompleted), NormalizeSortKey(a.DateCompleted)));
            return all;
        }

        public static string FormatDate(string raw)
        {
            if (DateTime.TryParse(raw, out DateTime parsed))
                return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return string.IsNullOrEmpty(raw) ? "" : raw;
        }

        public static string FormatDifficulty(string stored)
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

        public static string FormatDetail(StoredSession s, bool includeUser)
        {
            if (s == null)
                return "";

            string who = includeUser && !string.IsNullOrEmpty(s.Username) ? s.Username + "  |  " : "";
            string ended = s.Aborted ? "ended early" : "completed";
            var line =
                $"{FormatDate(s.DateCompleted)}\n" +
                $"{who}{s.Phobia}  |  {FormatDifficulty(s.Difficulty)}  |  Stage {s.Stage}  |  {s.ExposureTime:F0}s  |  {ended}\n" +
                $"Heart rate      baseline {s.BaselineHeartRate}   average {s.AverageHeartRate:F0}   peak {s.PeakHeartRate}   final {s.FinalHeartRate}\n" +
                $"Head movement   baseline {s.BaselineHeadMovementScore:F1}   average {s.AverageHeadMovementScore:F1}   peak {s.PeakHeadMovementScore:F1}\n" +
                $"Panic           events {s.PanicEventCount}   average {s.AveragePanicScore:F1}   highest {s.HighestPanicScore:F1}   safety stops {s.SafetyTriggerCount}";

            if (!string.IsNullOrEmpty(s.AiRecommendation))
                line += $"\nAI              {s.AiRecommendation}";
            if (!string.IsNullOrEmpty(s.Feedback) && s.Feedback != s.AiRecommendation)
                line += $"\nFeedback        {s.Feedback}";
            return line + "\n";
        }

        public static string FormatCompact(StoredSession s, bool includeUser)
        {
            if (s == null)
                return "";

            string who = includeUser && !string.IsNullOrEmpty(s.Username) ? s.Username + "  " : "";
            string ended = s.Aborted ? "early" : "ok";
            return $"{FormatDate(s.DateCompleted)}  {who}{s.Phobia}  Stg {s.Stage}  {s.ExposureTime:F0}s  HR {s.AverageHeartRate:F0}/{s.PeakHeartRate}  stress {s.AveragePanicScore:F1}  {ended}";
        }

        public static string FormatBar(int value, int total, int width = 14)
        {
            if (total <= 0 || width <= 0)
                return "[" + new string('-', width) + "]  0%";

            int filled = Mathf.Clamp(Mathf.RoundToInt(width * (float)value / total), 0, width);
            int pct = Mathf.RoundToInt(100f * value / total);
            return "[" + new string('#', filled) + new string('-', width - filled) + "]  " + pct + "%";
        }

        public static bool IsHeight(string phobia) => MatchesPhobia(phobia, "Height", "Acro");
        public static bool IsDarkness(string phobia) => MatchesPhobia(phobia, "Darkness", "Nycto");
        public static bool IsCrowd(string phobia) => MatchesPhobia(phobia, "Crowd", "Ochlo");

        private static bool MatchesPhobia(string value, params string[] tokens)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            foreach (var token in tokens)
            {
                if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static StoredSession BuildFromCurrentMetrics()
        {
            return new StoredSession
            {
                Id = string.IsNullOrEmpty(openSessionId) ? Guid.NewGuid().ToString() : openSessionId,
                UserId = ResolveUserId(),
                Username = string.IsNullOrEmpty(UserData.Username) ? "Guest" : UserData.Username,
                Phobia = UserData.SelectedPhobia,
                Difficulty = UserData.SelectedDifficulty,
                Stage = UserData.CurrentStage,
                BaselineHeartRate = UserData.BaselineHeartRate,
                FinalHeartRate = SessionMetrics.FinalHeartRate,
                PeakHeartRate = SessionMetrics.PeakHeartRate,
                AverageHeartRate = SessionMetrics.AverageHeartRate,
                ExposureTime = SessionMetrics.TotalExposureDuration,
                PanicEventCount = SessionMetrics.PanicEventCount,
                SafetyTriggerCount = SessionMetrics.SafetyTriggerCount,
                HighestPanicScore = SessionMetrics.HighestPanicScore,
                AveragePanicScore = SessionMetrics.AveragePanicScore,
                BaselineHeadMovementScore = SessionMetrics.BaselineHeadMovementScore,
                AverageHeadMovementScore = SessionMetrics.AverageHeadMovementScore,
                PeakHeadMovementScore = SessionMetrics.PeakHeadMovementScore,
                AiRecommendation = SessionMetrics.LastAiRecommendation ?? "",
                Feedback = SessionMetrics.LastAiRecommendation ?? "",
                DateCompleted = DateTime.UtcNow.ToString("o"),
                Aborted = UserData.SessionWasAborted || SessionMetrics.SessionAborted,
                Synced = false
            };
        }

        private static string ResolveUserId()
        {
            return string.IsNullOrEmpty(UserData.UserId) ? GuestUserId : UserData.UserId;
        }

        private static bool BelongsToUser(StoredSession session, string userId)
        {
            if (session == null)
                return false;
            if (session.UserId == userId)
                return true;

            bool orphan = string.IsNullOrEmpty(session.UserId) || session.UserId == GuestUserId;
            if (orphan)
            {
                if (string.IsNullOrEmpty(session.Username) || session.Username == "Guest")
                    return true;
                return string.Equals(session.Username, UserData.Username, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrEmpty(session.Username) &&
                !string.IsNullOrEmpty(UserData.Username) &&
                UserData.Username != "Guest" &&
                string.Equals(session.Username, UserData.Username, StringComparison.OrdinalIgnoreCase))
                return true;

            var account = LocalAccountStore.FindById(userId);
            return account != null &&
                   !string.IsNullOrEmpty(session.Username) &&
                   string.Equals(session.Username, account.Username, StringComparison.OrdinalIgnoreCase);
        }

        private static void Upsert(StoredSession session)
        {
            var list = LoadAllInternal();
            int index = list.sessions.FindIndex(s => IsSameRecord(s, session));
            if (index >= 0)
            {
                if (string.IsNullOrEmpty(session.DateCompleted))
                    session.DateCompleted = list.sessions[index].DateCompleted;
                list.sessions[index] = session;
            }
            else
            {
                list.sessions.Add(session);
            }

            WriteAll(list);
        }

        private static bool IsSameRecord(StoredSession a, StoredSession b)
        {
            if (a == null || b == null)
                return false;
            if (!string.IsNullOrEmpty(a.Id) && !string.IsNullOrEmpty(b.Id) && a.Id == b.Id)
                return true;
            return a.UserId == b.UserId &&
                   a.Phobia == b.Phobia &&
                   a.DateCompleted == b.DateCompleted;
        }

        public static string NormalizeSortKey(string raw)
        {
            if (DateTime.TryParse(raw, out DateTime parsed))
                return parsed.ToUniversalTime().ToString("o");
            return raw ?? "";
        }

        private static StoredSessionList LoadAllInternal()
        {
            if (!File.Exists(StorePath))
                return new StoredSessionList();

            try
            {
                string json = File.ReadAllText(StorePath);
                return JsonUtility.FromJson<StoredSessionList>(json) ?? new StoredSessionList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalSessionStore read failed: {ex.Message}");
                return new StoredSessionList();
            }
        }

        private static void WriteAll(StoredSessionList list)
        {
            try
            {
                File.WriteAllText(StorePath, JsonUtility.ToJson(list, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalSessionStore write failed: {ex.Message}");
            }
        }
    }
}
