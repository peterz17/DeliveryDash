using UnityEngine;

public class PickupZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || GameManager.Instance.TimeRemaining <= 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (!player.HasPackage)
        {
            player.PickupPackage();
            GameManager.Instance.uiManager.ShowFeedback(LocalizationManager.L("feedback_pickup", "Package picked up!"), true);
            AudioManager.Play(a => a.PlayPickup());
        }
        else
        {
            GameManager.Instance.uiManager.ShowFeedback(LocalizationManager.L("feedback_carrying", "Already carrying a package!"), false);
        }
    }
}
