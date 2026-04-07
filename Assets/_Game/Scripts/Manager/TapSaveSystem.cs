using UnityEngine;

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