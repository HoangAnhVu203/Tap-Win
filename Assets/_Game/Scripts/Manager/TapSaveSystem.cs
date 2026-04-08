using UnityEngine;
using System;

public static class TapSaveSystem
{
    private const string KEY = "TAP_SAVE";

    public static void Save(int levelIndex, int nodeIndex)
    {
        TapSaveData data = new TapSaveData
        {
            levelIndex = levelIndex,
            nodeIndex = nodeIndex
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static TapSaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
            return null;

        string json = PlayerPrefs.GetString(KEY);
        return JsonUtility.FromJson<TapSaveData>(json);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY);
    }
}

public static class CoinSystem
{
    private const string KEY_COIN = "COIN_SAVE";
    private const int DEFAULT_COIN = 0;

    private static bool hasLoaded;
    private static int currentCoin;

    public static int CurrentCoin
    {
        get
        {
            EnsureLoaded();
            return currentCoin;
        }
    }

    public static event Action<int> OnCoinChanged;

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        EnsureLoaded();

        currentCoin += amount;
        SaveCoin();
        OnCoinChanged?.Invoke(currentCoin);
    }

    public static bool TrySpendCoins(int amount)
    {
        EnsureLoaded();

        if (amount <= 0)
            return true;

        if (currentCoin < amount)
            return false;

        currentCoin -= amount;
        SaveCoin();
        OnCoinChanged?.Invoke(currentCoin);
        return true;
    }

    public static void SetCoins(int amount)
    {
        EnsureLoaded();

        currentCoin = Mathf.Max(0, amount);
        SaveCoin();
        OnCoinChanged?.Invoke(currentCoin);
    }

    private static void EnsureLoaded()
    {
        if (hasLoaded)
            return;

        currentCoin = PlayerPrefs.GetInt(KEY_COIN, DEFAULT_COIN);
        hasLoaded = true;
    }

    private static void SaveCoin()
    {
        PlayerPrefs.SetInt(KEY_COIN, currentCoin);
        PlayerPrefs.Save();
    }
}
