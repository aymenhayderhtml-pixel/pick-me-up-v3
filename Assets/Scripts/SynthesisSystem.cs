using System.Collections.Generic;
using UnityEngine;

public enum SynthesisResultCode
{
    Success,
    TargetInvalid,
    AlreadyMaxTier,
    NotEnoughSacrifices,
    TierMismatch,
    SacrificeIsTarget,
    InsufficientEssence
}

public class SynthesisResult
{
    public SynthesisResultCode Code;
    public int OldStar;
    public int NewStar;
    public int EssenceSpent;
    public List<HeroInstance> Consumed = new List<HeroInstance>();
}

/// <summary>
/// Core promotion logic for star tier enhancements.
/// Static class — no scene attachment required.
/// </summary>
public static class SynthesisSystem
{
    // Essence cost indexed by the TARGET's current star rating (1–4; 5★ blocked)
    private static readonly int[] EssenceCost = { 0, 15, 35, 70, 130 }; // [0] unused

    // ─── Validation (call before showing confirm UI) ───────────────────────

    /// <summary>
    /// Returns the first error code, or Success if the operation is legal.
    /// Does NOT mutate any state — safe to call on every selection change.
    /// </summary>
    public static SynthesisResultCode Validate(
        HeroInstance target,
        List<HeroInstance> sacrifices)
    {
        if (target == null
            || target.Status == HeroStatus.Dead
            || target.Status == HeroStatus.Deployed)
            return SynthesisResultCode.TargetInvalid;

        if (target.starRating >= 5)
            return SynthesisResultCode.AlreadyMaxTier;

        if (sacrifices == null || sacrifices.Count < 2)
            return SynthesisResultCode.NotEnoughSacrifices;

        foreach (var s in sacrifices)
        {
            if (s.Id == target.Id)
                return SynthesisResultCode.SacrificeIsTarget;

            if (s.starRating != target.starRating)
                return SynthesisResultCode.TierMismatch;
        }

        int cost = GetEssenceCost(target);
        if (GameManager.Instance.Essence < cost)
            return SynthesisResultCode.InsufficientEssence;

        return SynthesisResultCode.Success;
    }

    // ─── Execution ────────────────────────────────────────────────────────

    /// <summary>
    /// Executes synthesis. Always call Validate() first and gate on Success.
    /// Mutates GameManager state directly; caller must trigger UI refresh.
    /// </summary>
    public static SynthesisResult Execute(
        HeroInstance target,
        List<HeroInstance> sacrifices)
    {
        var result = new SynthesisResult();

        SynthesisResultCode code = Validate(target, sacrifices);
        if (code != SynthesisResultCode.Success)
        {
            result.Code = code;
            return result;
        }

        int cost = GetEssenceCost(target);
        result.OldStar    = target.starRating;
        result.EssenceSpent = cost;

        // ── Spend essence ──
        GameManager.Instance.SpendEssence(cost);

        // ── Consume sacrifices ──
        // Take exactly 2; ignore extras defensively
        int consumed = 0;
        foreach (var sacrifice in sacrifices)
        {
            if (consumed >= 2) break;

            sacrifice.Status = HeroStatus.Dead;
            sacrifice.EssenceExtracted = true; // already consumed; block double-extraction
            sacrifice.AddHistory(
                $"Sacrificed in synthesis for {target.HeroName} ({result.OldStar}★ → {result.OldStar + 1}★).");

            // Add to Memorial without firing OnHeroDied (not a combat death)
            GameManager.Instance.SendToMemorial(sacrifice);

            result.Consumed.Add(sacrifice);
            consumed++;
        }

        // ── Promote target ──
        target.starRating += 1;
        result.NewStar = target.starRating;

        // Stat scaling on promotion: persistent 15% boost to base stats
        ApplyPromotionStatBoost(target);

        // ── Latent Effect: Skill Inheritance (Pick Me Up Manhwa Style!) ──
        string latentEffectMessage = "";
        foreach (var sacrifice in result.Consumed)
        {
            if (sacrifice.skills == null || sacrifice.skills.Count == 0) continue;

            foreach (var sacrificeSkill in sacrifice.skills)
            {
                // Check if target already has this skill
                var targetSkill = target.skills.Find(s => s.skillId == sacrificeSkill.skillId);
                if (targetSkill == null)
                {
                    // 50% chance to inherit new skill
                    if (Random.value < 0.5f)
                    {
                        var newSkill = new SkillInstance(sacrificeSkill.skillId);
                        target.skills.Add(newSkill);
                        latentEffectMessage += $"\n⚡ [Latent Effect] Inherited [{newSkill.Data.skillName}] from {sacrifice.HeroName}!";
                    }
                }
                else
                {
                    // Refine skill if target already has it
                    bool leveledUp = targetSkill.GainExp(50);
                    string levelUpText = leveledUp ? $" (Leveled up to Lv {targetSkill.level}!)" : "";
                    latentEffectMessage += $"\n⚡ [Latent Effect] [{targetSkill.Data.skillName}] refined (+50 Skill EXP) by absorbing {sacrifice.HeroName}!{levelUpText}";
                }
            }
        }

        target.AddHistory(
            $"Promoted to {result.NewStar}★ via synthesis. " +
            $"Cost: {cost} essence. " +
            $"Sacrificed: {string.Join(", ", result.Consumed.ConvertAll(s => s.HeroName))}.{latentEffectMessage}");

        GameManager.Instance.SaveGame();
        GameManager.Instance.FireRosterChanged();

        // Track Quests
        if (GameManager.Instance.QuestSystem != null)
        {
            GameManager.Instance.QuestSystem.Track(QuestType.SacrificeHeroes, 2);
        }

        result.Code = SynthesisResultCode.Success;

#if UNITY_EDITOR
        Debug.Log($"[Synthesis] {target.HeroName}: {result.OldStar}★ → {result.NewStar}★ " +
                  $"| -{cost} essence | consumed: {result.Consumed.Count}");
#endif

        return result;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    public static int GetEssenceCost(HeroInstance target)
    {
        int star = Mathf.Clamp(target.starRating, 1, EssenceCost.Length - 1);
        return EssenceCost[star];
    }

    /// <summary>
    /// Returns all roster heroes that are legally sacrificeable FOR the given target.
    /// Use to populate the sacrifice picker in SynthesisUI.
    /// </summary>
    public static List<HeroInstance> GetValidSacrifices(HeroInstance target)
    {
        var pool = new List<HeroInstance>();
        if (target == null) return pool;

        foreach (var h in GameManager.Instance.GetFullRoster())
        {
            if (h.Id == target.Id) continue;
            if (h.Status == HeroStatus.Dead || h.Status == HeroStatus.Deployed) continue;
            if (h.starRating == target.starRating) pool.Add(h);
        }
        return pool;
    }

    private static void ApplyPromotionStatBoost(HeroInstance hero)
    {
        // Persistent 15% promotion boost to base stats
        hero.synthesisPromotions += 1;
        hero.RecalculateStats(hero.data);
    }

    public static void QuickRelease(HeroInstance fodder)
    {
        if (fodder.Status == HeroStatus.Deployed || fodder.starRating >= 4) return;

        // Calculate returns based on star rating
        int essenceRefund = fodder.starRating * 10;
        int goldRefund = fodder.starRating * 200;

        GameManager.Instance.AddEssence(essenceRefund);
        GameManager.Instance.AddGold(goldRefund);

        // Completely remove from roster
        GameManager.Instance.RemoveHeroFromRoster(fodder.Id);
    }
}
