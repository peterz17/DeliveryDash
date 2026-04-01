using UnityEngine;
public class DeliveryZone : MonoBehaviour
{
    [Header("Destination")]
    public string destinationName;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || GameManager.Instance.TimeRemaining <= 0f) return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || !player.HasPackage) return;
        if (GameManager.Instance.TryDeliver(destinationName))
            player.DropPackage();
    }
}
