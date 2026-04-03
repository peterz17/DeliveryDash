using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    const int MaxEntries = 10;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddEntry(LeaderboardEntry entry)
    {
        var data = Load(entry.mode);
        var list = new List<LeaderboardEntry>(data.entries) { entry };
        list.Sort();
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        data.entries = list.ToArray();
        Save(entry.mode, data);
    }

    public List<LeaderboardEntry> GetEntries(GameMode mode) => GetEntriesStatic(mode);

    public static List<LeaderboardEntry> GetEntriesStatic(GameMode mode)
    {
        string key = "Leaderboard_" + mode.ToString();
        string json = PlayerPrefs.GetString(key, "");
        LeaderboardData data = string.IsNullOrEmpty(json) ? new LeaderboardData() : JsonUtility.FromJson<LeaderboardData>(json);
        var list = new List<LeaderboardEntry>(data.entries);
        list.Sort();
        return list;
    }

    public int GetRank(GameMode mode, int score)
    {
        var entries = GetEntries(mode);
        for (int i = 0; i < entries.Count; i++)
        {
            if (score >= entries[i].score) return i + 1;
        }
        return entries.Count < MaxEntries ? entries.Count + 1 : -1;
    }

    public void ClearEntries(GameMode mode)
    {
        PlayerPrefs.DeleteKey("Leaderboard_" + mode.ToString());
        PlayerPrefs.Save();
    }

    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "Player");
    }

    public static void SetPlayerName(string name)
    {
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
    }

    LeaderboardData Load(string modeStr)
    {
        string key = "Leaderboard_" + modeStr;
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new LeaderboardData();
        return JsonUtility.FromJson<LeaderboardData>(json);
    }

    void Save(string modeStr, LeaderboardData data)
    {
        string key = "Leaderboard_" + modeStr;
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    // Static convenience methods that work even if Instance is null
    public static void RecordEntry(string playerName, int score, string mode, int level, int deliveries, string carId)
    {
        var entry = new LeaderboardEntry
        {
            playerName = playerName,
            score = score,
            mode = mode,
            level = level,
            deliveries = deliveries,
            carId = carId,
            date = System.DateTime.Now.ToString("yyyy-MM-dd")
        };

        // Save directly via PlayerPrefs - no Instance needed
        string key = "Leaderboard_" + mode;
        string json = PlayerPrefs.GetString(key, "");
        LeaderboardData data = string.IsNullOrEmpty(json) ? new LeaderboardData() : JsonUtility.FromJson<LeaderboardData>(json);
        var list = new List<LeaderboardEntry>(data.entries) { entry };
        list.Sort();
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        data.entries = list.ToArray();
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
