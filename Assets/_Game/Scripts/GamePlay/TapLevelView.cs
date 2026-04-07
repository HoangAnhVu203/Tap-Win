using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TapLevelView : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private int levelId = 1;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollDuration = 0.2f;

    [Header("Nodes (Bottom -> Top)")]
    [SerializeField] private List<LevelNodeUI> nodes = new List<LevelNodeUI>();

    [Header("Step")]
    [SerializeField] private float delayBetweenDisable = 0.12f;

    public int LevelId => levelId;
    public bool IsCompleted => GetFirstAvailableNodeIndex() == -1;
    public int TotalNodeCount => nodes.Count;

    public event Action<TapLevelView> OnLevelCompleted;

    public void ResetLevelView()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                nodes[i].SetDisabled(false);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    public bool HasAvailableNode()
    {
        return GetFirstAvailableNodeIndex() != -1;
    }

    public IEnumerator DisableStepsRoutine(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int index = GetFirstAvailableNodeIndex();
            if (index == -1)
                break;

            nodes[index].SetDisabled(true);

            TapSaveSystem.Save(
                LevelManager.Instance.CurrentLevelIndex,
                index + 1
            );

            yield return CoScrollToIndex(index);
            yield return new WaitForSeconds(delayBetweenDisable);
        }

        if (GetFirstAvailableNodeIndex() == -1)
        {
            OnLevelCompleted?.Invoke(this);
        }
    }

    private int GetFirstAvailableNodeIndex()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && !nodes[i].IsDisabled)
                return i;
        }

        return -1;
    }

    private IEnumerator CoScrollToIndex(int index)
    {
        if (scrollRect == null || nodes.Count <= 1)
            yield break;

        float start = scrollRect.verticalNormalizedPosition;
        float target = GetNormalizedByIndex(index);

        float time = 0f;
        while (time < scrollDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / scrollDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = target;
    }

    private float GetNormalizedByIndex(int index)
    {
        if (nodes.Count <= 1) return 0f;
        return (float)index / (nodes.Count - 1);
    }

    public void RestoreProgress(int nodeIndex)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;

            if (i < nodeIndex)
                nodes[i].SetDisabled(true);
            else
                nodes[i].SetDisabled(false);
        }

        if (scrollRect != null)
        {
            float normalized = nodes.Count <= 1 ? 0f : (float)nodeIndex / (nodes.Count - 1);
            scrollRect.verticalNormalizedPosition = normalized;
        }
    }

    
}