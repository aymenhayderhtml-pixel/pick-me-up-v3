using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Model-level manager for Hub facilities (Tavern & Training Ground)
/// and real-time/offline clock synchronization safeguards.
/// Attached to GameManager GameObject.
/// </summary>
public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance { get; private set; }

    [Header("Tavern Configs")]
    [Tooltip("Morale recovered per hour during passive rest")]
    [SerializeField] private float passiveMoralePerHour = 5f;
    [Tooltip("Gold cost for an active Tavern premium meal")]
    [SerializeField] private int activeMealGoldCost = 200;
    [Tooltip("Morale recovered instantly from an active meal")]
    [SerializeField] private int activeMealMoraleRestore = 50;

    [Header("Training Ground Configs")]
    [Tooltip("XP granted per hour during passive training")]
    [SerializeField] private float passiveXpPerHour = 10f;

    [Header("Safeguards")]
    [Tooltip("Maximum minutes allowed for offline passive accumulation (8 Hours hard cap)")]
    [SerializeField] private int maxOfflineAccumulationMinutes = 480;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Automatically sync all facilities and subscriptions on start
        SyncOnGameLaunch();
    }

    // ─────────────────────────────────────────────────────
    // LAUNCH & REALTIME CLOCK SYNC
    // ─────────────────────────────────────────────────────
    public void SyncOnGameLaunch()
    {
        ResetDailyStaminaRefillsIfNeeded();
        ClaimSubscriptionDailyGems();
        SyncPassiveFacilities();
    }

    /// <summary>
    /// Processes offline Morale/XP accumulation for all resting/training units,
    /// enforcing timezone/clock manipulation safeguards.
    /// </summary>
    public void SyncPassiveFacilities()
    {
        var roster = GameManager.Instance.GetFullRoster();
        DateTime now = DateTime.UtcNow;

        foreach (var hero in roster)
        {
            if (hero.status == HeroStatus.Dead) continue;

            if (hero.status == HeroStatus.Resting)
            {
                if (string.IsNullOrEmpty(hero.facilityAssignmentDate))
                {
                    hero.facilityAssignmentDate = now.ToString("o");
                    continue;
                }

                if (DateTime.TryParse(hero.facilityAssignmentDate, out DateTime assignedDate))
                {
                    TimeSpan elapsed = now - assignedDate;
                    if (elapsed.TotalSeconds < 0) // Timezone manipulation bypass trigger
                    {
                        hero.facilityAssignmentDate = now.ToString("o");
                        continue;
                    }

                    // Enforce the 8-hour offline cap safeguard
                    int minutes = Mathf.Min(Mathf.FloorToInt((float)elapsed.TotalMinutes), maxOfflineAccumulationMinutes);
                    if (minutes > 0)
                    {
                        // Calculate morale gained dynamically based on hourly rest rate
                        float hours = minutes / 60f;
                        int moraleGained = Mathf.FloorToInt(hours * passiveMoralePerHour);
                        if (moraleGained > 0 && GameManager.Instance.MoraleSystem != null)
                        {
                            GameManager.Instance.MoraleSystem.ApplyMoraleDelta(hero, moraleGained, "Passive Tavern Rest");
                        }
                        // Advance the stamp by the minutes processed
                        hero.facilityAssignmentDate = assignedDate.AddMinutes(minutes).ToString("o");
                    }
                }
            }
            else if (hero.status == HeroStatus.Training)
            {
                if (string.IsNullOrEmpty(hero.facilityAssignmentDate))
                {
                    hero.facilityAssignmentDate = now.ToString("o");
                    continue;
                }

                if (DateTime.TryParse(hero.facilityAssignmentDate, out DateTime assignedDate))
                {
                    TimeSpan elapsed = now - assignedDate;
                    if (elapsed.TotalSeconds < 0) // Timezone manipulation bypass trigger
                    {
                        hero.facilityAssignmentDate = now.ToString("o");
                        continue;
                    }

                    // Enforce the 8-hour offline cap safeguard
                    int minutes = Mathf.Min(Mathf.FloorToInt((float)elapsed.TotalMinutes), maxOfflineAccumulationMinutes);
                    if (minutes > 0)
                    {
                        // Calculate XP gained dynamically based on hourly training rate
                        float hours = minutes / 60f;
                        int xpGained = Mathf.FloorToInt(hours * passiveXpPerHour);
                        if (xpGained > 0)
                        {
                            hero.AddXP(xpGained, hero.data);
                        }
                        // Advance stamp
                        hero.facilityAssignmentDate = assignedDate.AddMinutes(minutes).ToString("o");
                    }
                }
            }
        }

        GameManager.Instance.SaveGame();
    }

    // ─────────────────────────────────────────────────────
    // TAVERN (RESTING)
    // ─────────────────────────────────────────────────────
    public bool AssignHeroToRest(HeroInstance hero)
    {
        if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed || hero.status == HeroStatus.Resting)
            return false;

        hero.status = HeroStatus.Resting;
        hero.facilityAssignmentDate = DateTime.UtcNow.ToString("o");
        GameManager.Instance.SaveGame();
        return true;
    }

    public bool RemoveHeroFromRest(HeroInstance hero)
    {
        if (hero.status != HeroStatus.Resting) return false;

        // Sync first to apply any final pending recovery
        SyncPassiveFacilities();

        hero.status = HeroStatus.Active;
        hero.facilityAssignmentDate = "";
        
        // Re-evaluate if Wounded or Fatigued
        if (hero.HPPercent < 1f && GameManager.Instance.MoraleSystem != null)
        {
            GameManager.Instance.MoraleSystem.MarkWoundedIfNeeded(hero, hero.HPPercent);
        }

        GameManager.Instance.SaveGame();
        return true;
    }

    /// <summary>
    /// Premium active meal. Gold sink to instantly restore 50 morale.
    /// </summary>
    public bool BuyInstantMeal(HeroInstance hero)
    {
        if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed) return false;
        if (hero.morale >= 100) return false;

        if (GameManager.Instance.SpendGold(activeMealGoldCost))
        {
            if (GameManager.Instance.MoraleSystem != null)
            {
                GameManager.Instance.MoraleSystem.ApplyMoraleDelta(hero, activeMealMoraleRestore, "Instant Premium Meal");
            }
            GameManager.Instance.SaveGame();
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────
    // TRAINING GROUNDS (TRAINING)
    // ─────────────────────────────────────────────────────
    public bool AssignHeroToTraining(HeroInstance hero)
    {
        if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed || hero.status == HeroStatus.Training)
            return false;

        hero.status = HeroStatus.Training;
        hero.facilityAssignmentDate = DateTime.UtcNow.ToString("o");
        GameManager.Instance.SaveGame();
        return true;
    }

    public bool RemoveHeroFromTraining(HeroInstance hero)
    {
        if (hero.status != HeroStatus.Training) return false;

        // Sync first to apply final pending XP
        SyncPassiveFacilities();

        hero.status = HeroStatus.Active;
        hero.facilityAssignmentDate = "";

        GameManager.Instance.SaveGame();
        return true;
    }

    // ─────────────────────────────────────────────────────
    // STAMINA & SUBSCRIPTION CLAIMS
    // ─────────────────────────────────────────────────────
    public void ClaimSubscriptionDailyGems()
    {
        var state = GameManager.Instance.State;
        if (!state.isSubscriptionActive) return;

        string todayString = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (state.lastSubscriptionClaimDate != todayString)
        {
            GameManager.Instance.AddGems(300); // Monthly Card daily reward
            state.lastSubscriptionClaimDate = todayString;
            GameManager.Instance.SaveGame();
            Debug.Log("[FacilityManager] Claimed Daily 300 Gems from Subscription Card.");
        }
    }

    public void ResetDailyStaminaRefillsIfNeeded()
    {
        var state = GameManager.Instance.State;
        string todayString = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (state.lastStaminaRefillResetDate != todayString)
        {
            state.dailyStaminaRefillsCount = 0;
            state.lastStaminaRefillResetDate = todayString;
            GameManager.Instance.SaveGame();
            Debug.Log("[FacilityManager] Reset Daily progressive Stamina Gem refills counter.");
        }
    }
}
