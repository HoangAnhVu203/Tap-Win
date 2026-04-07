using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTapLevelController : MonoBehaviour
{
    [Serializable]
    public class PowerSector
    {
        public string name;
        public float minAngle;
        public float maxAngle;
        public int weight = 10;
        public int steps = 1;
    }

    [Header("Pointer")]
    [SerializeField] private RectTransform pointer;
    [SerializeField] private float pointerSpinDuration = 1.2f;
    [SerializeField] private int extraTurns = 2;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Button")]
    [SerializeField] private Button tapButton;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollDuration = 0.25f;

    [Header("Nodes (Bottom -> Top)")]
    [SerializeField] private List<LevelNodeUI> nodes = new List<LevelNodeUI>();

    [Header("Power Sectors")]
    [SerializeField] private List<PowerSector> sectors = new List<PowerSector>();

    [Header("Step")]
    [SerializeField] private float delayBetweenDisable = 0.12f;

    private bool isPlaying;
    private float currentPointerAngle;

    private void Start()
    {
        if (pointer != null)
            currentPointerAngle = pointer.localEulerAngles.z;

        ResetLevelView();
    }

    public void OnClickTap()
    {
        if (isPlaying) return;
        if (GetFirstAvailableNodeIndex() == -1) return;

        StartCoroutine(CoTap());
    }

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

    private IEnumerator CoTap()
    {
        isPlaying = true;

        if (tapButton != null)
            tapButton.interactable = false;

        PowerSector sector = GetRandomSector();
        float targetAngle = UnityEngine.Random.Range(
            Mathf.Min(sector.minAngle, sector.maxAngle),
            Mathf.Max(sector.minAngle, sector.maxAngle)
        );

        yield return StartCoroutine(CoRotatePointer(targetAngle));
        yield return StartCoroutine(CoDisableSteps(sector.steps));

        int firstAvailable = GetFirstAvailableNodeIndex();
        if (firstAvailable == -1)
        {
            Debug.Log("Hoàn thành level.");
        }
        else
        {
            Debug.Log("Ô đầu tiên chưa disable: " + firstAvailable);
        }

        if (tapButton != null)
            tapButton.interactable = true;

        isPlaying = false;
    }

    private PowerSector GetRandomSector()
    {
        int totalWeight = 0;
        for (int i = 0; i < sectors.Count; i++)
        {
            totalWeight += Mathf.Max(0, sectors[i].weight);
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < sectors.Count; i++)
        {
            cumulative += Mathf.Max(0, sectors[i].weight);
            if (randomValue < cumulative)
                return sectors[i];
        }

        return sectors[0];
    }

    private IEnumerator CoRotatePointer(float endAngle)
    {
        if (pointer == null) yield break;

        // Giới hạn trong 180 độ
        endAngle = Mathf.Clamp(endAngle, -80f, 80f);

        float startAngle = currentPointerAngle;

        // Xác định hướng sweep
        float sweepAngle;

        if (startAngle >= 0)
            sweepAngle = -80f; // đang bên phải → quét sang trái
        else
            sweepAngle = 80f;  // đang bên trái → quét sang phải

        // ===== PHASE 1: QUÉT HẾT 180 ĐỘ =====
        yield return StartCoroutine(RotateAngle(startAngle, sweepAngle, 0.35f));

        // ===== PHASE 2: QUAY NGƯỢC & DỪNG RANDOM =====
        yield return StartCoroutine(RotateAngle(sweepAngle, endAngle, pointerSpinDuration));

        currentPointerAngle = endAngle;
    }

    private IEnumerator RotateAngle(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float curveT = spinCurve.Evaluate(t);

            float angle = Mathf.Lerp(from, to, curveT);
            pointer.localEulerAngles = new Vector3(0f, 0f, angle);

            yield return null;
        }

        pointer.localEulerAngles = new Vector3(0f, 0f, to);
    }

    private IEnumerator CoDisableSteps(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            int index = GetFirstAvailableNodeIndex();
            if (index == -1)
                yield break;

            nodes[index].SetDisabled(true);

            yield return StartCoroutine(CoScrollToIndex(index));
            yield return new WaitForSeconds(delayBetweenDisable);
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

        // index 0 = ô dưới cùng
        // index cuối = ô trên cùng
        return (float)index / (nodes.Count - 1);
    }
}