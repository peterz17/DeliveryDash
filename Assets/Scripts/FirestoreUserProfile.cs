using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class UserProfileData
{
    public int coins;
    public int unlockedHeart;
    public int unlockedRush;
    public int unlockedHeartExtreme;
    public int unlockedRushExtreme;
    public int bestScoreHeart;
    public int bestScoreRush;
    public int bestScoreEndless;
    public int bestScoreHeartExtreme;
    public int bestScoreRushExtreme;
    public int bestTierEndless;
    public int endlessTier10Count;
    public List<string> unlockedCars = new List<string>();
    public string selectedCar = "";
    public string playerName = "";
    public string email = "";
    public List<string> linkedProviders = new List<string>();
}

public static class FirestoreUserProfile
{
    const string BASE_URL =
        "https://firestore.googleapis.com/v1/projects/deliverydash-b47d0/databases/(default)/documents";

    static Coroutine _saveCoroutine;
    static MonoBehaviour _saveRunner;
    static float _saveDelay = 1.5f;
    static bool _syncing;
    static readonly List<Action> _pendingCallbacks = new List<Action>();
    static readonly HashSet<string> _dirtyFields = new HashSet<string>();

    // ── Public API ─────────────────────────────────────────────────────────

    public static void SyncOnLogin(Action onComplete)
    {
        if (_syncing)
        {
            if (onComplete != null) _pendingCallbacks.Add(onComplete);
            return;
        }
        _syncing = true;
        var go = new GameObject("FirestoreProfileSync");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(SyncCoroutine(onComplete, go));
    }

