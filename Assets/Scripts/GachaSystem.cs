using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic — no MonoBehaviour, no UI.
/// Called by the Summon screen UI, returns HeroInstance list.
/// </summary>
public static class GachaSystem
{
    // ── Pull costs & Pity limits ──────────────────────────
    public const int COST_GEMS_1X   = 300;
    public const int COST_GEMS_10X  = 3000;
    public const int PITY_THRESHOLD = 50; // Hard pity locked at 50

    // ─────────────────────────────────────────────────────
    // PUBLIC ENTRY POINTS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Attempt a 1x pull. Returns null if not enough gems.
    /// </summary>
    public static HeroInstance Pull1x(GameManager gm)
    {
        if (!gm.SpendGems(COST_GEMS_1X)) return null;

        var result = DoSinglePull(gm);
        gm.State.totalSummons++;
        gm.SaveGame();
        return result;
    }

    /// <summary>
    /// Attempt a 10x pull. Returns empty list if not enough gems.
    /// </summary>
    public static List<HeroInstance> Pull10x(GameManager gm)
    {
        if (!gm.SpendGems(COST_GEMS_10X)) return new List<HeroInstance>();

        var results = new List<HeroInstance>();
        for (int i = 0; i < 10; i++)
            results.Add(DoSinglePull(gm));

        gm.State.totalSummons += 10;
        gm.SaveGame();
        return results;
    }

    // ─────────────────────────────────────────────────────
    // CORE ROLL
    // ─────────────────────────────────────────────────────
    static HeroInstance DoSinglePull(GameManager gm)
    {
        gm.State.pityCounter++;

        int stars = RollStars(gm.State.pityCounter);

        // Pity triggered: force 4★ (5★ is promotion-only!)
        if (gm.State.pityCounter >= PITY_THRESHOLD)
        {
            stars = 4;
        }

        // Reset pity counter upon rolling a natural or pity 4★ hero
        if (stars == 4)
        {
            gm.State.pityCounter = 0;
        }

        HeroData data = PickHeroFromPool(gm, stars);
        if (data == null)
        {
            Debug.LogError($"[GachaSystem] No HeroData found for {stars}★ — check HeroDatabase in GameManager.");
            return null;
        }

        var instance = new HeroInstance(data);
        gm.AddHeroToRoster(instance);
        return instance;
    }

    // ─────────────────────────────────────────────────────
    // STAR ROLL
    // ─────────────────────────────────────────────────────
    static int RollStars(int currentPity)
    {
        // Category 4 Soft Pity: begins at 35 pulls, increasing 4★ rate from 3% progressively to ~30% by pull 50
        float fourStarRate = 3.0f; // Base 3% rate
        if (currentPity > 35)
        {
            float progress = (float)(currentPity - 35) / (PITY_THRESHOLD - 35);
            fourStarRate = Mathf.Lerp(3.0f, 30.0f, progress);
        }

        float roll = Random.Range(0f, 100f);

        // Roll probabilities (5★ is promotion-only!)
        if (roll < fourStarRate) return 4;
        if (roll < fourStarRate + 10.0f) return 3; // 10% base rate
        if (roll < fourStarRate + 37.0f) return 2; // 27% base rate
        return 1; // 60% base rate
    }

    // ─────────────────────────────────────────────────────
    // HERO PICKER (weighted random within star tier)
    // ─────────────────────────────────────────────────────
    static HeroData PickHeroFromPool(GameManager gm, int stars)
    {
        HeroData[] pool = gm.GetPoolByStars(stars);
        if (pool == null || pool.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var h in pool) totalWeight += h.dropWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var h in pool)
        {
            cumulative += h.dropWeight;
            if (roll <= cumulative) return h;
        }

        // Fallback: return last in pool
        return pool[pool.Length - 1];
    }
}
