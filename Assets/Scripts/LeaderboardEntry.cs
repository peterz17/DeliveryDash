using System;

[Serializable]
public class LeaderboardEntry : IComparable<LeaderboardEntry>
{
    public string playerName;
    public int score;
    public string mode;
    public int level;
    public int deliveries;
    public string carId;
    public string date;

    public int CompareTo(LeaderboardEntry other)
    {
        return other.score.CompareTo(score);
    }
}

[Serializable]
public class LeaderboardData
{
    public LeaderboardEntry[] entries = Array.Empty<LeaderboardEntry>();
}
