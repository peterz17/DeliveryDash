public static class LevelData
{
    public struct Level
    {
        public float time;
        public int scoreNeeded;
        public int lives;

        public Level(float time, int scoreNeeded, int lives)
        {
            this.time = time;
            this.scoreNeeded = scoreNeeded;
            this.lives = lives;
        }
    }

    public const int TotalLevels = 30;

    public static readonly Level[] Levels =
    {
        new(30f,   50, 3),   // 1
        new(30f,   70, 3),   // 2
        new(35f,   90, 3),   // 3
        new(35f,  110, 3),   // 4
        new(40f,  130, 4),   // 5
        new(40f,  155, 4),   // 6
        new(45f,  180, 4),   // 7
        new(45f,  210, 4),   // 8
        new(50f,  240, 5),   // 9
        new(50f,  275, 5),   // 10
        new(55f,  290, 6),   // 11
        new(57f,  330, 6),   // 12
        new(59f,  370, 6),   // 13
        new(61f,  415, 6),   // 14
        new(63f,  465, 7),   // 15
        new(65f,  520, 7),   // 16
        new(67f,  580, 7),   // 17
        new(69f,  645, 7),   // 18
        new(71f,  715, 8),   // 19
        new(73f,  805, 8),   // 20
        new(75f,  900, 8),   // 21
        new(77f, 1000, 8),   // 22
        new(79f, 1105, 9),   // 23
        new(81f, 1215, 9),   // 24
        new(83f, 1295, 9),   // 25
        new(85f, 1400, 9),   // 26
        new(87f, 1510, 10),  // 27
        new(89f, 1625, 10),  // 28
        new(91f, 1745, 10),  // 29
        new(93f, 1870, 11),  // 30
    };

    public static float GetTime(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= Levels.Length) return 0f;
        return Levels[levelIndex].time;
    }

    public static int GetScoreTarget(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= Levels.Length) return 0;
        return Levels[levelIndex].scoreNeeded;
    }

    // ── Persistence ────────────────────────────────────────────────────────

    static string UnlockKey(GameMode mode) => mode switch
    {
        GameMode.Rush         => "Unlocked_Rush",
        GameMode.HeartExtreme => "Unlocked_HeartExtreme",
        GameMode.RushExtreme  => "Unlocked_RushExtreme",
        _                     => "Unlocked_Normal"
    };

    public static string[] CloudFields(GameMode mode) => mode switch
    {
        GameMode.Rush         => new[] { "coins", "unlockedRush", "bestScoreRush" },
        GameMode.HeartExtreme => new[] { "coins", "unlockedHeartExtreme", "bestScoreHeartExtreme" },
        GameMode.RushExtreme  => new[] { "coins", "unlockedRushExtreme", "bestScoreRushExtreme" },
        GameMode.Endless      => new[] { "coins", "bestScoreEndless", "bestTierEndless", "endlessTier10Count" },
        _                     => new[] { "coins", "unlockedHeart", "bestScoreHeart" }
    };

    public static int GetUnlockedLevel(GameMode mode)
        => UnityEngine.PlayerPrefs.GetInt(UnlockKey(mode), 0);

    public static void SaveUnlockedLevel(GameMode mode, int levelIndex)
    {
        string key = UnlockKey(mode);
        if (levelIndex > UnityEngine.PlayerPrefs.GetInt(key, 0))
        {
            UnityEngine.PlayerPrefs.SetInt(key, levelIndex);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    public static int GetBestScore(GameMode mode)
        => UnityEngine.PlayerPrefs.GetInt("BestScore_" + mode.ToString(), 0);

    public static void SaveBestScore(GameMode mode, int score)
    {
        string key = "BestScore_" + mode.ToString();
        if (score > UnityEngine.PlayerPrefs.GetInt(key, 0))
        {
            UnityEngine.PlayerPrefs.SetInt(key, score);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    public static int GetBestEndlessTier()
        => UnityEngine.PlayerPrefs.GetInt("BestTier_Endless", 0);

    public static void SaveBestEndlessTier(int tier)
    {
        if (tier > UnityEngine.PlayerPrefs.GetInt("BestTier_Endless", 0))
        {
            UnityEngine.PlayerPrefs.SetInt("BestTier_Endless", tier);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    public static int GetEndlessTier10Count()
        => UnityEngine.PlayerPrefs.GetInt("EndlessTier10Count", 0);

    public static void IncrementEndlessTier10()
    {
        UnityEngine.PlayerPrefs.SetInt("EndlessTier10Count", GetEndlessTier10Count() + 1);
        UnityEngine.PlayerPrefs.Save();
    }
}
