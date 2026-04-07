using System;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public static EnergySystem Instance { get; private set; }

    private const string KEY_ENERGY = "ENERGY_SAVE";

    [SerializeField] private int defaultEnergy = 2000;

    private int currentEnergy;

    public int CurrentEnergy => currentEnergy;

    public event Action<int> OnEnergyChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        currentEnergy += amount;
        Save();
        OnEnergyChanged?.Invoke(currentEnergy);
    }

    public bool CanSpend(int amount)
    {
        return currentEnergy >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;

        if (currentEnergy < amount)
            return false;

        currentEnergy -= amount;
        Save();
        OnEnergyChanged?.Invoke(currentEnergy);
        return true;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_ENERGY, currentEnergy);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        currentEnergy = PlayerPrefs.GetInt(KEY_ENERGY, defaultEnergy);
    }
}