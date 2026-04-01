using UnityEngine;

[CreateAssetMenu(fileName = "NewCar", menuName = "Delivery Dash/Car Data")]
public class CarData : ScriptableObject
{
    public string carId;
    public string localizationKey;
    public Sprite carSprite;
    public float speedMultiplier = 1f;
    public float durabilitySeconds = 2f;
    public Vector2 colliderSize = new Vector2(0.4f, 0.8f);
    public float spriteScale = 1f;
    public int bonusHearts;
    public int displaySpeed;
    public int displaySize;
    public int unlockCost;

    [Header("Unlock Requirements")]
    public int requiredLevel;
    public int requiredEndlessTier10;
}
