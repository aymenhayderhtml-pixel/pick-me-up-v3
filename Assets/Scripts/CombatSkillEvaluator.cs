using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CombatSkillEvaluator
{
    /// <summary>
    /// Governs autonomous skill decision-making for a unit during battle.
    /// Returns the selected skill to activate, or null if they perform a basic attack.
    /// </summary>
    public static SkillInstance EvaluateActiveSkill(CombatUnit unit, List<CombatUnit> allies, List<CombatUnit> enemies, int currentTurn)
    {
        if (unit.SourceHero == null) return null;

        // Get all unlocked, ready active skills
        var readySkills = unit.SourceHero.GetUnlockedSkills()
            .Where(s => s.Data != null && s.Data.type == SkillType.ActiveCombat && s.cooldownTurnsRemaining <= 0)
            .ToList();

        if (readySkills.Count == 0) return null;

        // Autonomous Hero Intellect Decision-Making:
        foreach (var skill in readySkills)
        {
            var data = skill.Data;

            // Class and role specific priorities
            switch (data.skillId)
            {
                case "holy_heal":
                case "rejuvenation":
                    // Priests: Only cast healing skills if an ally is below 60% HP
                    bool needsHeal = allies.Any(a => a.IsAlive && a.HpPercent < 0.6f);
                    if (needsHeal) return skill;
                    break;

                case "shield_wall":
                case "iron_fortress":
                    // Knights: Cast shield skills if any squishy ally (Mage/Priest/Rogue) is critically low (< 40% HP)
                    bool allyInDanger = allies.Any(a => a.IsAlive && a.HpPercent < 0.4f && a.SourceHero != null && 
                        (a.SourceHero.data.heroClass == HeroClass.Priest || a.SourceHero.data.heroClass == HeroClass.Mage || a.SourceHero.data.heroClass == HeroClass.Rogue));
                    if (allyInDanger || unit.HpPercent < 0.5f) return skill;
                    break;

                case "shadow_strike":
                case "viper_strike":
                    // Rogues: Execute target instantly if an enemy is in execution range (< 30% HP)
                    bool canExecute = enemies.Any(e => e.IsAlive && e.HpPercent < 0.3f);
                    if (canExecute) return skill;
                    break;

                case "whirlwind":
                case "meteor_strike":
                    // Mages/Warriors: Unleash area spells if there are multiple healthy enemies standing
                    int healthyEnemiesCount = enemies.Count(e => e.IsAlive && e.HpPercent > 0.5f);
                    if (healthyEnemiesCount >= 2) return skill;
                    break;

                default:
                    // Default behavior: Cast standard attack skills immediately if ready
                    if (data.targetType == SkillTargetType.SingleEnemy) return skill;
                    break;
            }
        }

        // Fallback to the first available skill with standard targeting
        return readySkills.FirstOrDefault();
    }

    /// <summary>
    /// Processes reaction/achievement triggers when events happen in battle.
    /// Highly thematic to the Pick Me Up manhwa!
    /// </summary>
    public static void EvaluateReactionSkills(CombatUnit unit, SkillType triggerType, CombatUnit targetUnit, string eventMessage)
    {
        if (unit.SourceHero == null) return;

        var reactionSkills = unit.SourceHero.GetUnlockedSkills()
            .Where(s => s.Data != null && s.Data.type == SkillType.Reaction)
            .ToList();

        foreach (var skill in reactionSkills)
        {
            var data = skill.Data;

            // Highly thematic reactive skills:
            if (data.skillId == "pain_tolerance" && unit.HpPercent < 0.25f)
            {
                // Pain Tolerance: Hero gains defense and ignores status impact at low HP
                unit.Def = Mathf.RoundToInt(unit.Def * 1.3f);
                Debug.Log($"[Manhwa Skill] {unit.unitName}'s Pain Tolerance active! DEF increased by 30%.");
            }
            else if (data.skillId == "vengeful_strike" && triggerType == SkillType.Reaction && eventMessage == "ally_died")
            {
                // Vengeful Strike: Critical rate reaches 100% for the next turn when a comrade falls
                unit.critChance = 1.0f;
                Debug.Log($"[Manhwa Skill] {unit.unitName} is consumed by rage over their fallen comrade! Crit chance boosted to 100%.");
            }
        }
    }
}
