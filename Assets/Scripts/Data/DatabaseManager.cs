using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace PhobiaReliefTherapy.Data
{
    [System.Serializable]
    public class User
    {
        public string Id;
        public string Username;
        public string Email;
        public string CreatedAt;
        public bool IsAdmin;
        public string AccountStatus;
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
        public int PeakHeartRate;
        public float AverageHeartRate;
        public float ExposureTime;
        public int PanicEventCount;
        public int SafetyTriggerCount;
        public float AveragePanicScore;
        public float HighestPanicScore;
        public string Feedback;
        public string DateCompleted;
    }

    [System.Serializable]
    public class AdminAuditEntry
    {
        public string Id;
        public string AdminUserId;
        public string Action;
        public string TargetUserId;
        public string Details;
        public string CreatedAt;
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

        private const string SUPABASE_URL = "https://vzdtagvmpuhyahewooly.supabase.co";
        private const string SUPABASE_ANON_KEY = "sb_publishable_QSBY1oj2CZoXAekpLt5Xcg_72xPbbi4";
        private const string SUPABASE_SIGNUP_REDIRECT_URL = "https://ahmedtayyab.github.io/PhobiaReliefTherapy/registration-success/";
        private const string SUPABASE_PASSWORD_RESET_REDIRECT_URL = "https://ahmedtayyab.github.io/PhobiaReliefTherapy/password-reset/";

        public static bool CloudReachable { get; private set; }
        public static bool TablesReady { get; private set; }
        public static bool LastRegisterNeedsEmailVerification { get; private set; }
        public static string StatusLabel { get; private set; } = "Cloud: checking...";
        public static string LastError { get; private set; } = "";

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
                return;
            }

            StartCoroutine(ProbeCloudHealth());
        }

        public IEnumerator ProbeCloudHealth()
        {
            long code = 0;
            string body = "";
            yield return PostRpc("prt_health", "{}", (responseCode, responseBody) =>
            {
                code = responseCode;
                body = responseBody;
            });

            if (code >= 200 && code < 300)
            {
                CloudReachable = true;
                TablesReady = true;
                LastError = "";
                StatusLabel = "Cloud: connected";
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/users?select=id&limit=1";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, SUPABASE_ANON_KEY);
                yield return request.SendWebRequest();

                code = request.responseCode;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    CloudReachable = true;
                    TablesReady = true;
                    LastError = "";
                    StatusLabel = "Cloud: connected";
                }
                else if (code == 404)
                {
                    CloudReachable = true;
                    TablesReady = false;
                    LastError = FirstNonEmpty(body, request.downloadHandler?.text, request.error);
                    StatusLabel = "Cloud: setup required";
                    Debug.LogWarning("Supabase tables/RPCs are missing. Apply docs/supabase-admin-schema.sql once.");
                }
                else
                {
                    CloudReachable = false;
                    TablesReady = false;
                    LastError = FirstNonEmpty(request.downloadHandler?.text, request.error);
                    StatusLabel = "Cloud: unreachable";
                    Debug.LogWarning($"Supabase probe failed ({code}): {LastError}");
                }
            }
        }

        public IEnumerator RegisterUser(string username, string password, string email, System.Action<bool, string> callback)
        {
            LastRegisterNeedsEmailVerification = false;

            var rpc = new RegisterRpc
            {
                p_username = username,
                p_email = email,
                p_password = password
            };
            long rpcCode = 0;
            string rpcBody = "";
            yield return PostRpc("prt_register", JsonUtility.ToJson(rpc), (code, body) =>
            {
                rpcCode = code;
                rpcBody = body;
            });

            if (rpcCode >= 200 && rpcCode < 300)
            {
                var profile = ParseUserObject(rpcBody);
                string rpcId = profile != null && !string.IsNullOrEmpty(profile.Id)
                    ? profile.Id
                    : ExtractUuid(rpcBody);
                if (!string.IsNullOrEmpty(rpcId))
                {
                    TablesReady = true;
                    CloudReachable = true;
                    StatusLabel = "Cloud: connected";
                    callback(true, rpcId);
                    yield break;
                }
            }

            string url = $"{SUPABASE_URL}/auth/v1/signup";
            var authData = new AuthRequest
            {
                email = email,
                password = password,
                redirect_to = SUPABASE_SIGNUP_REDIRECT_URL,
                data = new UserMetadata { username = username }
            };

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(authData)));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, SUPABASE_ANON_KEY);

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                long code = request.responseCode;
                string userId = ExtractUuid(responseBody);
                bool httpOk = code >= 200 && code < 300;

                if (httpOk && !string.IsNullOrEmpty(userId))
                {
                    CloudReachable = true;
                    StatusLabel = "Cloud: connected";
                    var response = JsonUtility.FromJson<AuthResponse>(responseBody);
                    LastRegisterNeedsEmailVerification = response == null || string.IsNullOrEmpty(response.access_token);
                    if (!LastRegisterNeedsEmailVerification)
                    {
                        PlayerPrefs.SetString("auth_token", response.access_token);
                        yield return UpsertUserProfile(userId, username, email, false, "active", response.access_token, null);
                    }

                    callback(true, userId);
                    yield break;
                }

                if (httpOk)
                {
                    CloudReachable = true;
                    LastRegisterNeedsEmailVerification = true;
                    callback(true, userId ?? "");
                    yield break;
                }

                string errorText = FirstNonEmpty(ParseSupabaseError(responseBody), ParseSupabaseError(rpcBody), request.error);
                if (LooksLikeAlreadyRegistered(errorText) || LooksLikeAlreadyRegistered(rpcBody))
                {
                    callback(false, "Email already registered. Log in instead.");
                    yield break;
                }

                Debug.LogError($"RegisterUser failed: {errorText}; response: {responseBody}; code: {code}");
                callback(false, FirstNonEmpty(errorText, "Registration failed."));
            }
        }

        private static bool LooksLikeAlreadyRegistered(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return text.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("registered", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("exists", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ExtractUuid(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var auth = JsonUtility.FromJson<AuthResponse>(json);
                if (auth?.user != null && IsUuid(auth.user.id))
                    return auth.user.id;
            }
            catch
            {
                // Fall through.
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                json,
                "\"id\"\\s*:\\s*\"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        public IEnumerator RecoverPassword(string email, System.Action<bool, string> callback)
        {
            string url = $"{SUPABASE_URL}/auth/v1/recover";

            var recoverData = new RecoverRequest
            {
                email = email,
                redirect_to = SUPABASE_PASSWORD_RESET_REDIRECT_URL
            };
            string jsonData = JsonUtility.ToJson(recoverData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, SUPABASE_ANON_KEY);

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(true, null);
                }
                else
                {
                    string errorText = FirstNonEmpty(ParseSupabaseError(responseBody), request.error);
                    Debug.LogError($"RecoverPassword failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(false, errorText);
                }
            }
        }

        public IEnumerator LookupUsernameByEmail(string email, System.Action<string, string> callback)
        {
            var rpc = new EmailRpc { p_email = email };
            long rpcCode = 0;
            string rpcBody = "";
            yield return PostRpc("prt_lookup_username", JsonUtility.ToJson(rpc), (code, body) =>
            {
                rpcCode = code;
                rpcBody = body;
            });

            if (rpcCode >= 200 && rpcCode < 300)
            {
                var lookup = JsonUtility.FromJson<UserEmailLookup>(rpcBody);
                if (!string.IsNullOrEmpty(lookup?.username))
                {
                    callback(lookup.username, null);
                    yield break;
                }
            }

            string encodedEmail = UnityWebRequest.EscapeURL(email);
            string url = $"{SUPABASE_URL}/rest/v1/users?email=eq.{encodedEmail}&select=username";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
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
                    string errorText = FirstNonEmpty(ParseSupabaseError(responseBody), request.error);
                    Debug.LogError($"LookupUsernameByEmail failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(null, errorText);
                }
            }
        }

        public IEnumerator LoginUser(string email, string password, System.Action<User, string> callback)
        {
            var rpc = new LoginRpc { p_email = email, p_password = password };
            long rpcCode = 0;
            string rpcBody = "";
            yield return PostRpc("prt_login", JsonUtility.ToJson(rpc), (code, body) =>
            {
                rpcCode = code;
                rpcBody = body;
            });

            if (rpcCode >= 200 && rpcCode < 300)
            {
                var profile = ParseUserObject(rpcBody);
                if (profile != null && !string.IsNullOrEmpty(profile.Id))
                {
                    PlayerPrefs.SetString("auth_token", SUPABASE_ANON_KEY);
                    CloudReachable = true;
                    TablesReady = true;
                    StatusLabel = "Cloud: connected";
                    callback(profile, null);
                    yield break;
                }
            }

            if (rpcCode > 0 && rpcCode != 404)
            {
                callback(null, FirstNonEmpty(ParseSupabaseError(rpcBody), "Invalid email or password."));
                yield break;
            }

            string url = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";

            var authData = new LoginRequest { email = email, password = password };
            string jsonData = JsonUtility.ToJson(authData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, SUPABASE_ANON_KEY);

                yield return request.SendWebRequest();

                string responseBody = request.downloadHandler.text;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(responseBody);
                    if (!string.IsNullOrEmpty(response.access_token))
                    {
                        PlayerPrefs.SetString("auth_token", response.access_token);

                        if (string.IsNullOrEmpty(response.user?.id))
                        {
                            Debug.LogWarning($"LoginUser succeeded but response missing user id: {responseBody}");
                            callback(null, "Login succeeded but user information could not be retrieved.");
                            yield break;
                        }

                        User profile = null;
                        yield return GetUserData(response.user.id, user => profile = user);

                        if (profile == null)
                        {
                            yield return UpsertUserProfile(
                                response.user.id,
                                email.Contains("@") ? email.Split('@')[0] : email,
                                email,
                                false,
                                "active",
                                response.access_token,
                                null);
                            yield return GetUserData(response.user.id, user => profile = user);
                        }

                        if (profile == null)
                        {
                            profile = new User
                            {
                                Id = response.user.id,
                                Email = email,
                                Username = email.Contains("@") ? email.Split('@')[0] : email,
                                AccountStatus = "active"
                            };
                        }

                        CloudReachable = true;
                        callback(profile, null);
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
                    string errorText = FirstNonEmpty(ParseSupabaseError(responseBody), request.error);
                    Debug.LogError($"LoginUser failed: {errorText}; response: {responseBody}; code: {request.responseCode}");
                    callback(null, errorText);
                }
            }
        }

        public IEnumerator GetUserData(string userId, System.Action<User> callback)
        {
            if (string.IsNullOrEmpty(userId))
            {
                callback(null);
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/users?id=eq.{userId}&select=id,username,email,created_at,is_admin,account_status";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var rows = ParseUserRows(request.downloadHandler.text);
                    callback(rows.Count > 0 ? ToUser(rows[0]) : null);
                }
                else
                {
                    Debug.LogError($"GetUserData failed: {request.error} {request.downloadHandler.text}");
                    callback(null);
                }
            }
        }

        public IEnumerator UpsertUserProfile(string userId, string username, string email, bool isAdmin, string accountStatus, string authToken, System.Action<bool> callback)
        {
            if (!IsUuid(userId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/users";
            var row = new UserUpsertRow
            {
                id = userId,
                username = username ?? "",
                email = email ?? "",
                is_admin = isAdmin,
                account_status = string.IsNullOrEmpty(accountStatus) ? "active" : accountStatus
            };
            string jsonData = JsonUtility.ToJson(row);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, string.IsNullOrEmpty(authToken) ? AuthTokenOrAnon() : authToken);
                request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("User profile saved to Supabase");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"Failed to save user profile: {request.error} {request.downloadHandler.text}");
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator UpdateUserProfile(string userId, string username, string email, string accountStatus, bool? isAdmin, System.Action<bool, string> callback)
        {
            if (!IsUuid(userId))
            {
                callback?.Invoke(false, "Local-only account cannot be updated in Supabase.");
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/users?id=eq.{userId}";
            string jsonData;
            if (isAdmin.HasValue && !string.IsNullOrEmpty(accountStatus) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
            {
                jsonData = JsonUtility.ToJson(new UserStatusPatch
                {
                    account_status = accountStatus,
                    is_admin = isAdmin.Value
                });
            }
            else if (!string.IsNullOrEmpty(accountStatus) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email) && !isAdmin.HasValue)
            {
                jsonData = JsonUtility.ToJson(new UserStatusOnlyPatch { account_status = accountStatus });
            }
            else
            {
                jsonData = JsonUtility.ToJson(new UserProfilePatch
                {
                    username = username,
                    email = email
                });
            }

            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, AuthTokenOrAnon());
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback?.Invoke(true, null);
                else
                    callback?.Invoke(false, FirstNonEmpty(ParseSupabaseError(request.downloadHandler.text), request.error));
            }
        }

        public IEnumerator DeleteUserProfile(string userId, System.Action<bool, string> callback)
        {
            if (!IsUuid(userId))
            {
                callback?.Invoke(false, "Local-only account.");
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/users?id=eq.{userId}";
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback?.Invoke(true, null);
                else
                    callback?.Invoke(false, FirstNonEmpty(ParseSupabaseError(request.downloadHandler.text), request.error));
            }
        }

        public IEnumerator UpdateAuthPassword(string newPassword, System.Action<bool, string> callback)
        {
            if (!IsUuid(UserData.UserId))
            {
                callback?.Invoke(false, "Not signed in to cloud.");
                yield break;
            }

            var rpc = new PasswordRpc { p_user_id = UserData.UserId, p_new_password = newPassword };
            long rpcCode = 0;
            string rpcBody = "";
            yield return PostRpc("prt_set_password", JsonUtility.ToJson(rpc), (code, body) =>
            {
                rpcCode = code;
                rpcBody = body;
            });

            if (rpcCode >= 200 && rpcCode < 300)
            {
                callback?.Invoke(true, null);
                yield break;
            }

            callback?.Invoke(false, FirstNonEmpty(ParseSupabaseError(rpcBody), "Could not update password."));
        }

        public IEnumerator SaveTherapySession(string userId, string phobia, string difficulty, int stage, int baselineHR, int finalHR, float exposureTime, string feedback, System.Action<bool> callback = null)
        {
            var local = new LocalSessionStore.StoredSession
            {
                UserId = userId,
                Phobia = phobia,
                Difficulty = difficulty,
                Stage = stage,
                BaselineHeartRate = baselineHR,
                FinalHeartRate = finalHR,
                ExposureTime = exposureTime,
                Feedback = feedback,
                DateCompleted = DateTime.UtcNow.ToString("o")
            };
            yield return SaveTherapySessionFromLocal(local, callback);
        }

        public IEnumerator SaveTherapySessionFromLocal(LocalSessionStore.StoredSession session, System.Action<bool> callback = null)
        {
            if (session == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            if (session.Synced)
            {
                callback?.Invoke(true);
                yield break;
            }

            if (!IsUuid(session.UserId))
            {
                Debug.LogWarning("Skipping Supabase session save: user id is not a cloud UUID (local account).");
                callback?.Invoke(false);
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/therapy_sessions";
            var row = new SessionInsertRow
            {
                user_id = session.UserId,
                phobia = session.Phobia ?? "",
                difficulty = session.Difficulty ?? "",
                stage = session.Stage,
                baseline_heart_rate = session.BaselineHeartRate,
                final_heart_rate = session.FinalHeartRate,
                peak_heart_rate = session.PeakHeartRate,
                average_heart_rate = session.AverageHeartRate,
                exposure_time = session.ExposureTime,
                panic_event_count = session.PanicEventCount,
                safety_trigger_count = session.SafetyTriggerCount,
                average_panic_score = session.AveragePanicScore,
                highest_panic_score = session.HighestPanicScore,
                feedback = session.Feedback ?? "",
                date_completed = ToIsoTimestamp(session.DateCompleted)
            };

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(row)));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, AuthTokenOrAnon());
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Therapy session saved to Supabase");
                    TablesReady = true;
                    CloudReachable = true;
                    StatusLabel = "Cloud: connected";
                    LocalSessionStore.MarkSynced(session);
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"Failed to save session: {request.error} {request.downloadHandler.text}");
                    LastError = FirstNonEmpty(request.downloadHandler.text, request.error);
                    callback?.Invoke(false);
                }
            }
        }

        public IEnumerator SyncPendingLocalSessions()
        {
            var pending = LocalSessionStore.GetUnsyncedSessions();
            foreach (var local in pending)
            {
                bool done = false;
                yield return SaveTherapySessionFromLocal(local, _ => { done = true; });
                while (!done)
                    yield return null;
            }
        }

        public IEnumerator GetUserSessions(string userId, System.Action<TherapySession[]> callback)
        {
            if (string.IsNullOrEmpty(userId) || !IsUuid(userId))
            {
                callback(new TherapySession[0]);
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/therapy_sessions?user_id=eq.{userId}&select=*&order=date_completed.desc&limit=1000";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(ParseSessions(request.downloadHandler.text));
                }
                else
                {
                    Debug.LogError($"GetUserSessions failed: {request.error} {request.downloadHandler.text}");
                    callback(new TherapySession[0]);
                }
            }
        }

        public IEnumerator GetAllUsers(System.Action<User[]> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/users?select=id,username,email,created_at,is_admin,account_status&order=created_at.desc";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var rows = ParseUserRows(request.downloadHandler.text);
                    var users = new User[rows.Count];
                    for (int i = 0; i < rows.Count; i++)
                        users[i] = ToUser(rows[i]);
                    callback(users);
                }
                else
                {
                    Debug.LogError($"GetAllUsers failed: {request.error} {request.downloadHandler.text}");
                    callback(new User[0]);
                }
            }
        }

        public IEnumerator GetAllSessions(System.Action<TherapySession[]> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/therapy_sessions?select=*&order=date_completed.desc&limit=1000";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                request.SetRequestHeader("Range", "0-999");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback(ParseSessions(request.downloadHandler.text));
                else
                {
                    Debug.LogError($"GetAllSessions failed: {request.error} {request.downloadHandler.text}");
                    callback(new TherapySession[0]);
                }
            }
        }

        public IEnumerator GetAuditLog(System.Action<AdminAuditEntry[]> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/admin_audit_log?select=*&order=created_at.desc";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                ApplyApiHeaders(request, AuthTokenOrAnon());
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback(ParseAudit(request.downloadHandler.text));
                else
                {
                    Debug.LogError($"GetAuditLog failed: {request.error} {request.downloadHandler.text}");
                    callback(new AdminAuditEntry[0]);
                }
            }
        }

        public IEnumerator InsertAuditLog(string adminUserId, string action, string targetUserId, string details, System.Action<bool> callback = null)
        {
            if (!IsUuid(adminUserId))
            {
                callback?.Invoke(false);
                yield break;
            }

            string url = $"{SUPABASE_URL}/rest/v1/admin_audit_log";
            var row = new AuditInsertRow
            {
                admin_user_id = adminUserId,
                action = action ?? "",
                details = details ?? "",
                created_at = DateTime.UtcNow.ToString("o")
            };
            if (IsUuid(targetUserId))
                row.target_user_id = targetUserId;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(row)));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, AuthTokenOrAnon());
                request.SetRequestHeader("Prefer", "return=minimal");

                yield return request.SendWebRequest();
                callback?.Invoke(request.result == UnityWebRequest.Result.Success);
            }
        }

        public static bool IsUuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        public static LocalSessionStore.StoredSession ToStoredSession(TherapySession session)
        {
            if (session == null)
                return null;

            return new LocalSessionStore.StoredSession
            {
                Id = session.Id,
                UserId = session.UserId,
                Phobia = session.Phobia,
                Difficulty = session.Difficulty,
                Stage = session.Stage,
                BaselineHeartRate = session.BaselineHeartRate,
                FinalHeartRate = session.FinalHeartRate,
                PeakHeartRate = session.PeakHeartRate,
                AverageHeartRate = session.AverageHeartRate,
                ExposureTime = session.ExposureTime,
                PanicEventCount = session.PanicEventCount,
                SafetyTriggerCount = session.SafetyTriggerCount,
                HighestPanicScore = session.HighestPanicScore,
                AveragePanicScore = session.AveragePanicScore,
                Feedback = session.Feedback,
                DateCompleted = session.DateCompleted,
                Synced = true
            };
        }

        public static List<LocalSessionStore.StoredSession> MergeSessions(List<LocalSessionStore.StoredSession> local, TherapySession[] remote)
        {
            var merged = new List<LocalSessionStore.StoredSession>();
            var indexByKey = new Dictionary<string, int>();

            void AddOrReplace(LocalSessionStore.StoredSession session)
            {
                if (session == null)
                    return;

                string key = SessionKey(session);
                if (indexByKey.TryGetValue(key, out int existing))
                {
                    if (IsRicher(session, merged[existing]))
                        merged[existing] = session;
                    return;
                }

                indexByKey[key] = merged.Count;
                merged.Add(session);
            }

            if (local != null)
            {
                foreach (var session in local)
                    AddOrReplace(session);
            }

            if (remote != null)
            {
                foreach (var session in remote)
                    AddOrReplace(ToStoredSession(session));
            }

            merged.Sort((a, b) => string.CompareOrdinal(
                LocalSessionStore.NormalizeSortKey(b.DateCompleted),
                LocalSessionStore.NormalizeSortKey(a.DateCompleted)));
            return merged;
        }

        private static bool IsRicher(LocalSessionStore.StoredSession candidate, LocalSessionStore.StoredSession existing)
        {
            int candidateScore =
                candidate.PeakHeartRate + candidate.BaselineHeartRate +
                Mathf.RoundToInt(candidate.AverageHeartRate) +
                Mathf.RoundToInt(candidate.ExposureTime) +
                candidate.PanicEventCount;
            int existingScore =
                existing.PeakHeartRate + existing.BaselineHeartRate +
                Mathf.RoundToInt(existing.AverageHeartRate) +
                Mathf.RoundToInt(existing.ExposureTime) +
                existing.PanicEventCount;
            return candidateScore > existingScore;
        }

        private static string SessionKey(LocalSessionStore.StoredSession session)
        {
            string stamp = LocalSessionStore.NormalizeSortKey(session.DateCompleted);
            if (stamp.Length >= 19)
                stamp = stamp.Substring(0, 19);
            return $"{session.UserId}|{session.Phobia}|{session.Stage}|{session.PeakHeartRate}|{session.ExposureTime:F0}|{session.PanicEventCount}|{stamp}";
        }

        private List<UserRow> ParseUserRows(string json)
        {
            string body = WrapJsonArray(json, "users");
            var parsed = JsonUtility.FromJson<UserRowArray>(body);
            if (parsed?.users == null)
                return new List<UserRow>();
            return new List<UserRow>(parsed.users);
        }

        private TherapySession[] ParseSessions(string json)
        {
            string body = WrapJsonArray(json, "sessions");
            var parsed = JsonUtility.FromJson<SessionRowArray>(body);
            if (parsed?.sessions == null)
                return new TherapySession[0];

            var sessions = new TherapySession[parsed.sessions.Length];
            for (int i = 0; i < parsed.sessions.Length; i++)
                sessions[i] = ToSession(parsed.sessions[i]);
            return sessions;
        }

        private AdminAuditEntry[] ParseAudit(string json)
        {
            string body = WrapJsonArray(json, "entries");
            var parsed = JsonUtility.FromJson<AuditRowArray>(body);
            if (parsed?.entries == null)
                return new AdminAuditEntry[0];

            var entries = new AdminAuditEntry[parsed.entries.Length];
            for (int i = 0; i < parsed.entries.Length; i++)
            {
                var row = parsed.entries[i];
                entries[i] = new AdminAuditEntry
                {
                    Id = row.id,
                    AdminUserId = row.admin_user_id,
                    Action = row.action,
                    TargetUserId = row.target_user_id,
                    Details = row.details,
                    CreatedAt = row.created_at
                };
            }
            return entries;
        }

        private static User ToUser(UserRow row)
        {
            return new User
            {
                Id = row.id,
                Username = row.username,
                Email = row.email,
                CreatedAt = row.created_at,
                IsAdmin = row.is_admin,
                AccountStatus = string.IsNullOrEmpty(row.account_status) ? "active" : row.account_status
            };
        }

        private static TherapySession ToSession(SessionRow row)
        {
            return new TherapySession
            {
                Id = row.id,
                UserId = row.user_id,
                Phobia = row.phobia,
                Difficulty = row.difficulty,
                Stage = row.stage,
                BaselineHeartRate = row.baseline_heart_rate,
                FinalHeartRate = row.final_heart_rate,
                PeakHeartRate = row.peak_heart_rate,
                AverageHeartRate = row.average_heart_rate,
                ExposureTime = row.exposure_time,
                PanicEventCount = row.panic_event_count,
                SafetyTriggerCount = row.safety_trigger_count,
                AveragePanicScore = row.average_panic_score,
                HighestPanicScore = row.highest_panic_score,
                Feedback = row.feedback,
                DateCompleted = row.date_completed
            };
        }

        private IEnumerator PostRpc(string functionName, string json, System.Action<long, string> callback)
        {
            string url = $"{SUPABASE_URL}/rest/v1/rpc/{functionName}";
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json));
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyJsonHeaders(request, SUPABASE_ANON_KEY);
                yield return request.SendWebRequest();
                callback?.Invoke(request.responseCode, request.downloadHandler?.text ?? "");
            }
        }

        private User ParseUserObject(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            json = json.Trim();
            if (json.StartsWith("["))
            {
                var rows = ParseUserRows(json);
                return rows.Count > 0 ? ToUser(rows[0]) : null;
            }

            try
            {
                var row = JsonUtility.FromJson<UserRow>(json);
                if (row == null || string.IsNullOrEmpty(row.id))
                    return null;
                return ToUser(row);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyApiHeaders(UnityWebRequest request, string bearer)
        {
            request.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
            request.SetRequestHeader("Authorization", $"Bearer {bearer}");
            request.SetRequestHeader("Accept", "application/json");
        }

        private void ApplyJsonHeaders(UnityWebRequest request, string bearer)
        {
            ApplyApiHeaders(request, bearer);
            request.SetRequestHeader("Content-Type", "application/json");
        }

        private string AuthTokenOrAnon()
        {
            string token = PlayerPrefs.GetString("auth_token", "");
            return string.IsNullOrEmpty(token) ? SUPABASE_ANON_KEY : token;
        }

        private static string ToIsoTimestamp(string value)
        {
            if (DateTime.TryParse(value, out DateTime parsed))
                return parsed.ToUniversalTime().ToString("o");
            return DateTime.UtcNow.ToString("o");
        }

        private string WrapJsonArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json))
                return $"{{\"{fieldName}\":[]}}";

            json = json.Trim();
            if (json.StartsWith("["))
                return $"{{\"{fieldName}\":{json}}}";

            return json;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return "";
        }

        [System.Serializable]
        private class LoginRpc
        {
            public string p_email;
            public string p_password;
        }

        [System.Serializable]
        private class RegisterRpc
        {
            public string p_username;
            public string p_email;
            public string p_password;
        }

        [System.Serializable]
        private class EmailRpc
        {
            public string p_email;
        }

        [System.Serializable]
        private class PasswordRpc
        {
            public string p_user_id;
            public string p_new_password;
        }

        [System.Serializable]
        private class LoginRequest
        {
            public string email;
            public string password;
        }

        [System.Serializable]
        private class AuthRequest
        {
            public string email;
            public string password;
            public string redirect_to;
            public UserMetadata data;
        }

        [System.Serializable]
        private class UserMetadata
        {
            public string username;
        }

        [System.Serializable]
        private class RecoverRequest
        {
            public string email;
            public string redirect_to;
        }

        [System.Serializable]
        private class PasswordUpdate
        {
            public string password;
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
        private class UserRow
        {
            public string id;
            public string username;
            public string email;
            public string created_at;
            public bool is_admin;
            public string account_status;
        }

        [System.Serializable]
        private class UserUpsertRow
        {
            public string id;
            public string username;
            public string email;
            public bool is_admin;
            public string account_status;
        }

        [System.Serializable]
        private class UserProfilePatch
        {
            public string username;
            public string email;
        }

        [System.Serializable]
        private class UserStatusPatch
        {
            public string account_status;
            public bool is_admin;
        }

        [System.Serializable]
        private class UserStatusOnlyPatch
        {
            public string account_status;
        }

        [System.Serializable]
        private class UserRowArray
        {
            public UserRow[] users;
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
        private class SessionInsertRow
        {
            public string user_id;
            public string phobia;
            public string difficulty;
            public int stage;
            public int baseline_heart_rate;
            public int final_heart_rate;
            public int peak_heart_rate;
            public float average_heart_rate;
            public float exposure_time;
            public int panic_event_count;
            public int safety_trigger_count;
            public float average_panic_score;
            public float highest_panic_score;
            public string feedback;
            public string date_completed;
        }

        [System.Serializable]
        private class SessionRow
        {
            public string id;
            public string user_id;
            public string phobia;
            public string difficulty;
            public int stage;
            public int baseline_heart_rate;
            public int final_heart_rate;
            public int peak_heart_rate;
            public float average_heart_rate;
            public float exposure_time;
            public int panic_event_count;
            public int safety_trigger_count;
            public float average_panic_score;
            public float highest_panic_score;
            public string feedback;
            public string date_completed;
        }

        [System.Serializable]
        private class SessionRowArray
        {
            public SessionRow[] sessions;
        }

        [System.Serializable]
        private class AuditInsertRow
        {
            public string admin_user_id;
            public string action;
            public string target_user_id;
            public string details;
            public string created_at;
        }

        [System.Serializable]
        private class AuditRowArray
        {
            public AdminAuditEntryDummy[] entries;
        }

        [System.Serializable]
        private class AdminAuditEntryDummy
        {
            public string id;
            public string admin_user_id;
            public string action;
            public string target_user_id;
            public string details;
            public string created_at;
        }
    }
}
