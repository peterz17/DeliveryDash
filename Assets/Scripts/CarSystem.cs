using UnityEngine;

public class CarSystem
{
    readonly CarData[] catalog;

    public CarSystem(CarData[] catalog)
    {
        this.catalog = catalog;
    }

    // ── Coins ──────────────────────────────────────────────────────────────

    public int Coins => PlayerPrefs.GetInt("Coins", 0);

    public void AddCoins(int amount)
    {
        PlayerPrefs.SetInt("Coins", Coins + amount);
        PlayerPrefs.Save();
    }

    // ── Unlock checks ──────────────────────────────────────────────────────

    public bool IsCarUnlocked(CarData car)
    {
        if (car == null) return false;
        if (car.unlockCost == 0 && car.requiredLevel == 0 && car.requiredEndlessTier10 == 0) return true;
        return PlayerPrefs.GetInt("CarUnlocked_" + car.carId, 0) == 1;
    }

    public bool CanUnlockCar(CarData car)
    {
        if (car == null) return false;
        if (Coins < car.unlockCost) return false;
        if (car.requiredLevel > 0)
        {
            if (LevelData.GetUnlockedLevel(GameMode.Heart) < car.requiredLevel) return false;
            if (LevelData.GetUnlockedLevel(GameMode.Rush) < car.requiredLevel) return false;
        }
        if (car.requiredEndlessTier10 > 0)
        {
            if (LevelData.GetEndlessTier10Count() < car.requiredEndlessTier10) return false;
        }
        return true;
    }

    public string GetUnlockRequirementText(CarData car)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (car.requiredLevel > 0)
        {
            bool heartOk = LevelData.GetUnlockedLevel(GameMode.Heart) >= car.requiredLevel;
            bool rushOk = LevelData.GetUnlockedLevel(GameMode.Rush) >= car.requiredLevel;
            string heartIcon = heartOk ? "\u2713" : "\u2717";
            string rushIcon = rushOk ? "\u2713" : "\u2717";
            parts.Add($"{heartIcon} Heart Lv.{car.requiredLevel}");
            parts.Add($"{rushIcon} Rush Lv.{car.requiredLevel}");
        }
        if (car.requiredEndlessTier10 > 0)
        {
            int count = LevelData.GetEndlessTier10Count();
            string icon = count >= car.requiredEndlessTier10 ? "\u2713" : "\u2717";
            parts.Add($"{icon} Endless T10 x{count}/{car.requiredEndlessTier10}");
        }
        return string.Join("\n", parts);
    }

    // ── Purchase & selection ───────────────────────────────────────────────

    public bool TryUnlockCar(CarData car)
    {
        if (car == null || IsCarUnlocked(car)) return false;
        if (!CanUnlockCar(car)) return false;
        AddCoins(-car.unlockCost);
        PlayerPrefs.SetInt("CarUnlocked_" + car.carId, 1);
        PlayerPrefs.Save();
        FirestoreUserProfile.QueueSave("coins", "unlockedCars");
        return true;
    }

    public CarData SelectCar(CarData car, CarData current, System.Action onChanged)
    {
        if (car == null) return current;
        PlayerPrefs.SetString("SelectedCar", car.carId);
        PlayerPrefs.Save();
        FirestoreUserProfile.QueueSave("selectedCar");
        onChanged?.Invoke();
        return car;
    }

    public CarData LoadSelectedCar()
    {
        string savedId = PlayerPrefs.GetString("SelectedCar", "");
        if (catalog != null)
        {
            foreach (var car in catalog)
                if (car != null && car.carId == savedId)
                    return car;
            if (catalog.Length > 0 && catalog[0] != null)
                return catalog[0];
        }
        return null;
    }

    // ── Debug ──────────────────────────────────────────────────────────────

    public void UnlockAllCars()
    {
        if (catalog == null) return;
        foreach (var car in catalog)
            if (car != null) PlayerPrefs.SetInt("CarUnlocked_" + car.carId, 1);
        PlayerPrefs.Save();
        FirestoreUserProfile.QueueSave("unlockedCars");
    }

    public CarData ResetAllCars()
    {
        if (catalog == null) return null;
        foreach (var car in catalog)
        {
            if (car == null) continue;
            if (car.unlockCost == 0 && car.requiredLevel == 0 && car.requiredEndlessTier10 == 0) continue;
            PlayerPrefs.DeleteKey("CarUnlocked_" + car.carId);
        }
        PlayerPrefs.SetInt("Coins", 0);
        PlayerPrefs.SetInt("EndlessTier10Count", 0);
        CarData defaultCar = null;
        if (catalog.Length > 0 && catalog[0] != null)
            defaultCar = catalog[0];
        PlayerPrefs.Save();
        FirestoreUserProfile.QueueSave("coins", "unlockedCars", "selectedCar", "endlessTier10Count");
        return defaultCar;
    }
}
