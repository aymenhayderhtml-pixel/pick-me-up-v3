using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passed into combat trait hooks so traits can read battle context
/// without coupling to CombatManager internals.
/// </summary>
public class CombatTraitContext
{
    public List<CombatUnit> AllUnits;       // full unit list this battle
    public List<CombatUnit> FallenAllies;   // allies who died before this action
    public bool IsPlayerUnit;               // attacker/defender is player-controlled

    public int AliveAlliesCount()
    {
        int count = 0;
        foreach (var u in AllUnits)
            if (u.IsAlive && u.IsPlayerUnit == IsPlayerUnit) count++;
        return count;
    }

    public bool AnyAllyHasFallen() => FallenAllies != null && FallenAllies.Count > 0;
}

public static class TraitSystem
{
    // ─── Combat: Offensive ────────────────────────────────────────────────────

    /// <summary>
    /// Modifies attacker's ATK multiplier based on their traits + battle context.
    /// Call before damage calculation in CombatManager.
    /// </summary>
    public static void ApplyOffensiveTraits(
        CombatUnit attacker,
        CombatUnit defender,
        CombatTraitContext context,
        ref float atkMult)
    {
        if (attacker?.SourceHero == null) return;

        foreach (var trait in attacker.SourceHero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Brave:
                    // +15% ATK when own HP < 30%
                    if (attacker.HpPercent < 0.30f)
                        atkMult *= 1.15f;
                    break;

                case PersonalityTrait.Loyal:
                    // +5% ATK per fallen ally (stacks, caps at +25%)
                    if (context.AnyAllyHasFallen())
                    {
                        float bonus = Mathf.Min(context.FallenAllies.Count * 0.05f, 0.25f);
                        atkMult *= (1f + bonus);
                    }
                    break;

                case PersonalityTrait.Rebellious:
                    // +10% ATK when fighting alone (last ally standing)
                    if (context.AliveAlliesCount() <= 1)
                        atkMult *= 1.10f;
                    break;

                case PersonalityTrait.Cautious:
                    // -8% ATK (Cautious heroes hold back offensively)
                    atkMult *= 0.92f;
                    break;

                case PersonalityTrait.Stoic:
                    // No offensive modifier — Stoic is purely defensive/morale
                    break;

                case PersonalityTrait.Compassionate:
                    // +8% ATK when attacking an enemy that killed an ally this battle
                    if (context.AnyAllyHasFallen())
                        atkMult *= 1.08f;
                    break;
            }
        }
    }

    // ─── Combat: Defensive ────────────────────────────────────────────────────

    /// <summary>
    /// Modifies defender's DEF multiplier based on their traits + battle context.
    /// Call before damage calculation in CombatManager.
    /// </summary>
    public static void ApplyDefensiveTraits(
        CombatUnit defender,
        CombatTraitContext context,
        ref float defMult)
    {
        if (defender?.SourceHero == null) return;

        foreach (var trait in defender.SourceHero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Cautious:
                    // +10% DEF when HP < 50%
                    if (defender.HpPercent < 0.50f)
                        defMult *= 1.10f;
                    break;

                case PersonalityTrait.Stoic:
                    // Flat +8% DEF always — absorbs punishment without flinching
                    defMult *= 1.08f;
                    break;

                case PersonalityTrait.Brave:
                    // No defensive modifier — Brave heroes don't focus on defense
                    break;

                case PersonalityTrait.Loyal:
                    // +5% DEF when at least one ally is still alive
                    if (context.AliveAlliesCount() > 1)
                        defMult *= 1.05f;
                    break;

                case PersonalityTrait.Compassionate:
                    // No defensive modifier
                    break;

                case PersonalityTrait.Rebellious:
                    // -5% DEF (reckless — takes more hits)
                    defMult *= 0.95f;
                    break;
            }
        }
    }

    // ─── Combat: Turn Events ──────────────────────────────────────────────────

    /// <summary>
    /// Called when a player-side ally dies mid-battle.
    /// Returns a flat HP restore amount for any surviving unit that should react.
    /// CombatManager iterates survivors and calls this for each.
    /// </summary>
    public static int GetDeathReactionHpRestore(CombatUnit survivor, CombatUnit fallen)
    {
        if (survivor?.SourceHero == null) return 0;

        int restore = 0;
        foreach (var trait in survivor.SourceHero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Compassionate:
                    // Grief briefly destabilizes — small HP loss actually (negative restore)
                    restore -= Mathf.RoundToInt(survivor.MaxHp * 0.03f);
                    break;

                case PersonalityTrait.Brave:
                    // Rage: brief adrenaline, no HP effect but atkMult handled offensively
                    break;

                case PersonalityTrait.Loyal:
                    // Resolve: small HP restoration from determination
                    restore += Mathf.RoundToInt(survivor.MaxHp * 0.05f);
                    break;
            }
        }
        return restore;
    }

    // ─── Morale ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplier applied to passive morale decay for this hero.
    /// MoraleSystem.TickPassiveDecay() multiplies base decay by this value.
    /// </summary>
    public static float GetMoraleDecayMultiplier(HeroInstance hero)
    {
        if (hero?.data?.possibleTraits == null) return 1f;

        float mult = 1f;
        foreach (var trait in hero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Stoic:        mult *= 0.5f;  break; // decays half as fast
                case PersonalityTrait.Cautious:     mult *= 0.75f; break; // cautious, stable
                case PersonalityTrait.Rebellious:   mult *= 1.5f;  break; // volatile
                case PersonalityTrait.Compassionate:mult *= 1.25f; break; // feels everything more
            }
        }
        return Mathf.Max(0.1f, mult); // never fully stop decay
    }

    /// <summary>
    /// Multiplier on grief received when an ally dies.
    /// MoraleSystem applies: grief = base * GetGriefMultiplier(hero)
    /// </summary>
    public static float GetGriefMultiplier(HeroInstance hero)
    {
        if (hero?.data?.possibleTraits == null) return 1f;

        float mult = 1f;
        foreach (var trait in hero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Compassionate: mult *= 2.0f;  break;
                case PersonalityTrait.Loyal:         mult *= 1.5f;  break;
                case PersonalityTrait.Stoic:         mult *= 0.4f;  break;
                case PersonalityTrait.Rebellious:    mult *= 0.75f; break; // doesn't dwell on loss
            }
        }
        return mult;
    }

    /// <summary>
    /// Net morale delta modifier for post-battle resolution.
    /// MoraleSystem multiplies its computed delta by this before applying.
    /// Positive values amplify gains; values < 1 dampen losses.
    /// </summary>
    public static float GetBattleMoraleModifier(HeroInstance hero, BattleResult result)
    {
        if (hero?.data?.possibleTraits == null) return 1f;

        float mult = 1f;
        foreach (var trait in hero.data.possibleTraits)
        {
            switch (trait)
            {
                case PersonalityTrait.Brave:
                    // Wins feel bigger, losses feel smaller
                    mult *= result.victory ? 1.3f : 0.8f;
                    break;

                case PersonalityTrait.Stoic:
                    // Neither highs nor lows hit hard
                    mult *= 0.6f;
                    break;

                case PersonalityTrait.Compassionate:
                    // Wins feel great, losses feel catastrophic
                    mult *= result.victory ? 1.2f : 1.4f;
                    break;

                case PersonalityTrait.Cautious:
                    // Steady — slight dampening both ways
                    mult *= 0.85f;
                    break;
            }
        }
        return mult;
    }

    // ─── Deployment Gate ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns null if hero can deploy, or a user-facing reason string if blocked.
    /// SquadFormationUI calls this instead of inline morale checks.
    /// Replaces MoraleSystem.CanDeploy() — retire that method and call this instead.
    /// </summary>
    public static string GetDeployBlockReason(HeroInstance hero)
    {
        if (hero == null)                          return "Invalid hero.";
        if (hero.status == HeroStatus.Dead)        return "This hero has fallen.";
        if (hero.status == HeroStatus.Deployed)    return "Already deployed.";
        if (hero.status == HeroStatus.Fatigued)    return $"{hero.heroName} is too exhausted to fight.";

        if (hero.HasTrait(PersonalityTrait.Rebellious) && hero.morale < 30)
            return $"{hero.heroName} refuses your orders.";

        return null; // null = allowed
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Human-readable description for trait tooltip in RosterUI / SynthesisUI.
    /// </summary>
    public static string GetTraitDescription(PersonalityTrait trait) => trait switch
    {
        PersonalityTrait.Brave        => "Gains ATK at low HP. Morale swings harder on battle outcomes.",
        PersonalityTrait.Cautious     => "Gains DEF at low HP. Slower morale decay. Slightly reduced offense.",
        PersonalityTrait.Loyal        => "Gains ATK and DEF while allies live. Grieves deeply for the fallen.",
        PersonalityTrait.Rebellious   => "Strongest alone. Volatile morale. Refuses orders below 30 morale.",
        PersonalityTrait.Compassionate=> "Empowered by vengeance. Devastated by loss. Unstable under casualties.",
        PersonalityTrait.Stoic        => "Constant DEF bonus. Barely affected by morale swings or grief.",
        _ => string.Empty
    };
}
