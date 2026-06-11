using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;

namespace PhobiaReliefTherapy.Data
{
    [System.Serializable]
    public class User
    {
        public string Id;
        public string Username;
        public string Email;
        public string CreatedAt;
    }

    [System.Serializable]
    public class TherapySession
    {
        public string Id;
        public string UserId;
        public string Phobia;
        public string Difficulty;
        public int Stage;
        public int BaselineHeartRate;
        public int FinalHeartRate;
        public float ExposureTime;
        public string Feedback;
        public string DateCompleted;
    }

    public class DatabaseManager : MonoBehaviour
    {
        private static DatabaseManager _instance;
        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DatabaseManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DatabaseManager", typeof(DatabaseManager));
                        _instance = go.GetComponent<DatabaseManager>();
                    }
                }
                return _instance;
            }
        }

        // Supabase configuration
        private const string SUPABASE_URL = "https://vzdtagvmpuhyahewooly.supabase.co";
        private const string SUPABASE_ANON_KEY = "sb_publishable_QSBY1oj2CZoXAekpLt5Xcg_72xPbbi4";
        private const string SUPABASE_AUTH_REDIRECT_URL = "https://vzdtagvmpuhyahewooly.supabase.co"; // Must be a reachable web page configured in Supabase Auth; localhost will fail unless you are serving a local callback page.

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // User Management
        public IEnumerator RegisterUser(string username, string password, string email, System.Action<bool, string> callback)
        {
            string url = $"{SUPABASE_URL}/auth/v1/signup";

            var authData = new AuthRequest
            {
                email = email,
                password = password,
                redirect_to = SUPABASE_AUTH_REDIRECT_URL
            };
            string jsonData = JsonUtility.ToJson(authData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(responseBody);
                    if (!string.IsNullOrEmpty(response.user?.id))
                    {
                        if (!string.IsNullOrEmpty(response.access_token))
                        {
                            PlayerPrefs.SetString("auth_token", response.access_token);
                            // Store additional user data using the newly issued auth token.
                            StartCoroutine(SaveUserData(response.user.id, username, email, response.access_token));
                        }
                        callback(true, response.user.id);
                    }
                    else
                    {
                        var error = ParseSupabaseError(responseBody);
                        Debug.LogError($"RegisterUser unexpected response: {responseBody}");
                        callback(false, string.IsNullOrEmpty(error) ? "Registration failed: invalid response from Supabase" : error);
                    }
                }
                else
                {
                    string errorText = !string.IsNullOrEmpty(responseBody) ? ParseSupabaseError(responseBody) : request.error;
                    if (string.IsNullOrEmpty(errorText))
                        errorText = request.error;
                    Debug.LogError($"RegisterUser failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(false, errorText);
                }
            }
        }

        public IEnumerator RecoverPassword(string email, System.Action<bool, string> callback)
        {
            string url = $"{SUPABASE_URL}/auth/v1/recover";

            var recoverData = new RecoverRequest
            {
                email = email,
                redirect_to = SUPABASE_AUTH_REDIRECT_URL
            };
            string jsonData = JsonUtility.ToJson(recoverData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(true, null);
                }
                else
                {
                    string errorText = !string.IsNullOrEmpty(responseBody) ? ParseSupabaseError(responseBody) : request.error;
                    if (string.IsNullOrEmpty(errorText))
                        errorText = request.error;
                    Debug.LogError($"RecoverPassword failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(false, errorText);
                }
            }
        }

        public IEnumerator LookupUsernameByEmail(string email, System.Action<string, string> callback)
        {
            string encodedEmail = UnityWebRequest.EscapeURL(email);
            string url = $"{SUPABASE_URL}/rest/v1/users?email=eq.{encodedEmail}&select=username";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string body = WrapJsonArray(responseBody, "users");
                    var users = JsonUtility.FromJson<UserEmailLookupArray>(body);
                    if (users?.users != null && users.users.Length > 0 && !string.IsNullOrEmpty(users.users[0].username))
                    {
                        callback(users.users[0].username, null);
                    }
                    else
                    {
                        callback(null, "No account found for that email address.");
                    }
                }
                else
                {
                    string errorText = !string.IsNullOrEmpty(responseBody) ? ParseSupabaseError(responseBody) : request.error;
                    if (string.IsNullOrEmpty(errorText))
                        errorText = request.error;
                    Debug.LogError($"LookupUsernameByEmail failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(null, errorText);
                }
            }
        }

        public IEnumerator LoginUser(string email, string password, System.Action<User, string> callback)
        {
            string url = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";

            var authData = new AuthRequest { email = email, password = password };
            string jsonData = JsonUtility.ToJson(authData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_ANON_KEY}");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(responseBody);
                    if (!string.IsNullOrEmpty(response.access_token))
                    {
                        PlayerPrefs.SetString("auth_token", response.access_token);

                        if (!string.IsNullOrEmpty(response.user?.id))
                        {
                            StartCoroutine(GetUserData(response.user.id, (user) =>
                            {
                                if (user != null)
                                {
                                    callback(user, null);
                                }
                                else
                                {
                                    callback(new User { Id = response.user.id, Email = email }, null);
                                }
                            }));
                        }
                        else
                        {
                            Debug.LogWarning($"LoginUser succeeded but response missing user id: {responseBody}");
                            callback(null, "Login succeeded but user information could not be retrieved.");
                        }
                    }
                    else
                    {
                        var error = ParseSupabaseError(responseBody);
                        Debug.LogError($"LoginUser unexpected response: {responseBody}");
                        callback(null, string.IsNullOrEmpty(error) ? "Login failed: invalid response from Supabase" : error);
                    }
                }
                else
                {
                    string errorText = !string.IsNullOrEmpty(responseBody) ? ParseSupabaseError(responseBody) : request.error;
                    if (string.IsNullOrEmpty(errorText))
                        errorText = request.error;
                    Debug.LogError($"LoginUser failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(null, errorText);
                }
            }
        }

        private IEnumerator SaveUserData(string userId, string username, string email, string authToken)
        {
            string url = $"{SUPABASE_URL}/rest/v1/users";

            var userData = new User
            {
                Id = userId,
                Username = username,
                Email = email,
                CreatedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string jsonData = JsonUtility.ToJson(userData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("User data saved");
                }
                else
                {
                    Debug.LogError($"Failed to save user data: {request.error}");
                }
            }
        }

        private IEnumerator GetUserData(string userId, System.Action<User> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/users?id=eq.{userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string body = request.downloadHandler.text;
                    body = WrapJsonArray(body, "users");
                    var users = JsonUtility.FromJson<UserArray>(body);
                    callback(users?.users != null && users.users.Length > 0 ? users.users[0] : null);
                }
                else
                {
                    Debug.LogError($"GetUserData failed: {request.error}");
                    callback(null);
                }
            }
        }

        // Therapy Session Management
        public IEnumerator SaveTherapySession(string userId, string phobia, string difficulty, int stage, int baselineHR, int finalHR, float exposureTime, string feedback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/therapy_sessions";

            var session = new TherapySession
            {
                UserId = userId,
                Phobia = phobia,
                Difficulty = difficulty,
                Stage = stage,
                BaselineHeartRate = baselineHR,
                FinalHeartRate = finalHR,
                ExposureTime = exposureTime,
                Feedback = feedback,
                DateCompleted = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string jsonData = JsonUtility.ToJson(session);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Therapy session saved");
                }
                else
                {
                    Debug.LogError($"Failed to save session: {request.error}");
                }
            }
        }

        public IEnumerator GetUserSessions(string userId, System.Action<TherapySession[]> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/therapy_sessions?user_id=eq.{userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string body = request.downloadHandler.text;
                    body = WrapJsonArray(body, "sessions");
                    var sessions = JsonUtility.FromJson<TherapySessionArray>(body);
                    callback(sessions?.sessions ?? new TherapySession[0]);
                }
                else
                {
                    Debug.LogError($"GetUserSessions failed: {request.error}");
                    callback(new TherapySession[0]);
                }
            }
        }

        private string WrapJsonArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            json = json.Trim();
            if (json.StartsWith("["))
            {
                return $"{{\"{fieldName}\":{json}}}";
            }

            return json;
        }

        // JSON Helper Classes
        [System.Serializable]
        private class AuthRequest
        {
            public string email;
            public string password;
            public string redirect_to;
        }

        [System.Serializable]
        private class RecoverRequest
        {
            public string email;
            public string redirect_to;
        }

        [System.Serializable]
        private class AuthResponse
        {
            public string access_token;
            public SupabaseUser user;
            public string error;
            public string error_description;
            public string message;
            public string msg;
        }

        [System.Serializable]
        private class SupabaseUser
        {
            public string id;
        }

        private string ParseSupabaseError(string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
                return null;

            try
            {
                var errorResponse = JsonUtility.FromJson<AuthResponse>(responseBody);
                if (!string.IsNullOrEmpty(errorResponse.error_description))
                    return errorResponse.error_description;
                if (!string.IsNullOrEmpty(errorResponse.error))
                    return errorResponse.error;
                if (!string.IsNullOrEmpty(errorResponse.message))
                    return errorResponse.message;
                if (!string.IsNullOrEmpty(errorResponse.msg))
                    return errorResponse.msg;
            }
            catch
            {
                // Fall through to raw response
            }

            return responseBody;
        }

        [System.Serializable]
        private class UserArray
        {
            public User[] users;
        }

        [System.Serializable]
        private class UserEmailLookup
        {
            public string username;
        }

        [System.Serializable]
        private class UserEmailLookupArray
        {
            public UserEmailLookup[] users;
        }

        [System.Serializable]
        private class TherapySessionArray
        {
            public TherapySession[] sessions;
        }
    }
}