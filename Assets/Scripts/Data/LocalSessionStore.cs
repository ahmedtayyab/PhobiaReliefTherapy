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
        [Serializable]
        public class StoredSession
        {
            public string UserId;
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
            public string Feedback;
            public string DateCompleted;
            public bool Synced;
        }

        [Serializable]
        private class StoredSessionList
        {
            public List<StoredSession> sessions = new List<StoredSession>();
        }

        private static string StorePath => Path.Combine(Application.persistentDataPath, "PhobiaReliefTherapy_sessions.json");

        public static void SaveSession(StoredSession session)
        {
            var list = LoadAllInternal();
            session.Synced = false;
            list.sessions.Add(session);
            WriteAll(list);
        }

        public static List<StoredSession> GetUnsyncedSessions()
        {
            return LoadAllInternal().sessions.FindAll(s => !s.Synced);
        }

        public static void MarkSynced(StoredSession session)
        {
            var list = LoadAllInternal();
            for (int i = 0; i < list.sessions.Count; i++)
            {
                if (list.sessions[i].DateCompleted == session.DateCompleted &&
                    list.sessions[i].UserId == session.UserId &&
                    !list.sessions[i].Synced)
                {
                    list.sessions[i].Synced = true;
                    break;
                }
            }
            WriteAll(list);
        }

        public static List<StoredSession> GetSessionsForUser(string userId)
        {
            return LoadAllInternal().sessions.FindAll(s => s.UserId == userId);
        }

        public static List<StoredSession> GetAllSessions()
        {
            return LoadAllInternal().sessions;
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
                File.WriteAllText(StorePath, JsonUtility.ToJson(list));
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalSessionStore write failed: {ex.Message}");
            }
        }
    }
}
