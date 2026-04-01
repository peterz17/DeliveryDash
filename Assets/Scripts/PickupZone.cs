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
            GameManager.Instance.uiManager.ShowFeedback(LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("feedback_pickup") : "Package picked up!", true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
        }
        else
        {
            GameManager.Instance.uiManager.ShowFeedback(LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("feedback_carrying") : "Already carrying a package!", false);
        }
    }
}
