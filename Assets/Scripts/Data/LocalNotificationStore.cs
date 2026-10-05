using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// In-app notifications (SRS §3.2.12). Appointments from §3.2.13 are out of scope
    /// (copied from a lab SRS); session reminders are stored here instead.
    /// </summary>
    public static class LocalNotificationStore
    {
        [Serializable]
        public class NotificationItem
        {
            public string Id;
            public string UserId;
            public string Title;
            public string Body;
            public string CreatedAt;
            public bool Read;
        }

        [Serializable]
        private class StoreFile
        {
            public List<NotificationItem> items = new List<NotificationItem>();
        }

        private static string StorePath => Path.Combine(Application.persistentDataPath, "PhobiaReliefTherapy_notifications.json");

        public static void Add(string userId, string title, string body)
        {
            var store = Load();
            store.items.Add(new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId ?? "",
                Title = title,
                Body = body,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Read = false
            });
            Save(store);
        }

        public static List<NotificationItem> GetForUser(string userId)
        {
            var all = Load().items.FindAll(n =>
                string.IsNullOrEmpty(n.UserId) || n.UserId == userId);
            all.Reverse();
            return all;
        }

        public static int UnreadCount(string userId)
        {
            return GetForUser(userId).FindAll(n => !n.Read).Count;
        }

        public static void MarkAllRead(string userId)
        {
            var store = Load();
            foreach (var item in store.items)
            {
                if (item.UserId == userId || string.IsNullOrEmpty(item.UserId))
                    item.Read = true;
            }
            Save(store);
        }

        private static StoreFile Load()
        {
            if (!File.Exists(StorePath))
                return new StoreFile();

            try
            {
                return JsonUtility.FromJson<StoreFile>(File.ReadAllText(StorePath)) ?? new StoreFile();
            }
            catch
            {
                return new StoreFile();
            }
        }

        private static void Save(StoreFile store)
        {
            try
            {
                File.WriteAllText(StorePath, JsonUtility.ToJson(store, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalNotificationStore write failed: {ex.Message}");
            }
        }
    }
}
