using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Attach to: GameManager GameObject (alongside GameManager.cs).
/// Controls all morale calculations, resting recovery, status updates,
/// and personality trait logic.
/// </summary>
public class MoraleSystem : MonoBehaviour
{
    // ─── Tunables ─────────────────────────────────────────────────────────────

    [Header("Battle Morale Costs")]
    [Tooltip("Morale lost per ally death witnessed in the same battle")]
    [SerializeField] private int moralePerAllyDeath = 12;

    [Tooltip("Morale lost for being in a losing battle (all heroes wiped)")]
    [SerializeField] private int moraleLossBattleLost = 20;

    [Tooltip("Morale gained for surviving a victorious battle")]
    [SerializeField] private int moraleGainBattleWon = 10;

    [Tooltip("Bonus morale for survivors when no allies died")]
    [SerializeField] private int moraleGainFlawlessBonus = 5;

    [Header("Passive Decay")]
    [Tooltip("Real-time seconds between passive morale ticks")]
    [SerializeField] private float decayIntervalSeconds = 600f; // 10 min real time

    [Tooltip("Morale lost per passive tick (Active heroes only)")]
    [SerializeField] private int passiveDecayAmount = 2;

    [Header("Rest")]
    [Tooltip("Morale restored per rest action (costs gold)")]
    [SerializeField] private int restMoraleRestore = 30;

    [Tooltip("Gold cost per rest")]
    [SerializeField] private int restGoldCost = 50;

    [Tooltip("Minimum morale before Fatigued status clears on rest")]
    [SerializeField] private int fatiguedClearThreshold = 40;

    [Header("State Thresholds")]
    [SerializeField] private int fatiguedThreshold = 20;      // morale < this → Fatigued
    [SerializeField] private int rebellionThreshold = 30;     // Rebellious trait + morale < this → refuses deploy
    [SerializeField] private int woundedMoraleDecayMult = 2;  // Wounded heroes decay faster

    // ─── Events ───────────────────────────────────────────────────────────────

    public static event Action<HeroInstance, int, int> OnMoraleChanged;
    public static event Action<HeroInstance, HeroStatus, HeroStatus> OnHeroStatusChanged;
    public static event Action<HeroInstance> OnHeroRebelling;

    // ─── State ────────────────────────────────────────────────────────────────

