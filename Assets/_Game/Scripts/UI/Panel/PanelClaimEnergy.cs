using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelClaimEnergy : UICanvas
{
    [Header("Free Energy Config")]
    [SerializeField] private int maxFreeEnergy = 2000;
    [SerializeField] private int fullMinutes = 150;

    [Header("Free Energy UI")]
    [SerializeField] private Image progressFill;
    [SerializeField] private Text txtCountdown;
    [SerializeField] private Text txtProgress;
    [SerializeField] private Button btnClaim;

    [Header("Extra Reward UI")]
    [SerializeField] private Button btnGo;
    [SerializeField] private int adRewardEnergy = 1000;

    private const string KEY_FREE_START_TICKS = "CLAIM_ENERGY_FREE_START_TICKS";

    private DateTime startTimeUtc;

    private double FullSeconds => fullMinutes * 60.0;

    private void Start()
    {
        LoadStartTime();

        if (btnClaim != null)
            btnClaim.onClick.AddListener(OnClickClaim);

        if (btnGo != null)
            btnGo.onClick.AddListener(OnClickGo);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (btnClaim != null)
            btnClaim.onClick.RemoveListener(OnClickClaim);

        if (btnGo != null)
            btnGo.onClick.RemoveListener(OnClickGo);

    }

    private void Update()
    {
        RefreshUI();
    }

    private void LoadStartTime()
    {
        if (PlayerPrefs.HasKey(KEY_FREE_START_TICKS))
        {
            string raw = PlayerPrefs.GetString(KEY_FREE_START_TICKS);
            if (long.TryParse(raw, out long ticks))
            {
                startTimeUtc = new DateTime(ticks, DateTimeKind.Utc);
                return;
            }
        }

        startTimeUtc = DateTime.UtcNow;
        SaveStartTime();
    }

    private void SaveStartTime()
    {
        PlayerPrefs.SetString(KEY_FREE_START_TICKS, startTimeUtc.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private double GetElapsedSeconds()
    {
        return (DateTime.UtcNow - startTimeUtc).TotalSeconds;
    }

    private double GetClampedElapsedSeconds()
    {
        return Math.Min(GetElapsedSeconds(), FullSeconds);
    }

    private int GetCurrentFreeEnergy()
    {
        double ratio = GetClampedElapsedSeconds() / FullSeconds;
        int value = Mathf.FloorToInt((float)(ratio * maxFreeEnergy));
        return Mathf.Clamp(value, 0, maxFreeEnergy);
    }

    private bool IsFull()
    {
        return GetElapsedSeconds() >= FullSeconds;
    }

    private TimeSpan GetRemainingTime()
    {
        double remain = Math.Max(0, FullSeconds - GetElapsedSeconds());
        return TimeSpan.FromSeconds(remain);
    }

    private void RefreshUI()
    {
        int currentEnergy = GetCurrentFreeEnergy();

        if (txtProgress != null)
            txtProgress.text = $"{currentEnergy}/{maxFreeEnergy}";

        if (progressFill != null)
            progressFill.fillAmount = maxFreeEnergy > 0 ? (float)currentEnergy / maxFreeEnergy : 0f;

        if (txtCountdown != null)
        {
            if (IsFull())
            {
                txtCountdown.text = "Full";
            }
            else
            {
                TimeSpan remain = GetRemainingTime();
                txtCountdown.text = $"{remain.Hours:00}:{remain.Minutes:00}:{remain.Seconds:00}";
            }
        }

        if (btnClaim != null)
            btnClaim.interactable = currentEnergy > 0;
    }

    private void OnClickClaim()
    {
        int amount = GetCurrentFreeEnergy();
        if (amount <= 0) return;

        if (EnergySystem.Instance != null)
            EnergySystem.Instance.AddEnergy(amount);

        startTimeUtc = DateTime.UtcNow;
        SaveStartTime();
        RefreshUI();
    }

    private void OnClickGo()
    {
        if (EnergySystem.Instance != null)
            EnergySystem.Instance.AddEnergy(adRewardEnergy);

        Debug.Log($"+{adRewardEnergy} energy");
    }

    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelClaimEnergy>();
    }
}