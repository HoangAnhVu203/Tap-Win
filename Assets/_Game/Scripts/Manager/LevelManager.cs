using System.Collections.Generic;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Spawn")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private List<TapLevelView> levelPrefabs = new List<TapLevelView>();

    [Header("State")]
    [SerializeField] private int startLevelIndex = 0;

    private TapLevelView currentLevelInstance;
    private int currentLevelIndex;

    public TapLevelView CurrentLevel => currentLevelInstance;
    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentLevelNumber => currentLevelIndex + 1;

    private void Start()
    {
        var save = TapSaveSystem.Load();

        if (save != null)
        {
            currentLevelIndex = save.levelIndex;
        }
        else
        {
            currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Mathf.Max(0, levelPrefabs.Count - 1));
        }

        SpawnCurrentLevel();

        // Restore node progress
        if (save != null && currentLevelInstance != null)
        {
            currentLevelInstance.RestoreProgress(save.nodeIndex);
        }
    }

    public void SpawnCurrentLevel()
    {
        ClearCurrentLevel();

        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            Debug.LogError("Chưa có level prefab.");
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= levelPrefabs.Count)
        {
            Debug.LogWarning("Đã hết level.");
            return;
        }

        currentLevelInstance = Instantiate(levelPrefabs[currentLevelIndex], levelRoot);
        currentLevelInstance.ResetLevelView();
        currentLevelInstance.OnLevelCompleted += HandleLevelCompleted;
    }

    public bool HasCurrentLevel()
    {
        return currentLevelInstance != null;
    }

    public void LoadNextLevel()
    {
        currentLevelIndex++;

        if (currentLevelIndex >= levelPrefabs.Count)
        {
            Debug.Log("Đã hoàn thành tất cả level.");
            ClearCurrentLevel();
            return;
        }

        SpawnCurrentLevel();
    }

    private void HandleLevelCompleted(TapLevelView completedLevel)
    {
        if (completedLevel != currentLevelInstance)
            return;

        Debug.Log("Level hoàn thành: " + completedLevel.LevelId);
        TapSaveSystem.Save(currentLevelIndex + 1, 0);

        LoadNextLevel();
    }

    private void ClearCurrentLevel()
    {
        if (currentLevelInstance != null)
        {
            currentLevelInstance.OnLevelCompleted -= HandleLevelCompleted;
            Destroy(currentLevelInstance.gameObject);
            currentLevelInstance = null;
        }
    }
}