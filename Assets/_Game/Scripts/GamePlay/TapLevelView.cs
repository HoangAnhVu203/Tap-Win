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
    public int LastDisabledCount { get; private set; }
    public bool LastActionCompletedLevel { get; private set; }

    public event Action<TapLevelView> OnLevelCompleted;

    private bool hasNotifiedLevelCompleted;

    private void Awake()
    {
        RefreshNodeDrawOrder();
    }
    public void ResetLevelView()
    {
        LastDisabledCount = 0;
        LastActionCompletedLevel = false;
        hasNotifiedLevelCompleted = false;

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
        LastDisabledCount = 0;
        LastActionCompletedLevel = false;

        for (int i = 0; i < steps; i++)
        {
            int index = GetFirstAvailableNodeIndex();
            if (index == -1)
                break;

            nodes[index].SetDisabled(true);
            LastDisabledCount++;

            TapSaveSystem.Save(
                LevelManager.Instance.CurrentLevelIndex,
                index + 1
            );

            int focusIndex = GetFirstAvailableNodeIndex();
            if (focusIndex == -1)
                focusIndex = index;

            yield return CoScrollToIndex(focusIndex);
            yield return new WaitForSeconds(delayBetweenDisable);
        }

        LastActionCompletedLevel = GetFirstAvailableNodeIndex() == -1;
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
        if (scrollRect == null || nodes.Count == 0)
            yield break;

        float start = scrollRect.verticalNormalizedPosition;
        float target = GetNormalizedByIndex(index);

        if (Mathf.Approximately(start, target))
        {
            scrollRect.verticalNormalizedPosition = target;
            yield break;
        }

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
        if (nodes.Count == 0)
            return 0f;

        int clampedIndex = Mathf.Clamp(index, 0, nodes.Count - 1);

        if (!TryGetScrollRects(out RectTransform contentRect, out RectTransform viewportRect))
        {
            if (nodes.Count <= 1)
                return 0f;

            return (float)clampedIndex / (nodes.Count - 1);
        }

        RectTransform nodeRect = nodes[clampedIndex] != null ? nodes[clampedIndex].Rect : null;
        if (nodeRect == null)
        {
            if (nodes.Count <= 1)
                return 0f;

            return (float)clampedIndex / (nodes.Count - 1);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float scrollableHeight = contentRect.rect.height - viewportRect.rect.height;
        if (scrollableHeight <= 0.001f)
            return 0f;

        Vector3[] nodeCorners = new Vector3[4];
        nodeRect.GetWorldCorners(nodeCorners);

        Vector3 nodeCenterWorld = (nodeCorners[0] + nodeCorners[2]) * 0.5f;
        Vector3 nodeCenterLocal = contentRect.InverseTransformPoint(nodeCenterWorld);

        float nodeCenterFromBottom = nodeCenterLocal.y + (contentRect.rect.height * contentRect.pivot.y);
        float targetScrollFromBottom = nodeCenterFromBottom - (viewportRect.rect.height * 0.5f);

        return Mathf.Clamp01(targetScrollFromBottom / scrollableHeight);
    }

    public void RestoreProgress(int nodeIndex)
    {
        LastDisabledCount = 0;
        LastActionCompletedLevel = false;
        hasNotifiedLevelCompleted = false;

        int disabledCount = Mathf.Clamp(nodeIndex, 0, nodes.Count);

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;

            if (i < disabledCount)
                nodes[i].SetDisabled(true);
            else
                nodes[i].SetDisabled(false);
        }

        if (scrollRect != null)
        {
            int focusIndex = GetFirstAvailableNodeIndex();
            if (focusIndex == -1 && nodes.Count > 0)
                focusIndex = nodes.Count - 1;

            scrollRect.verticalNormalizedPosition = focusIndex == -1 ? 0f : GetNormalizedByIndex(focusIndex);
        }
    }

    public void CompleteLevel()
    {
        if (hasNotifiedLevelCompleted)
            return;

        if (!LastActionCompletedLevel && GetFirstAvailableNodeIndex() != -1)
            return;

        hasNotifiedLevelCompleted = true;
        OnLevelCompleted?.Invoke(this);
    }

    public void RefreshNodeDrawOrder()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            nodes[i].transform.SetSiblingIndex(i);
        }
    }

    private bool TryGetScrollRects(out RectTransform contentRect, out RectTransform viewportRect)
    {
        contentRect = null;
        viewportRect = null;

        if (scrollRect == null)
            return false;

        contentRect = scrollRect.content;
        viewportRect = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.GetComponent<RectTransform>();

        return contentRect != null && viewportRect != null;
    }
}
