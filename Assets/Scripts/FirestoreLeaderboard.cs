using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class FirestoreLeaderboard
{
    const string BASE_URL =
        "https://firestore.googleapis.com/v1/projects/deliverydash-b47d0/databases/(default)/documents";

    static List<LeaderboardEntry> _cache;
    static float _cacheTime;
    const float CACHE_DURATION = 60f;
    const int TOP_LIMIT = 10;

    // Prefetch all leaderboard data at game start
    public static void Prefetch()
    {
        _cache = null;
        var go = new GameObject("FirestorePrefetch");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(FetchAllCoroutine("", 0, null, go));
    }

    // Check if score qualifies for top 10 before uploading
    public static void UploadIfQualified(LeaderboardEntry entry)
    {
        if (_cache == null)
        {
            // No cache yet — upload anyway to be safe
            UploadEntry(entry);
            return;
        }

        var modeEntries = FilterByMode(_cache, entry.mode, TOP_LIMIT);

        if (modeEntries.Count < TOP_LIMIT)
        {
            // Less than 10 entries — always qualifies
            UploadEntry(entry);
            return;
        }

        // Check if score beats the lowest in top 10
        int lowestScore = modeEntries[modeEntries.Count - 1].score;
        if (entry.score > lowestScore)
        {
            UploadEntry(entry);
        }
        else
        {
            Debug.Log("[Firestore] Score " + entry.score + " doesn't qualify for top 10 in " + entry.mode + " (min: " + lowestScore + ")");
        }
    }

    public static void UploadEntry(LeaderboardEntry entry)
    {
        _cache = null; // invalidate cache
        var go = new GameObject("FirestoreUpload");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(UploadCoroutine(entry, go));
    }

    static IEnumerator UploadCoroutine(LeaderboardEntry entry, GameObject runner)
    {
        string url = BASE_URL + "/LeaderBoard";

        string json = "{\"fields\":{"
            + "\"name\":{\"stringValue\":\"" + Esc(entry.playerName) + "\"},"
            + "\"coin\":{\"integerValue\":\"" + entry.score + "\"},"
            + "\"mode\":{\"stringValue\":\"" + Esc(entry.mode) + "\"},"
            + "\"level\":{\"integerValue\":\"" + entry.level + "\"},"
            + "\"deliveries\":{\"integerValue\":\"" + entry.deliveries + "\"},"
            + "\"carType\":{\"stringValue\":\"" + Esc(entry.carId) + "\"},"
            + "\"updatedAt\":{\"stringValue\":\"" + DateTime.UtcNow.ToString("o") + "\"}"
            + "}}";

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("[Firestore] Created: " + entry.playerName + " score=" + entry.score + " mode=" + entry.mode);
        else
            Debug.LogWarning("[Firestore] Upload failed: " + request.error + " " + request.downloadHandler.text);

        request.Dispose();
        UnityEngine.Object.Destroy(runner);
    }

    public static void FetchLeaderboard(string mode, int limit, Action<List<LeaderboardEntry>> callback)
    {
        if (_cache != null && Time.realtimeSinceStartup - _cacheTime < CACHE_DURATION)
        {
            callback?.Invoke(FilterByMode(_cache, mode, limit));
            return;
        }

        var go = new GameObject("FirestoreFetch");
        go.hideFlags = HideFlags.HideAndDontSave;
        var runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(FetchAllCoroutine(mode, limit, callback, go));
    }

    static IEnumerator FetchAllCoroutine(string mode, int limit, Action<List<LeaderboardEntry>> callback, GameObject runner)
    {
        string url = BASE_URL + "/LeaderBoard?pageSize=300";

        var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            _cache = ParseListResponse(response);
            _cacheTime = Time.realtimeSinceStartup;
            Debug.Log("[Firestore] Fetched " + _cache.Count + " total entries");
            if (callback != null && !string.IsNullOrEmpty(mode))
                callback.Invoke(FilterByMode(_cache, mode, limit));
        }
        else
        {
            Debug.LogWarning("[Firestore] Fetch failed: " + request.error);
            callback?.Invoke(new List<LeaderboardEntry>());
        }

        request.Dispose();
        UnityEngine.Object.Destroy(runner);
    }

    static List<LeaderboardEntry> FilterByMode(List<LeaderboardEntry> all, string mode, int limit)
    {
        var filtered = new List<LeaderboardEntry>();
        foreach (var e in all)
        {
            if (e.mode == mode) filtered.Add(e);
        }
        filtered.Sort();
        if (limit > 0 && filtered.Count > limit) filtered.RemoveRange(limit, filtered.Count - limit);
        return filtered;
    }

    static List<LeaderboardEntry> ParseListResponse(string json)
    {
        var entries = new List<LeaderboardEntry>();

        // Remove all whitespace outside quotes for reliable parsing
        var compact = new StringBuilder(json.Length);
        bool inQuote = false;
        for (int c = 0; c < json.Length; c++)
        {
            char ch = json[c];
            if (ch == '"' && (c == 0 || json[c - 1] != '\\')) inQuote = !inQuote;
            if (inQuote || (ch != ' ' && ch != '\n' && ch != '\r' && ch != '\t'))
                compact.Append(ch);
        }
        string j = compact.ToString();

        var parts = j.Split(new[] { "\"fields\":{" }, StringSplitOptions.None);

        for (int i = 1; i < parts.Length; i++)
        {
            var doc = parts[i];
            var entry = new LeaderboardEntry();
            entry.playerName = ExtractString(doc, "name");
            entry.score = ExtractInt(doc, "coin");
            entry.mode = ExtractString(doc, "mode");
            entry.level = ExtractInt(doc, "level");
            entry.deliveries = ExtractInt(doc, "deliveries");
            entry.carId = ExtractString(doc, "carType");
            string ts = ExtractString(doc, "updatedAt");
            entry.date = !string.IsNullOrEmpty(ts) && ts.Length >= 10 ? ts.Substring(0, 10) : "";

            if (!string.IsNullOrEmpty(entry.playerName) || entry.score > 0)
                entries.Add(entry);
        }

        return entries;
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

    static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    class CoroutineRunner : MonoBehaviour { }
}
