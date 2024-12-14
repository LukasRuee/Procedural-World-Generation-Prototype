using MyBox;
using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InGameUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text points;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private TMP_Text health;
    private int score;
    static public InGameUI Instance { get; private set; }
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }
    void Update()
    {
        if(InventoryManager.Instance.SelectedWand != null)
        {
            SetMana(InventoryManager.Instance.SelectedWand.CurrentMana);
        }
        else
        {
            SetMana(0);
        }
    }
    /// <summary>
    /// Sets the score
    /// </summary>
    /// <param name="score"></param>
    public void SetScore(int score)
    {
        this.score += score;
        points.text = "Score: " + this.score.ToString();
    }
    /// <summary>
    /// Adds points to the score
    /// </summary>
    /// <param name="score"></param>
    public void AddScore(int score)
    {
        this.score += score;
        points.text = "Score: " + this.score.ToString();
    }
    /// <summary>
    /// Sets the mana
    /// </summary>
    /// <param name="mana"></param>
    public void SetMana(float mana)
    {
        this.mana.text = "Mana: " + mana.RoundToInt().ToString();
    }
    /// <summary>
    /// Sets the health
    /// </summary>
    /// <param name="system"></param>
    public void SetHealth(HealthSystem system)
    {
        health.text = "Health: " + system.Health.ToString();
    }
}
