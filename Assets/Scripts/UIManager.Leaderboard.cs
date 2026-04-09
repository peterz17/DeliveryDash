using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    GameMode leaderboardCurrentMode = GameMode.Heart;

    public void ShowLeaderboardScreen(GameMode mode = GameMode.Heart)
    {
        SetAllScreens(leaderboard: true);
        leaderboardCurrentMode = mode;
        PopulateLeaderboard(mode);
        UpdateLeaderboardTabHighlight();
    }

    void PopulateLeaderboard(GameMode mode)
    {
        if (leaderboardContent == null) return;
        ClearLeaderboardContent();
        ShowLoadingText();

        FirestoreLeaderboard.FetchLeaderboard(mode.ToString(), 10, entries =>
        {
            ClearLeaderboardContent();
            if (entries == null || entries.Count == 0)
            {
                ShowEmptyText();
                return;
            }
            for (int i = 0; i < entries.Count; i++)
                CreateLeaderboardRow(i + 1, entries[i], mode);
        });
    }

    void ClearLeaderboardContent()
    {
        if (leaderboardContent == null) return;
        for (int i = leaderboardContent.childCount - 1; i >= 0; i--)
            Destroy(leaderboardContent.GetChild(i).gameObject);
    }

    void ShowLoadingText()
    {
        var go = new GameObject("Loading", typeof(RectTransform));
        go.transform.SetParent(leaderboardContent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Loading...";
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.3f, 0.15f, 0.15f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 50);
    }

    void ShowEmptyText()
    {
        var go = new GameObject("Empty", typeof(RectTransform));
        go.transform.SetParent(leaderboardContent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = LocalizationManager.L("lb_empty", "No records yet");
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.5f, 0.35f, 0.35f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 50);
    }

    void CreateLeaderboardRow(int rank, LeaderboardEntry entry, GameMode mode)
    {
        var rowGO = new GameObject("Row_" + rank, typeof(RectTransform));
        rowGO.transform.SetParent(leaderboardContent, false);
        var rt = (RectTransform)rowGO.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 55);

        var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(20, 20, 0, 0);

        Color textColor = new Color(0.3f, 0.15f, 0.15f);
        Color rankColor = rank <= 3 ? new Color(0.6f, 0.25f, 0.05f) : textColor;
        bool isEndless = mode == GameMode.Endless;
        string levelStr = isEndless
            ? LocalizationManager.LFmt("lb_tier_fmt", "Tier {0}", entry.level)
            : LocalizationManager.LFmt("lb_level_fmt", "Lv.{0}", entry.level);

        AddRowCell(rowGO.transform, LocalizationManager.LFmt("lb_rank", "#{0}", rank), 60, 0, rankColor, TextAlignmentOptions.Center, rank <= 3 ? 26 : 22);
        AddRowCell(rowGO.transform, entry.playerName, 0, 1, textColor, TextAlignmentOptions.Left, 22);
        AddRowCell(rowGO.transform, LocalizationManager.LFmt("lb_score_fmt", "{0} coins", entry.score), 180, 0, rankColor, TextAlignmentOptions.Right, 22);
        AddRowCell(rowGO.transform, levelStr, 80, 0, textColor, TextAlignmentOptions.Center, 20);
        AddRowCell(rowGO.transform, LocalizationManager.LFmt("lb_deliveries_fmt", "{0} deliveries", entry.deliveries), 140, 0, textColor, TextAlignmentOptions.Center, 18);
        AddRowCell(rowGO.transform, entry.date, 140, 0, new Color(0.35f, 0.2f, 0.2f), TextAlignmentOptions.Right, 18);
    }

    void AddRowCell(Transform parent, string text, float prefWidth, float flexWidth, Color color, TextAlignmentOptions align, float fontSize)
    {
        var go = new GameObject("Cell", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        var le = go.AddComponent<LayoutElement>();
        if (prefWidth > 0) le.preferredWidth = prefWidth;
        le.flexibleWidth = flexWidth;
    }

    void UpdateLeaderboardTabHighlight()
    {
        if (leaderboardModeTabs == null) return;
        GameMode[] modes = { GameMode.Heart, GameMode.Rush, GameMode.Endless, GameMode.HeartExtreme, GameMode.RushExtreme };
        for (int i = 0; i < leaderboardModeTabs.Length && i < modes.Length; i++)
        {
            var txt = leaderboardModeTabs[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = modes[i] == leaderboardCurrentMode ? new Color(1f, 0.85f, 0.1f) : Color.white;
        }
    }
}
