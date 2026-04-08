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
    public int unlockedNormal;
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
}

public static class FirestoreUserProfile
{
    const string BASE_URL =
        "https://firestore.googleapis.com/v1/projects/deliverydash-b47d0/databases/(default)/documents";

    static Coroutine _saveCoroutine;
    static MonoBehaviour _saveRunner;
    static float _saveDelay = 1.5f;
    static bool _syncing;

    // ── Public API ─────────────────────────────────────────────────────────

    public static void SyncOnLogin(Action onComplete)
    {
        if (_syncing) { onComplete?.Invoke(); return; }
        _syncing = true;
        var go = new GameObject("FirestoreProfileSync");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(SyncCoroutine(onComplete, go));
    }

    public static void QueueSave()
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated) return;
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
        UnityEngine.Object.Destroy(runner);
    }

    // ── Debounced save ─────────────────────────────────────────────────────

    static IEnumerator DebouncedSave()
    {
        yield return new WaitForSecondsRealtime(_saveDelay);
        _saveCoroutine = null;

        string uid = AuthManager.Instance != null ? AuthManager.Instance.UserId : "";
        if (string.IsNullOrEmpty(uid)) yield break;

        var data = ProfileFromLocal();
        var go = new GameObject("FirestoreProfileUpload");
        go.hideFlags = HideFlags.HideAndDontSave;
        var r = go.AddComponent<CoroutineRunner>();
        r.StartCoroutine(SaveProfileCoroutine(uid, data, go, retried: false, destroyRunner: true));
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
        data.unlockedNormal = PlayerPrefs.GetInt("Unlocked_Normal", 0);
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
        PlayerPrefs.SetInt("Coins", data.coins);
        PlayerPrefs.SetInt("Unlocked_Normal", data.unlockedNormal);
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

    // ── JSON builders (Firestore REST format) ──────────────────────────────

    static string BuildProfileJson(UserProfileData data)
    {
        var sb = new StringBuilder();
        sb.Append("{\"fields\":{");
        sb.Append(IntField("coins", data.coins)); sb.Append(",");
        sb.Append(IntField("unlockedNormal", data.unlockedNormal)); sb.Append(",");
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
        sb.Append("]}}");

        sb.Append("}}");
        return sb.ToString();
    }

    static string IntField(string name, int val) =>
        "\"" + name + "\":{\"integerValue\":\"" + val + "\"}";

    static string StrField(string name, string val) =>
        "\"" + name + "\":{\"stringValue\":\"" + Esc(val) + "\"}";

    // ── Parse Firestore document ───────────────────────────────────────────

    static UserProfileData ParseProfile(string json)
    {
        var data = new UserProfileData();
        data.coins = ExtractInt(json, "coins");
        data.unlockedNormal = ExtractInt(json, "unlockedNormal");
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

        data.unlockedCars = new List<string>();
        string arrPattern = "\"unlockedCars\":{\"arrayValue\":{\"values\":[";
        int arrIdx = json.IndexOf(arrPattern, StringComparison.Ordinal);
        if (arrIdx >= 0)
        {
            arrIdx += arrPattern.Length;
            int arrEnd = json.IndexOf("]", arrIdx, StringComparison.Ordinal);
            if (arrEnd > arrIdx)
            {
                string arrContent = json.Substring(arrIdx, arrEnd - arrIdx);
                string valPattern = "\"stringValue\":\"";
                int pos = 0;
                while (true)
                {
                    int vi = arrContent.IndexOf(valPattern, pos, StringComparison.Ordinal);
                    if (vi < 0) break;
                    vi += valPattern.Length;
                    int ve = arrContent.IndexOf("\"", vi, StringComparison.Ordinal);
                    if (ve > vi)
                        data.unlockedCars.Add(arrContent.Substring(vi, ve - vi));
                    pos = ve + 1;
                }
            }
        }

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
