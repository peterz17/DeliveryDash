using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    public void ShowTutorialHints()
    {
        ShowFeedback(LocalizationManager.L("tutorial_pickup", "Pick up packages here!"), true);
        StartCoroutine(DelayedTutorialHint(LocalizationManager.L("tutorial_deliver", "Deliver to the glowing zone!")));
    }

    IEnumerator DelayedTutorialHint(string message)
    {
        yield return new WaitForSecondsRealtime(2f);
        ShowFeedback(message, true);
    }

    public void StartInteractiveTutorial()
    {
        StartCoroutine(InteractiveTutorialRoutine());
    }

    IEnumerator InteractiveTutorialRoutine()
    {
        var gm = GameManager.Instance;
        if (gm == null) yield break;

        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) yield break;

        var overlayGO = new GameObject("TutorialOverlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvas.transform, false);
        AnchorRect(overlayGO, new Vector2(0f, 0f), new Vector2(1f, 0.12f));
        var bg = overlayGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var textGO = new GameObject("InstrText", typeof(RectTransform));
        textGO.transform.SetParent(overlayGO.transform, false);
        AnchorRect(textGO, new Vector2(0.02f, 0f), new Vector2(0.78f, 1f));
        var instrText = textGO.AddComponent<TextMeshProUGUI>();
        instrText.fontSize = 28;
        instrText.alignment = TextAlignmentOptions.MidlineLeft;
        instrText.color = new Color(1f, 1f, 0.6f);
        instrText.margin = new Vector4(12f, 0f, 4f, 0f);
        instrText.raycastTarget = false;

        var skipGO = new GameObject("SkipBtn", typeof(RectTransform));
        skipGO.transform.SetParent(overlayGO.transform, false);
        AnchorRect(skipGO, new Vector2(0.80f, 0.15f), new Vector2(0.98f, 0.85f));
        var skipImg = skipGO.AddComponent<Image>();
        skipImg.color = new Color(0.35f, 0.35f, 0.40f, 1f);
        var skipBtn = skipGO.AddComponent<Button>();
        skipBtn.targetGraphic = skipImg;
        var skipTxtGO = new GameObject("Text", typeof(RectTransform));
        skipTxtGO.transform.SetParent(skipGO.transform, false);
        AnchorFull(skipTxtGO);
        var skipTmp = skipTxtGO.AddComponent<TextMeshProUGUI>();
        skipTmp.text = LocalizationManager.L("btn_skip", "SKIP");
        skipTmp.fontSize = 22;
        skipTmp.fontStyle = FontStyles.Bold;
        skipTmp.alignment = TextAlignmentOptions.Center;
        skipTmp.color = Color.white;
        skipTmp.raycastTarget = false;

        bool skipped = false;
        skipBtn.onClick.AddListener(() => { skipped = true; });

        instrText.text = LocalizationManager.L("tutorial_pickup", "Pick up packages here!");
        overlayGO.SetActive(true);
        yield return new WaitUntil(() => skipped || gm.player == null || gm.player.HasPackage);

        if (!skipped)
        {
            instrText.text = LocalizationManager.L("tutorial_deliver", "Deliver to the glowing zone!");
            yield return new WaitUntil(() => skipped || gm.player == null || !gm.player.HasPackage);

            if (!skipped)
            {
                instrText.text = LocalizationManager.L("tutorial_done", "Great job! You're ready!");
                yield return new WaitForSecondsRealtime(1.8f);
            }
        }

        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.Save();
        overlayGO.SetActive(false);
        Destroy(overlayGO);
    }
}
