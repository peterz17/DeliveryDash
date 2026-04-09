using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LeaderboardSimulation
{
    struct SimPlayer
    {
        public string name;
        public float deliveryInterval;
        public float correctRate;
        public float crashRate;
        public GameMode mode;
        public int startLevel;
    }

    struct SimResult
    {
        public string name;
        public GameMode mode;
        public int score;
        public int level;
        public int deliveries;
        public int wrongDeliveries;
        public int crashes;
        public bool qualifiedForBoard;
        public int rank;
    }

#if UNITY_EDITOR
    [MenuItem("Delivery Dash/Simulate 20 Players")]
    static void RunMenu20() => Run(20);
    [MenuItem("Delivery Dash/Simulate 100 Players")]
    static void RunMenu100() => Run(100);
#endif
    public static void Run(int count = 20)
    {
        var rng = new System.Random(42);
        var players = count <= 20 ? Generate20Players(rng) : GeneratePlayers(count, rng);
        var results = new List<SimResult>();

        foreach (var p in players)
            results.Add(SimulatePlayer(p, rng));

        var boards = new Dictionary<GameMode, List<SimResult>>();
        foreach (GameMode m in Enum.GetValues(typeof(GameMode)))
            boards[m] = new List<SimResult>();

        foreach (var r in results)
            boards[r.mode].Add(r);

        foreach (var kv in boards)
            kv.Value.Sort((a, b) => b.score.CompareTo(a.score));

        int totalPlayers = results.Count;
        int qualified = 0;
        int notQualified = 0;

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var board = boards[r.mode];
            int idx = board.FindIndex(x => x.name == r.name);
            if (idx < 10)
            {
                r.qualifiedForBoard = true;
                r.rank = idx + 1;
                qualified++;
            }
            else
            {
                r.qualifiedForBoard = false;
                r.rank = 0;
                notQualified++;
            }
            results[i] = r;
        }

        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log($"  LEADERBOARD SIMULATION — {totalPlayers} Players");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        foreach (GameMode m in Enum.GetValues(typeof(GameMode)))
        {
            var board = boards[m];
            if (board.Count == 0) continue;

            Debug.Log($"\n  ── {m} Mode ({board.Count} players) ──");
            Debug.Log($"  {"#",-4}{"Name",-16}{"Score",8}{"Lvl",5}{"Deliv",7}{"Wrong",7}{"Crash",7}{"Board?",8}");
            Debug.Log($"  {"─",-4}{"─",-16}{"─",8}{"─",5}{"─",7}{"─",7}{"─",7}{"─",8}");

            for (int i = 0; i < board.Count; i++)
            {
                var r = board[i];
                bool inTop10 = i < 10;
                string boardStr = inTop10 ? $"#{i + 1}" : "---";
                Debug.Log($"  {(i+1),-4}{r.name,-16}{r.score,8}{r.level,5}{r.deliveries,7}{r.wrongDeliveries,7}{r.crashes,7}{boardStr,8}");
            }
        }

        float qualPct = (float)qualified / totalPlayers * 100f;
        float notQualPct = (float)notQualified / totalPlayers * 100f;

        Debug.Log("\n═══════════════════════════════════════════════════════════════");
        Debug.Log($"  SUMMARY");
        Debug.Log($"  Total players:              {totalPlayers}");
        Debug.Log($"  Qualified for leaderboard:  {qualified} ({qualPct:F1}%)");
        Debug.Log($"  Did NOT qualify:            {notQualified} ({notQualPct:F1}%)");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        Debug.Log("\n  Per-mode breakdown:");
        foreach (GameMode m in Enum.GetValues(typeof(GameMode)))
        {
            var board = boards[m];
            if (board.Count == 0) continue;
            int modeQual = Math.Min(board.Count, 10);
            int modeNot = Math.Max(0, board.Count - 10);
            Debug.Log($"    {m,-15} — {board.Count} players, {modeQual} qualified ({(float)modeQual/board.Count*100f:F0}%), {modeNot} not ({(float)modeNot/board.Count*100f:F0}%)");
        }

        Debug.Log("\n  Score ranges across all players:");
        results.Sort((a, b) => b.score.CompareTo(a.score));
        int min = results[results.Count - 1].score;
        int max = results[0].score;
        int median = results[results.Count / 2].score;
        Debug.Log($"    Max: {max}  Median: {median}  Min: {min}");
    }

    static List<SimPlayer> GeneratePlayers(int count, System.Random rng)
    {
        var players = new List<SimPlayer>();
        var modes = (GameMode[])Enum.GetValues(typeof(GameMode));

        // Distribution: Heart 35%, Rush 25%, Endless 25%, HeartExtreme 10%, RushExtreme 5%
        float[] modeWeights = { 0.35f, 0.25f, 0.25f, 0.10f, 0.05f };

        // Skill tiers: pro 10%, good 20%, mid 35%, low 20%, beginner 15%
        float[] skillWeights = { 0.10f, 0.20f, 0.35f, 0.20f, 0.15f };
        // interval, correctRate, crashRate ranges per tier
        float[][] tierParams = {
            new[] { 2.0f, 3.0f, 0.90f, 0.97f, 0.03f, 0.08f }, // pro
            new[] { 3.0f, 3.8f, 0.82f, 0.92f, 0.08f, 0.14f }, // good
            new[] { 3.5f, 5.0f, 0.70f, 0.83f, 0.12f, 0.22f }, // mid
            new[] { 4.5f, 6.0f, 0.55f, 0.72f, 0.18f, 0.28f }, // low
            new[] { 5.5f, 7.5f, 0.40f, 0.58f, 0.25f, 0.38f }, // beginner
        };
        // Start level ranges per tier (for non-Endless)
        int[][] tierLevels = {
            new[] { 8, 20 },
            new[] { 4, 12 },
            new[] { 1, 7  },
            new[] { 0, 3  },
            new[] { 0, 1  },
        };

        for (int i = 0; i < count; i++)
        {
            // Pick mode
            float mRoll = (float)rng.NextDouble();
            int mIdx = 0;
            float mAcc = 0;
            for (int m = 0; m < modeWeights.Length; m++)
            {
                mAcc += modeWeights[m];
                if (mRoll < mAcc) { mIdx = m; break; }
            }
            GameMode mode = modes[mIdx];

            // Pick skill tier
            float sRoll = (float)rng.NextDouble();
            int tier = 0;
            float sAcc = 0;
            for (int s = 0; s < skillWeights.Length; s++)
            {
                sAcc += skillWeights[s];
                if (sRoll < sAcc) { tier = s; break; }
            }

            float[] tp = tierParams[tier];
            float interval = Lerp(tp[0], tp[1], (float)rng.NextDouble());
            float correct = Lerp(tp[2], tp[3], (float)rng.NextDouble());
            float crash = Lerp(tp[4], tp[5], (float)rng.NextDouble());

            int startLevel = 0;
            if (mode != GameMode.Endless)
            {
                int[] lv = tierLevels[tier];
                startLevel = rng.Next(lv[0], lv[1] + 1);
            }

            string name = $"Player_{i + 1:D3}";
            players.Add(MakePlayer(name, interval, correct, crash, mode, startLevel, rng));
        }

        return players;
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static List<SimPlayer> Generate20Players(System.Random rng)
    {
        var players = new List<SimPlayer>();
        string[] names =
        {
            "ProGamer", "SpeedKing", "DeliveryAce", "RushMaster", "CoinHunter",
            "NoviceNon", "SlowPoke", "CrashKid", "WrongTurn", "LostDriver",
            "MidTier01", "MidTier02", "MidTier03", "Average04", "Decent05",
            "TryHard06", "Lucky07", "Veteran08", "Rookie09", "GhostRacer"
        };

        // Pro players
        players.Add(MakePlayer(names[0],  2.5f, 0.95f, 0.05f, GameMode.Heart, 14, rng));
        players.Add(MakePlayer(names[1],  2.2f, 0.92f, 0.08f, GameMode.Rush, 10, rng));
        players.Add(MakePlayer(names[2],  2.8f, 0.93f, 0.04f, GameMode.Endless, 0, rng));

        // Good players
        players.Add(MakePlayer(names[3],  3.2f, 0.88f, 0.10f, GameMode.Rush, 7, rng));
        players.Add(MakePlayer(names[4],  3.0f, 0.90f, 0.08f, GameMode.Heart, 10, rng));
        players.Add(MakePlayer(names[5],  3.5f, 0.85f, 0.12f, GameMode.Endless, 0, rng));
        players.Add(MakePlayer(names[6],  3.3f, 0.87f, 0.09f, GameMode.HeartExtreme, 5, rng));

        // Mid-tier players
        players.Add(MakePlayer(names[7],  4.0f, 0.78f, 0.15f, GameMode.Heart, 5, rng));
        players.Add(MakePlayer(names[8],  4.2f, 0.75f, 0.18f, GameMode.Rush, 3, rng));
        players.Add(MakePlayer(names[9],  4.5f, 0.72f, 0.20f, GameMode.Endless, 0, rng));
        players.Add(MakePlayer(names[10], 3.8f, 0.80f, 0.14f, GameMode.Heart, 8, rng));
        players.Add(MakePlayer(names[11], 4.0f, 0.77f, 0.16f, GameMode.RushExtreme, 2, rng));

        // Low-tier players
        players.Add(MakePlayer(names[12], 5.0f, 0.65f, 0.22f, GameMode.Heart, 2, rng));
        players.Add(MakePlayer(names[13], 5.5f, 0.60f, 0.25f, GameMode.Rush, 1, rng));
        players.Add(MakePlayer(names[14], 5.2f, 0.62f, 0.20f, GameMode.Endless, 0, rng));
        players.Add(MakePlayer(names[15], 4.8f, 0.68f, 0.19f, GameMode.Heart, 3, rng));

        // Beginner players
        players.Add(MakePlayer(names[16], 6.0f, 0.50f, 0.30f, GameMode.Heart, 0, rng));
        players.Add(MakePlayer(names[17], 6.5f, 0.45f, 0.35f, GameMode.Rush, 0, rng));
        players.Add(MakePlayer(names[18], 7.0f, 0.48f, 0.28f, GameMode.Endless, 0, rng));
        players.Add(MakePlayer(names[19], 5.8f, 0.55f, 0.25f, GameMode.HeartExtreme, 0, rng));

        return players;
    }

    static SimPlayer MakePlayer(string name, float interval, float correct, float crash, GameMode mode, int level, System.Random rng)
    {
        float intervalJitter = (float)(rng.NextDouble() * 0.6 - 0.3);
        float correctJitter = (float)(rng.NextDouble() * 0.06 - 0.03);

        return new SimPlayer
        {
            name = name,
            deliveryInterval = Mathf.Max(1.5f, interval + intervalJitter),
            correctRate = Mathf.Clamp01(correct + correctJitter),
            crashRate = Mathf.Clamp01(crash),
            mode = mode,
            startLevel = Mathf.Clamp(level, 0, LevelData.TotalLevels - 1),
        };
    }

    static SimResult SimulatePlayer(SimPlayer p, System.Random rng)
    {
        return p.mode == GameMode.Endless ? SimulateEndless(p, rng) : SimulateNormalOrRush(p, rng);
    }

    static SimResult SimulateNormalOrRush(SimPlayer p, System.Random rng)
    {
        bool isHeart = p.mode == GameMode.Heart || p.mode == GameMode.HeartExtreme;
        bool isExtreme = p.mode == GameMode.HeartExtreme || p.mode == GameMode.RushExtreme;
        int level = p.startLevel;
        var levelData = LevelData.Levels[level];

        float time = levelData.time;
        int lives = isHeart ? levelData.lives : 0;
        int score = 0;
        int streak = 0;
        int deliveries = 0;
        int wrongDeliveries = 0;
        int crashes = 0;
        int deliveryCountForRush = 0;

        while (time > 0f && (!isHeart || lives > 0))
        {
            if (p.deliveryInterval > time) break;
            time -= p.deliveryInterval;

            // Crash check
            float crashMod = isExtreme ? 1.4f : 1f;
            crashMod *= 1f + level * 0.02f;
            if (rng.NextDouble() < p.crashRate * crashMod)
            {
                crashes++;
                if (isHeart)
                {
                    lives--;
                    if (lives <= 0) break;
                }
                else
                {
                    time -= 4f;
                    if (time <= 0f) break;
                }
            }

            // Delivery attempt
            bool isRushOrder = deliveryCountForRush % 4 == 0;
            if (rng.NextDouble() < p.correctRate)
            {
                int baseCoins = isRushOrder ? 20 : 10;
                streak++;
                int streakBonus = streak >= 5 ? 5 : streak >= 3 ? 2 : 0;
                int totalCoins = baseCoins + streakBonus;
                score += totalCoins;
                time += 4f;
                deliveries++;
                deliveryCountForRush++;

                if (score >= levelData.scoreNeeded)
                {
                    if (level < LevelData.TotalLevels - 1)
                    {
                        level++;
                        levelData = LevelData.Levels[level];
                        score = 0;
                        streak = 0;
                        deliveries = 0;
                        deliveryCountForRush = 0;
                        time = levelData.time;
                        if (isHeart) lives = levelData.lives;
                    }
                    else
                        break;
                }
            }
            else
            {
                wrongDeliveries++;
                streak = 0;
                if (!isHeart) time -= 2f;
            }
        }

        return new SimResult
        {
            name = p.name,
            mode = p.mode,
            score = score,
            level = level + 1,
            deliveries = deliveries,
            wrongDeliveries = wrongDeliveries,
            crashes = crashes,
        };
    }

    static SimResult SimulateEndless(SimPlayer p, System.Random rng)
    {
        float time = LevelData.Levels[0].time;
        int score = 0;
        int streak = 0;
        int deliveries = 0;
        int wrongDeliveries = 0;
        int crashes = 0;
        int deliveryCountForRush = 0;

        int tier = 0;
        int tierProgress = 0;
        float deliveryBonus = 4f;

        while (time > 0f)
        {
            float interval = p.deliveryInterval * (1f + tier * 0.03f);

            if (interval > time) break;
            time -= interval;

            float crashMod = 1f + tier * 0.04f;
            if (rng.NextDouble() < p.crashRate * crashMod)
            {
                crashes++;
                time -= 4f;
                if (time <= 0f) break;
                continue;
            }

            bool isRushOrder = deliveryCountForRush % 4 == 0;
            float effectiveCorrect = Mathf.Max(0.3f, p.correctRate * (1f - tier * 0.008f));

            if (rng.NextDouble() < effectiveCorrect)
            {
                int baseCoins = isRushOrder ? 20 : 10;
                streak++;
                int streakBonus = streak >= 5 ? 5 : streak >= 3 ? 2 : 0;
                int totalCoins = baseCoins + streakBonus;
                score += totalCoins;
                time += deliveryBonus;
                deliveries++;
                deliveryCountForRush++;

                tierProgress += totalCoins;
                int tierTarget = 40 + tier * 25 + (tier / 3) * 20;
                if (tierProgress >= tierTarget)
                {
                    tierProgress -= tierTarget;
                    tier++;
                    float tierTimeBonus = tier <= 10 ? 20f : tier <= 20 ? 15f : 10f;
                    time += tierTimeBonus;
                    deliveryBonus = Mathf.Max(1.5f, 4f - Mathf.Max(0, tier - 5) * 0.18f);
                }
            }
            else
            {
                wrongDeliveries++;
                streak = 0;
                if (tier >= 2)
                {
                    float penalty = Mathf.Min(2f + (tier - 2) * 0.2f, 5f);
                    time -= penalty;
                }
            }
        }

        return new SimResult
        {
            name = p.name,
            mode = GameMode.Endless,
            score = score,
            level = tier + 1,
            deliveries = deliveries,
            wrongDeliveries = wrongDeliveries,
            crashes = crashes,
        };
    }
}