    private float _decayTimer;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnHeroDied += HandleHeroDied;
    }

    private void OnDisable()
    {
        GameManager.OnHeroDied -= HandleHeroDied;
    }

    private void Update()
    {
        _decayTimer += Time.deltaTime;
        if (_decayTimer >= decayIntervalSeconds)
        {
            _decayTimer = 0f;
            TickPassiveDecay();
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Call from GameManager immediately after a BattleResult is processed.
    /// Handles all morale consequences for every participant.
    /// </summary>
    public void ResolveBattleMorale(BattleResult result)
    {
        if (result == null) return;

        int alliesKilled = result.fallen?.Count ?? 0;
        bool flawless = alliesKilled == 0 && result.victory;

        // Process survivors
        if (result.survivors != null)
        {
            foreach (var hero in result.survivors)
            {
                if (hero == null || hero.status == HeroStatus.Dead) continue;

                int delta = 0;

                if (result.victory)
                {
                    delta += moraleGainBattleWon;
                    if (flawless) delta += moraleGainFlawlessBonus;

                    // Close-death trauma penalty (HP < 25%)
                    if (hero.HPPercent < 0.25f)
                    {
                        delta -= 30; // Trauma hit
                        AddHistory(hero, "Barely survived the deployment (Trauma -30 Morale).");
                    }
                }
                else
                {
                    delta -= moraleLossBattleLost;
                }

                // Grief per fallen ally
                delta -= alliesKilled * moralePerAllyDeath;

                // Brave trait: small bonus for winning regardless of losses
                if (hero.HasTrait(PersonalityTrait.Brave) && result.victory)
                    delta += 5;

                // Multiply final delta using dynamic battle morale modifier from TraitSystem
                float modifier = TraitSystem.GetBattleMoraleModifier(hero, result);
                ApplyMoraleDelta(hero, Mathf.RoundToInt(delta * modifier), "Battle resolution");
            }
        }

        // The fallen already have Die() called; apply grief to roster witnesses
        if (result.fallen != null && result.fallen.Count > 0)
        {
            ApplyGriefToRosterWitnesses(result.fallen);
        }
    }

    /// <summary>
    /// Manual rest. Returns false if hero can't rest (wrong status, insufficient gold, already full morale).
    /// Call from RosterUI / LobbyUI rest button.
    /// </summary>
    public bool TryRest(HeroInstance hero)
    {
        if (hero == null || hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed)
            return false;

        if (hero.morale >= 100)
            return false;

        if (!GameManager.Instance.SpendGold(restGoldCost))
            return false;

        int oldMorale = hero.morale;
        hero.morale = Mathf.Min(100, hero.morale + restMoraleRestore);

        AddHistory(hero, $"Rested. Morale {oldMorale} → {hero.morale}.");
        OnMoraleChanged?.Invoke(hero, oldMorale, hero.morale);

        // Clear Fatigued if morale recovered enough
        if (hero.status == HeroStatus.Fatigued && hero.morale >= fatiguedClearThreshold)
            SetStatus(hero, HeroStatus.Active);

        EvaluateStatusTransitions(hero);
        return true;
    }

    /// <summary>
    /// Check if hero is allowed to be deployed.
    /// Rebellious heroes with morale below threshold refuse; fires OnHeroRebelling.
    /// </summary>
    public bool CanDeploy(HeroInstance hero)
    {
        string blockReason = TraitSystem.GetDeployBlockReason(hero);
        if (blockReason != null)
        {
            if (hero != null && hero.HasTrait(PersonalityTrait.Rebellious) && hero.morale < rebellionThreshold)
            {
                OnHeroRebelling?.Invoke(hero);
                AddHistory(hero, $"Refused to deploy (morale {hero.morale}, rebellious). Reason: {blockReason}");
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Mark hero as Wounded (post-battle HP not full).
    /// Called by GameManager after applying BattleResult XP/damage.
    /// </summary>
    public void MarkWoundedIfNeeded(HeroInstance hero, float hpPercent)
    {
        if (hero == null || hero.status == HeroStatus.Dead) return;

        if (hpPercent < 1f && hero.status == HeroStatus.Active)
        {
            SetStatus(hero, HeroStatus.Wounded);
            AddHistory(hero, $"Returned wounded from battle ({hpPercent * 100f:F0}% HP).");
        }
        else if (hpPercent >= 1f && hero.status == HeroStatus.Wounded)
        {
            SetStatus(hero, HeroStatus.Active);
        }
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void TickPassiveDecay()
    {
        var roster = GameManager.Instance.GetFullRoster();
        foreach (var hero in roster)
        {
            if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed) continue;

            int decay = Mathf.RoundToInt(passiveDecayAmount * TraitSystem.GetMoraleDecayMultiplier(hero));
            if (hero.status == HeroStatus.Wounded) decay *= woundedMoraleDecayMult;

            ApplyMoraleDelta(hero, -decay, "Passive decay");
        }
    }

    public void ApplyMoraleDelta(HeroInstance hero, int delta, string reason)
    {
        if (delta == 0) return;

        int oldMorale = hero.morale;
        hero.ModifyMorale(delta); // Runs personality modifiers safely!

        if (oldMorale == hero.morale) return;

        OnMoraleChanged?.Invoke(hero, oldMorale, hero.morale);
        EvaluateStatusTransitions(hero);

#if UNITY_EDITOR
        Debug.Log($"[Morale] {hero.heroName}: {oldMorale} → {hero.morale} ({reason}, Δ{delta})");
#endif
    }

    private void EvaluateStatusTransitions(HeroInstance hero)
    {
        if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed) return;

        // Fatigued threshold check
        if (hero.morale < fatiguedThreshold && hero.status != HeroStatus.Fatigued)
        {
            SetStatus(hero, HeroStatus.Fatigued);
            AddHistory(hero, $"Became fatigued (morale dropped to {hero.morale}).");
        }

        // Rebellion check (Rebellious trait)
        if (hero.HasTrait(PersonalityTrait.Rebellious) && hero.morale < rebellionThreshold)
        {
            OnHeroRebelling?.Invoke(hero);
        }
    }

    private void SetStatus(HeroInstance hero, HeroStatus newStatus)
    {
        HeroStatus old = hero.status;
        if (old == newStatus) return;
        hero.status = newStatus;
        OnHeroStatusChanged?.Invoke(hero, old, newStatus);
    }

    private void ApplyGriefToRosterWitnesses(List<HeroInstance> fallen)
    {
        var roster = GameManager.Instance.GetFullRoster();
        foreach (var hero in roster)
        {
            if (hero.status == HeroStatus.Dead || hero.status == HeroStatus.Deployed) continue;
            if (fallen.Any(f => f.instanceId == hero.instanceId)) continue; // skip the fallen themselves

            float grief = fallen.Count * 6f * TraitSystem.GetGriefMultiplier(hero);
            int finalGrief = Mathf.RoundToInt(grief);
            if (finalGrief > 0)
            {
                ApplyMoraleDelta(hero, -finalGrief, "Grief");
                AddHistory(hero, $"Grieved the loss of {fallen.Count} comrade(s) (morale -{finalGrief}).");
            }
        }
    }

    private void HandleHeroDied(HeroInstance hero)
    {
        // Ensure status is correct on death (GameManager.KillHero handles permadeath;
        // this just guarantees the status event fires for UI)
        SetStatus(hero, HeroStatus.Dead);
    }

    private static void AddHistory(HeroInstance hero, string entry)
    {
        hero?.AddHistory($"[Morale] {entry}");
    }
}
