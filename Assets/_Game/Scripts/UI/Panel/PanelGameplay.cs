using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelGameplay : UICanvas
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
    [SerializeField] private float minPointerAngle = -80f;
    [SerializeField] private float maxPointerAngle = 80f;
    [SerializeField] private float sweepDuration = 0.35f;
    [SerializeField] private float pointerSpinDuration = 0.8f;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Button")]
    [SerializeField] private Button tapButton;

    [Header("Reference")]
    [SerializeField] private LevelManager levelManager;

    [Header("Power Sectors")]
    [SerializeField] private List<PowerSector> sectors = new List<PowerSector>();

    [Header("Energy UI")]
    [SerializeField] private Text txtEnergyCurrent;
    [SerializeField] private Text txtEnergyCost;

    [Header("Coin Reward")]
    [SerializeField] private int coinPerStep = 1;
    [SerializeField] private int levelCompleteBonusCoin = 100;
    [SerializeField] private Text txtCoinTotal;

    private bool isPlaying;
    private float currentPointerAngle;

    private void Start()
    {
        if (pointer != null)
        {
            currentPointerAngle = NormalizeAngle(pointer.localEulerAngles.z);
            SetPointerAngle(currentPointerAngle);
        }

        if (tapButton != null)
            tapButton.onClick.AddListener(OnClickTap);

        if (EnergySystem.Instance != null)
            EnergySystem.Instance.OnEnergyChanged += OnEnergyChanged;

        CoinSystem.OnCoinChanged += OnCoinChanged;

        EnsureCoinText();
        UpdateEnergyUI();
        UpdateCoinUI();
    }

    private void OnDestroy()
    {
        if (tapButton != null)
            tapButton.onClick.RemoveListener(OnClickTap);

        if (EnergySystem.Instance != null)
            EnergySystem.Instance.OnEnergyChanged -= OnEnergyChanged;

        CoinSystem.OnCoinChanged -= OnCoinChanged;
    }

    private void OnEnergyChanged(int value)
    {
        UpdateEnergyUI();
    }

    private void OnCoinChanged(int value)
    {
        UpdateCoinUI();
    }

    public void OnClickTap()
    {
        if (isPlaying) return;
        if (levelManager == null || !levelManager.HasCurrentLevel()) return;
        if (!levelManager.CurrentLevel.HasAvailableNode()) return;

        int cost = GetEnergyCost();

        if (!EnergySystem.Instance.TrySpend(cost))
        {
            Debug.Log("Không đủ năng lượng!");
            return;
        }

        UpdateEnergyUI();

        StartCoroutine(CoTap());
    }

    private IEnumerator CoTap()
    {
        isPlaying = true;

        if (tapButton != null)
            tapButton.interactable = false;

        PowerSector sector = GetRandomSector();
        if (sector == null)
        {
            Debug.LogError("PowerSector rỗng.");
            EndTap();
            yield break;
        }

        float targetAngle = UnityEngine.Random.Range(
            Mathf.Min(sector.minAngle, sector.maxAngle),
            Mathf.Max(sector.minAngle, sector.maxAngle)
        );

        targetAngle = Mathf.Clamp(targetAngle, minPointerAngle, maxPointerAngle);

        yield return StartCoroutine(CoRotatePointer(targetAngle));

        TapLevelView currentLevel = levelManager.CurrentLevel;
        int rewardCoin = 0;
        bool completedLevel = false;

        if (currentLevel != null)
        {
            yield return StartCoroutine(currentLevel.DisableStepsRoutine(sector.steps));

            int movedSteps = currentLevel.LastDisabledCount;
            completedLevel = currentLevel.LastActionCompletedLevel;

            rewardCoin = movedSteps * Mathf.Max(0, coinPerStep);
            if (completedLevel)
                rewardCoin += Mathf.Max(0, levelCompleteBonusCoin);
        }

        if (rewardCoin > 0)
        {
            CoinSystem.AddCoins(rewardCoin);
            yield return StartCoroutine(CoShowPrize(rewardCoin));
        }

        if (completedLevel && currentLevel != null)
        {
            currentLevel.CompleteLevel();
        }

        EndTap();
    }

    private void EndTap()
    {
        if (tapButton != null)
            tapButton.interactable = true;

        isPlaying = false;
    }

    private PowerSector GetRandomSector()
    {
        if (sectors == null || sectors.Count == 0)
            return null;

        int totalWeight = 0;
        for (int i = 0; i < sectors.Count; i++)
        {
            totalWeight += Mathf.Max(0, sectors[i].weight);
        }

        if (totalWeight <= 0)
            return sectors[0];

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
        if (pointer == null)
            yield break;

        endAngle = Mathf.Clamp(endAngle, minPointerAngle, maxPointerAngle);

        float startAngle = currentPointerAngle;

        float sweepAngle = (startAngle >= 0f) ? minPointerAngle : maxPointerAngle;

        yield return StartCoroutine(RotateAngle(startAngle, sweepAngle, sweepDuration));
        yield return StartCoroutine(RotateAngle(sweepAngle, endAngle, pointerSpinDuration));

        currentPointerAngle = endAngle;
        SetPointerAngle(currentPointerAngle);
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
            SetPointerAngle(angle);

            yield return null;
        }

        SetPointerAngle(to);
    }

    private void SetPointerAngle(float angle)
    {
        if (pointer == null) return;
        pointer.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private int GetEnergyCost()
    {
        if (levelManager == null) return 200;

        int level = levelManager.CurrentLevelNumber;
        return level * 200;
    }

    private void UpdateEnergyUI()
    {
        if (EnergySystem.Instance == null) return;

        int current = EnergySystem.Instance.CurrentEnergy;
        int cost = GetEnergyCost();

        if (txtEnergyCurrent != null)
            txtEnergyCurrent.text = current.ToString();

        if (txtEnergyCost != null)
            txtEnergyCost.text = cost.ToString();
    }

    private void UpdateCoinUI()
    {
        EnsureCoinText();

        if (txtCoinTotal != null)
            txtCoinTotal.text = $"{CoinSystem.CurrentCoin}";
    }

    private IEnumerator CoShowPrize(int rewardCoin)
    {
        PanelPrize panelPrize = UIManager.Instance.OpenUI<PanelPrize>();
        panelPrize.ShowReward(rewardCoin);

        yield return new WaitUntil(() => panelPrize == null || !panelPrize.IsShowing);
    }

    private void EnsureCoinText()
    {
        if (txtCoinTotal != null)
            return;

        GameObject coinTextObject = new GameObject("CoinTotalText");
        coinTextObject.transform.SetParent(transform, false);

        RectTransform rect = coinTextObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-70f, -90f);
        rect.sizeDelta = new Vector2(320f, 70f);

        Text text = coinTextObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = coinTextObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        txtCoinTotal = text;
    }

    public void OpenClaimEnergyPanel() => UIManager.Instance.OpenUI<PanelClaimEnergy>();
}