    public static void QueueSave(params string[] fields)
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated) return;
        foreach (var f in fields) _dirtyFields.Add(f);
        if (_syncing) return;
        if (_saveRunner == null)
        {
            var go = new GameObject("FirestoreProfileSaver");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _saveRunner = go.AddComponent<CoroutineRunner>();
        }
        if (_saveCoroutine != null) _saveRunner.StopCoroutine(_saveCoroutine);
        _saveCoroutine = _saveRunner.StartCoroutine(DebouncedSave());
    }

    // ── Sync orchestrator ──────────────────────────────────────────────────

    static IEnumerator SyncCoroutine(Action onComplete, GameObject runner)
    {
        string uid = AuthManager.Instance != null ? AuthManager.Instance.UserId : "";
        if (string.IsNullOrEmpty(uid))
        {
            Debug.Log("[Profile] No uid, skipping sync");
            onComplete?.Invoke();
            UnityEngine.Object.Destroy(runner);
            yield break;
        }

        var local = ProfileFromLocal();
        UserProfileData cloud = null;
        bool loaded = false;
        bool notFound = false;
        bool failed = false;

        yield return LoadProfile(uid,
            data => { cloud = data; loaded = true; },
            () => { notFound = true; loaded = true; },
            () => { failed = true; loaded = true; });

        while (!loaded) yield return null;

        if (failed)
        {
            Debug.LogWarning("[Profile] Cloud load failed, using local data");
            _syncing = false;
            onComplete?.Invoke();
            foreach (var cb in _pendingCallbacks) cb?.Invoke();
            _pendingCallbacks.Clear();
            UnityEngine.Object.Destroy(runner);
            yield break;
        }

        if (notFound)
        {
            Debug.Log("[Profile] No cloud profile, uploading local as initial");
            ApplyToLocal(local);
            yield return SaveProfileCoroutine(uid, local, runner, retried: false, destroyRunner: false);
        }
        else
        {
            Debug.Log("[Profile] Cloud is source of truth, applying to local");
            ApplyToLocal(cloud);
        }

        _syncing = false;
        onComplete?.Invoke();
        foreach (var cb in _pendingCallbacks) cb?.Invoke();
        _pendingCallbacks.Clear();
        if (_dirtyFields.Count > 0) QueueSave();
        UnityEngine.Object.Destroy(runner);
    }

    // ── Debounced save ─────────────────────────────────────────────────────

    static IEnumerator DebouncedSave()
    {
        yield return new WaitForSecondsRealtime(_saveDelay);
        _saveCoroutine = null;

        string uid = AuthManager.Instance != null ? AuthManager.Instance.UserId : "";
        if (string.IsNullOrEmpty(uid) || _dirtyFields.Count == 0) yield break;

        var fields = new HashSet<string>(_dirtyFields);
        fields.Add("updatedAt");
        _dirtyFields.Clear();

        var data = ProfileFromLocal();
        string json = BuildPartialJson(data, fields);
        string mask = BuildUpdateMask(fields);

        var go = new GameObject("FirestoreProfileUpload");
        go.hideFlags = HideFlags.HideAndDontSave;
        var r = go.AddComponent<CoroutineRunner>();
        r.StartCoroutine(PatchFieldsCoroutine(uid, json, mask, go));
    }

    static IEnumerator PatchFieldsCoroutine(string uid, string json, string mask, GameObject runner)
    {
        string url = BASE_URL + "/users/" + uid + "?" + mask;

        var request = new UnityWebRequest(url, "PATCH");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("[Profile] Partial save OK: " + mask);
        else if (request.responseCode == 401 && AuthManager.Instance != null)
        {
            request.Dispose();
            bool done = false, ok = false;
            AuthManager.Instance.RefreshToken(() => { ok = true; done = true; }, _ => { done = true; });
            while (!done) yield return null;
            if (ok)
            {
                var req2 = new UnityWebRequest(BASE_URL + "/users/" + uid + "?" + mask, "PATCH");
                req2.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req2.downloadHandler = new DownloadHandlerBuffer();
                req2.SetRequestHeader("Content-Type", "application/json");
                AddAuthHeader(req2);
                yield return req2.SendWebRequest();
                if (req2.result == UnityWebRequest.Result.Success)
                    Debug.Log("[Profile] Partial save OK (retry): " + mask);
                else
                    Debug.LogWarning("[Profile] Partial save failed: " + req2.error);
                req2.Dispose();
            }
            UnityEngine.Object.Destroy(runner);
            yield break;
        }
        else
            Debug.LogWarning("[Profile] Partial save failed: " + request.error);

        request.Dispose();
        UnityEngine.Object.Destroy(runner);
    }

    // ── Load from Firestore ────────────────────────────────────────────────

    static IEnumerator LoadProfile(string uid, Action<UserProfileData> onLoaded, Action onNotFound, Action onError)
    {
        string url = BASE_URL + "/users/" + uid;

        var request = UnityWebRequest.Get(url);
        AddAuthHeader(request);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var data = ParseProfile(request.downloadHandler.text);
            request.Dispose();
            onLoaded?.Invoke(data);
        }
        else if (request.responseCode == 404)
        {
            request.Dispose();
            onNotFound?.Invoke();
        }
        else if (request.responseCode == 401 && AuthManager.Instance != null)
        {
            request.Dispose();
            bool refreshDone = false;
            bool refreshOk = false;
            AuthManager.Instance.RefreshToken(
                () => { refreshOk = true; refreshDone = true; },
                _ => { refreshDone = true; }
            );
            while (!refreshDone) yield return null;
            if (refreshOk)
                yield return LoadProfile(uid, onLoaded, onNotFound, onError);
            else
                onError?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Profile] Load failed: " + request.error);
            request.Dispose();
            onError?.Invoke();
        }
    }

    // ── Save to Firestore ──────────────────────────────────────────────────

    static IEnumerator SaveProfileCoroutine(string uid, UserProfileData data, GameObject runner, bool retried, bool destroyRunner)
    {
        string url = BASE_URL + "/users/" + uid;

        string json = BuildProfileJson(data);

        var request = new UnityWebRequest(url, "PATCH");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Profile] Saved to cloud for uid=" + uid);
        }
        else if (!retried && request.responseCode == 401 && AuthManager.Instance != null)
        {
            Debug.Log("[Profile] Got 401, refreshing token and retrying...");
            request.Dispose();
            bool refreshDone = false;
            bool refreshOk = false;
            AuthManager.Instance.RefreshToken(
                () => { refreshOk = true; refreshDone = true; },
                _ => { refreshDone = true; }
            );
            while (!refreshDone) yield return null;
            if (refreshOk)
                yield return SaveProfileCoroutine(uid, data, runner, retried: true, destroyRunner);
            else
                Debug.LogWarning("[Profile] Token refresh failed, save aborted");
            yield break;
        }
        else
        {
            Debug.LogWarning("[Profile] Save failed: " + request.error + " " + request.downloadHandler.text);
        }

        request.Dispose();
        if (destroyRunner) UnityEngine.Object.Destroy(runner);
    }

    // ── Build local profile from PlayerPrefs ───────────────────────────────

    public static UserProfileData ProfileFromLocal()
    {
        var data = new UserProfileData();
        data.coins = PlayerPrefs.GetInt("Coins", 0);
        data.unlockedHeart = PlayerPrefs.GetInt("Unlocked_Normal", 0);
        data.unlockedRush = PlayerPrefs.GetInt("Unlocked_Rush", 0);
        data.unlockedHeartExtreme = PlayerPrefs.GetInt("Unlocked_HeartExtreme", 0);
        data.unlockedRushExtreme = PlayerPrefs.GetInt("Unlocked_RushExtreme", 0);
        data.bestScoreHeart = PlayerPrefs.GetInt("BestScore_Heart", 0);
        data.bestScoreRush = PlayerPrefs.GetInt("BestScore_Rush", 0);
        data.bestScoreEndless = PlayerPrefs.GetInt("BestScore_Endless", 0);
        data.bestScoreHeartExtreme = PlayerPrefs.GetInt("BestScore_HeartExtreme", 0);
        data.bestScoreRushExtreme = PlayerPrefs.GetInt("BestScore_RushExtreme", 0);
        data.bestTierEndless = PlayerPrefs.GetInt("BestTier_Endless", 0);
        data.endlessTier10Count = PlayerPrefs.GetInt("EndlessTier10Count", 0);
        data.selectedCar = PlayerPrefs.GetString("SelectedCar", "");
        data.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        data.email = AuthManager.Instance != null ? AuthManager.Instance.Email : "";
        data.linkedProviders = new List<string>();
        if (AuthManager.Instance != null && AuthManager.Instance.Provider != AuthProviderType.Guest)
            data.linkedProviders.Add(AuthManager.Instance.Provider.ToString());

        data.unlockedCars = new List<string>();
        if (GameManager.Instance != null && GameManager.Instance.carCatalog != null)
        {
            foreach (var car in GameManager.Instance.carCatalog)
            {
                if (car == null) continue;
                if (PlayerPrefs.GetInt("CarUnlocked_" + car.carId, 0) == 1)
                    data.unlockedCars.Add(car.carId);
            }
        }

        return data;
    }

    // ── Apply cloud profile to PlayerPrefs ─────────────────────────────────

    public static void ApplyToLocal(UserProfileData data)
    {
        Debug.Log("[Profile] ApplyToLocal: coins=" + data.coins
            + " heart=" + data.unlockedHeart
            + " rush=" + data.unlockedRush
            + " cars=" + (data.unlockedCars != null ? data.unlockedCars.Count : 0)
            + " selectedCar=" + data.selectedCar
            + " name=" + data.playerName);
        PlayerPrefs.SetInt("Coins", data.coins);
        PlayerPrefs.SetInt("Unlocked_Normal", data.unlockedHeart);
        PlayerPrefs.SetInt("Unlocked_Rush", data.unlockedRush);
        PlayerPrefs.SetInt("Unlocked_HeartExtreme", data.unlockedHeartExtreme);
        PlayerPrefs.SetInt("Unlocked_RushExtreme", data.unlockedRushExtreme);
        PlayerPrefs.SetInt("BestScore_Heart", data.bestScoreHeart);
        PlayerPrefs.SetInt("BestScore_Rush", data.bestScoreRush);
        PlayerPrefs.SetInt("BestScore_Endless", data.bestScoreEndless);
        PlayerPrefs.SetInt("BestScore_HeartExtreme", data.bestScoreHeartExtreme);
        PlayerPrefs.SetInt("BestScore_RushExtreme", data.bestScoreRushExtreme);
        PlayerPrefs.SetInt("BestTier_Endless", data.bestTierEndless);
        PlayerPrefs.SetInt("EndlessTier10Count", data.endlessTier10Count);

        if (!string.IsNullOrEmpty(data.selectedCar))
            PlayerPrefs.SetString("SelectedCar", data.selectedCar);
        if (!string.IsNullOrEmpty(data.playerName) && data.playerName != "Player")
            PlayerPrefs.SetString("PlayerName", data.playerName);

        if (data.unlockedCars != null)
        {
            foreach (var carId in data.unlockedCars)
                PlayerPrefs.SetInt("CarUnlocked_" + carId, 1);
        }

        PlayerPrefs.Save();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadSelectedCar();
    }

    // ── Partial JSON (only dirty fields) ────────────────────────────────────

    static readonly Dictionary<string, System.Func<UserProfileData, string>> FieldBuilders =
        new Dictionary<string, System.Func<UserProfileData, string>>
    {
        ["coins"]                = d => IntField("coins", d.coins),
        ["unlockedHeart"]        = d => IntField("unlockedHeart", d.unlockedHeart),
        ["unlockedRush"]         = d => IntField("unlockedRush", d.unlockedRush),
        ["unlockedHeartExtreme"] = d => IntField("unlockedHeartExtreme", d.unlockedHeartExtreme),
        ["unlockedRushExtreme"]  = d => IntField("unlockedRushExtreme", d.unlockedRushExtreme),
        ["bestScoreHeart"]       = d => IntField("bestScoreHeart", d.bestScoreHeart),
        ["bestScoreRush"]        = d => IntField("bestScoreRush", d.bestScoreRush),
        ["bestScoreEndless"]     = d => IntField("bestScoreEndless", d.bestScoreEndless),
        ["bestScoreHeartExtreme"]= d => IntField("bestScoreHeartExtreme", d.bestScoreHeartExtreme),
        ["bestScoreRushExtreme"] = d => IntField("bestScoreRushExtreme", d.bestScoreRushExtreme),
        ["bestTierEndless"]      = d => IntField("bestTierEndless", d.bestTierEndless),
        ["endlessTier10Count"]   = d => IntField("endlessTier10Count", d.endlessTier10Count),
        ["selectedCar"]          = d => StrField("selectedCar", d.selectedCar),
        ["playerName"]           = d => StrField("playerName", d.playerName),
        ["email"]                = d => StrField("email", d.email),
        ["updatedAt"]            = d => StrField("updatedAt", DateTime.UtcNow.ToString("o")),
    };

    static string BuildPartialJson(UserProfileData data, HashSet<string> fields)
    {
        var sb = new StringBuilder();
        sb.Append("{\"fields\":{");
        bool first = true;

        foreach (var f in fields)
        {
            // Handle arrays separately
            if (f == "unlockedCars" || f == "linkedProviders") continue;

            if (FieldBuilders.TryGetValue(f, out var builder))
            {
                if (!first) sb.Append(',');
                sb.Append(builder(data));
                first = false;
            }
        }

        if (fields.Contains("unlockedCars"))
        {
            if (!first) sb.Append(',');
            sb.Append("\"unlockedCars\":{\"arrayValue\":{\"values\":[");
            if (data.unlockedCars != null)
                for (int i = 0; i < data.unlockedCars.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"stringValue\":\"" + Esc(data.unlockedCars[i]) + "\"}");
                }
            sb.Append("]}}");
            first = false;
        }

        if (fields.Contains("linkedProviders"))
        {
            if (!first) sb.Append(',');
            sb.Append("\"linkedProviders\":{\"arrayValue\":{\"values\":[");
            if (data.linkedProviders != null)
                for (int i = 0; i < data.linkedProviders.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append("{\"stringValue\":\"" + Esc(data.linkedProviders[i]) + "\"}");
                }
            sb.Append("]}}");
        }

        sb.Append("}}");
        return sb.ToString();
    }

    static string BuildUpdateMask(HashSet<string> fields)
    {
        var sb = new StringBuilder();
        foreach (var f in fields)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append("updateMask.fieldPaths=" + f);
        }
        return sb.ToString();
    }

    // ── JSON builders (Firestore REST format) ──────────────────────────────

    static string BuildProfileJson(UserProfileData data)
    {
        var sb = new StringBuilder();
        sb.Append("{\"fields\":{");
        sb.Append(IntField("coins", data.coins)); sb.Append(",");
        sb.Append(IntField("unlockedHeart", data.unlockedHeart)); sb.Append(",");
        sb.Append(IntField("unlockedRush", data.unlockedRush)); sb.Append(",");
        sb.Append(IntField("unlockedHeartExtreme", data.unlockedHeartExtreme)); sb.Append(",");
        sb.Append(IntField("unlockedRushExtreme", data.unlockedRushExtreme)); sb.Append(",");
        sb.Append(IntField("bestScoreHeart", data.bestScoreHeart)); sb.Append(",");
        sb.Append(IntField("bestScoreRush", data.bestScoreRush)); sb.Append(",");
        sb.Append(IntField("bestScoreEndless", data.bestScoreEndless)); sb.Append(",");
        sb.Append(IntField("bestScoreHeartExtreme", data.bestScoreHeartExtreme)); sb.Append(",");
        sb.Append(IntField("bestScoreRushExtreme", data.bestScoreRushExtreme)); sb.Append(",");
        sb.Append(IntField("bestTierEndless", data.bestTierEndless)); sb.Append(",");
        sb.Append(IntField("endlessTier10Count", data.endlessTier10Count)); sb.Append(",");
        sb.Append(StrField("selectedCar", data.selectedCar)); sb.Append(",");
        sb.Append(StrField("playerName", data.playerName)); sb.Append(",");
        sb.Append(StrField("email", data.email)); sb.Append(",");
        sb.Append(StrField("updatedAt", DateTime.UtcNow.ToString("o"))); sb.Append(",");

        // Array of unlocked cars
        sb.Append("\"unlockedCars\":{\"arrayValue\":{\"values\":[");
        if (data.unlockedCars != null)
        {
            for (int i = 0; i < data.unlockedCars.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"stringValue\":\"" + Esc(data.unlockedCars[i]) + "\"}");
            }
        }
        sb.Append("]}},");

        // Array of linked providers
        sb.Append("\"linkedProviders\":{\"arrayValue\":{\"values\":[");
        if (data.linkedProviders != null)
        {
            for (int i = 0; i < data.linkedProviders.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"stringValue\":\"" + Esc(data.linkedProviders[i]) + "\"}");
            }
        }
        sb.Append("]}}");

        sb.Append("}}");
        return sb.ToString();
    }

    static string IntField(string name, int val) =>
        "\"" + name + "\":{\"integerValue\":\"" + val + "\"}";

    static string StrField(string name, string val) =>
        "\"" + name + "\":{\"stringValue\":\"" + Esc(val) + "\"}";

    // ── Parse Firestore document ───────────────────────────────────────────

    static string StripWhitespace(string json)
    {
        var sb = new StringBuilder(json.Length);
        bool inQuote = false;
        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];
            if (ch == '"' && (i == 0 || json[i - 1] != '\\')) inQuote = !inQuote;
            if (inQuote || (ch != ' ' && ch != '\n' && ch != '\r' && ch != '\t'))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    static UserProfileData ParseProfile(string json)
    {
        json = StripWhitespace(json);
        var data = new UserProfileData();
        data.coins = ExtractInt(json, "coins");
        data.unlockedHeart = ExtractInt(json, "unlockedHeart");
        data.unlockedRush = ExtractInt(json, "unlockedRush");
        data.unlockedHeartExtreme = ExtractInt(json, "unlockedHeartExtreme");
        data.unlockedRushExtreme = ExtractInt(json, "unlockedRushExtreme");
        data.bestScoreHeart = ExtractInt(json, "bestScoreHeart");
        data.bestScoreRush = ExtractInt(json, "bestScoreRush");
        data.bestScoreEndless = ExtractInt(json, "bestScoreEndless");
        data.bestScoreHeartExtreme = ExtractInt(json, "bestScoreHeartExtreme");
        data.bestScoreRushExtreme = ExtractInt(json, "bestScoreRushExtreme");
        data.bestTierEndless = ExtractInt(json, "bestTierEndless");
        data.endlessTier10Count = ExtractInt(json, "endlessTier10Count");
        data.selectedCar = ExtractString(json, "selectedCar");
        data.playerName = ExtractString(json, "playerName");
        data.email = ExtractString(json, "email");

        data.unlockedCars = ParseStringArray(json, "unlockedCars");
        data.linkedProviders = ParseStringArray(json, "linkedProviders");

        return data;
    }

    static string ExtractString(string doc, string field)
    {
        string pattern = "\"" + field + "\":{\"stringValue\":\"";
        int idx = doc.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return "";
        idx += pattern.Length;
        int end = doc.IndexOf("\"", idx, StringComparison.Ordinal);
        return end > idx ? doc.Substring(idx, end - idx) : "";
    }

    static int ExtractInt(string doc, string field)
    {
        string pattern = "\"" + field + "\":{\"integerValue\":\"";
        int idx = doc.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return 0;
        idx += pattern.Length;
        int end = doc.IndexOf("\"", idx, StringComparison.Ordinal);
        if (end <= idx) return 0;
        int.TryParse(doc.Substring(idx, end - idx), out int val);
        return val;
    }

    static List<string> ParseStringArray(string json, string fieldName)
    {
        var list = new List<string>();
        string arrPattern = "\"" + fieldName + "\":{\"arrayValue\":{\"values\":[";
        int arrIdx = json.IndexOf(arrPattern, StringComparison.Ordinal);
        if (arrIdx < 0) return list;
        arrIdx += arrPattern.Length;
        int arrEnd = json.IndexOf("]", arrIdx, StringComparison.Ordinal);
        if (arrEnd <= arrIdx) return list;

        string arrContent = json.Substring(arrIdx, arrEnd - arrIdx);
        string valPattern = "\"stringValue\":\"";
        int pos = 0;
        while (true)
        {
            int vi = arrContent.IndexOf(valPattern, pos, StringComparison.Ordinal);
            if (vi < 0) break;
            vi += valPattern.Length;
            int ve = arrContent.IndexOf("\"", vi, StringComparison.Ordinal);
            if (ve > vi) list.Add(arrContent.Substring(vi, ve - vi));
            pos = ve + 1;
        }
        return list;
    }

    // ── Provider-email duplicate check ─────────────────────────────────────
    // Queries users collection to check if another uid has the same email + provider.
    // callback: true = available (can link), false = already taken

    public static void CheckProviderAvailable(string provider, string email, Action<bool> callback)
    {
        if (string.IsNullOrEmpty(email)) { callback?.Invoke(true); return; }
        var go = new GameObject("ProviderCheck");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(CheckProviderCoroutine(provider, email, callback, go));
    }

    static IEnumerator CheckProviderCoroutine(string provider, string email, Action<bool> callback, GameObject runner)
    {
        // Firestore structured query: find users where email == given email
        string url = "https://firestore.googleapis.com/v1/projects/deliverydash-b47d0/databases/(default)/documents:runQuery";
        string query = "{\"structuredQuery\":{"
            + "\"from\":[{\"collectionId\":\"users\"}],"
            + "\"where\":{\"fieldFilter\":{"
            + "\"field\":{\"fieldPath\":\"email\"},"
            + "\"op\":\"EQUAL\","
            + "\"value\":{\"stringValue\":\"" + Esc(email) + "\"}"
            + "}},"
            + "\"limit\":10"
            + "}}";

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(query));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeader(request);

        yield return request.SendWebRequest();

        string currentUid = AuthManager.Instance != null ? AuthManager.Instance.UserId : "";
        bool available = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            string resp = request.downloadHandler.text;
            // Check each returned user doc for matching provider + different uid
            var providerPattern = "\"linkedProviders\"";
            int searchPos = 0;
            while (true)
            {
                // Find next document in the response
                int docIdx = resp.IndexOf("\"document\":", searchPos, StringComparison.Ordinal);
                if (docIdx < 0) break;

                // Extract uid from this document's name (path ends with /users/{uid})
                int nameIdx = resp.IndexOf("\"name\":\"", docIdx, StringComparison.Ordinal);
                if (nameIdx < 0) break;
                nameIdx += 8;
                int nameEnd = resp.IndexOf("\"", nameIdx, StringComparison.Ordinal);
                string docName = nameEnd > nameIdx ? resp.Substring(nameIdx, nameEnd - nameIdx) : "";
                string docUid = "";
                int lastSlash = docName.LastIndexOf('/');
                if (lastSlash >= 0) docUid = docName.Substring(lastSlash + 1);

                // If this is our own doc, skip
                if (docUid == currentUid) { searchPos = nameEnd + 1; continue; }

                // Check if this doc has the same provider in linkedProviders
                int nextDoc = resp.IndexOf("\"document\":", nameEnd, StringComparison.Ordinal);
                string docSection = nextDoc > 0
                    ? resp.Substring(docIdx, nextDoc - docIdx)
                    : resp.Substring(docIdx);

                if (docSection.Contains("\"stringValue\":\"" + provider + "\""))
                {
                    available = false;
                    break;
                }

                searchPos = nameEnd + 1;
            }
        }

        request.Dispose();
        callback?.Invoke(available);
        UnityEngine.Object.Destroy(runner);
    }

    static void AddAuthHeader(UnityWebRequest request)
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated)
            request.SetRequestHeader("Authorization", "Bearer " + AuthManager.Instance.IdToken);
    }

    static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    class CoroutineRunner : MonoBehaviour { }
}
