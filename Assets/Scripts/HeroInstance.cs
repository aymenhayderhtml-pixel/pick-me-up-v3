using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime data for ONE specific summoned hero.
/// Serialized to JSON for save/load.
/// HeroData = template (ScriptableObject, read-only)
/// HeroInstance = this individual hero's life story
/// </summary>
[Serializable]
public class HeroInstance
{
    // ── Identity ──────────────────────────────────────────
    public string instanceId;           // GUID, unique per hero
    public string heroDataId;           // matches HeroData.name (SO asset name)
    public string heroName;             // copy from HeroData at summon time
    public int starRating;
    public int synthesisPromotions = 0; // tracks number of synthesis promotions applied
    public List<SkillInstance> skills = new List<SkillInstance>(); // manhwa-accurate skill slots

    // SPEC ADDITION
    public HeroClass heroClass;
    public int classRank;

    // ── Progression ───────────────────────────────────────
    public int level;                   // 1 to 50
    public int currentXP;
    public int xpToNextLevel;

    // ── Live Stats (calculated from base + level) ─────────
    public int maxHP;
    public int currentHP;
    public int atk;
    public int def;
    public int spd;
    public float critChance;
    public float critMult;

    // SPEC ADDITION STR, INT, AGI
    public int currentSTR;
    public int maxSTR;
    public int currentINT;
    public int maxINT;
    public int currentAGI;
    public int maxAGI;

    // ── Personality & Morale ──────────────────────────────
    public PersonalityTrait trait;
    public int morale;                  // 0-100
    public HeroStatus status;           // Active, Fatigued, Wounded, Dead
    public int fatigue;                 // SPEC ADDITION: 0-100

    // ── History (attachment system) ───────────────────────
    public int missionsCompleted;
    public int floorsCleared;
    public int kills;
    public int nearDeathMoments;        // survived with < 10% HP
    public string earnedTitle;          // "Kael the Enduring" after 10 missions
    public List<string> battleLog;      // last 5 notable events

    // ── State ─────────────────────────────────────────────
    public bool isDeployed;             // currently in a squad
    public bool isNew;                  // just summoned, show NEW badge
    public string dateObtained;         // ISO string
    public string dateDied;             // set on permadeath
    public string facilityAssignmentDate = ""; // ISO timestamp for passive rest/training clock sync

    // SPEC ADDITIONS
    public string possessedSkillId;
    public List<TraitData> traits = new List<TraitData>();
    public List<TraitData> traumas = new List<TraitData>();
    public string causeOfDeath;
    public int deathFloor;
    public int deathDay;

    // ── Equipment (Phase 5 Specs) ──────────────────────────
    public string equippedWeaponId = "";
    public string equippedArmorId = "";
    public string equippedRingId = "";

    // ─────────────────────────────────────────────────────
    // Constructor — called by GachaSystem when a hero is summoned
    // ─────────────────────────────────────────────────────
    public HeroInstance(HeroData data)
    {
        instanceId = Guid.NewGuid().ToString();
        heroDataId = data != null ? data.HeroId : "MockData";
        heroName = data != null ? data.DisplayName : "Unknown Hero";
        starRating = data != null ? data.starRating : 1;
        level = 1;
        currentXP = 0;
        xpToNextLevel = CalculateXPThreshold(1);
        morale = 80;
        status = HeroStatus.Active;
        isNew = true;
        dateObtained = DateTime.UtcNow.ToString("o");
        battleLog = new List<string>();
        kills = 0;
        missionsCompleted = 0;
        floorsCleared = 0;
        nearDeathMoments = 0;
        earnedTitle = "";

        // Spec fields initialization
        heroClass = data != null ? data.heroClass : HeroClass.Novice;
        classRank = HeroUtils.GetClassTier(heroClass);
        fatigue = UnityEngine.Random.Range(5, 45); // start active/fresh
        possessedSkillId = data != null ? data.possessedSkillId : "";

        // Initialize lists
        traits = new List<TraitData>();
        traumas = new List<TraitData>();

        // Pick a random trait from the data's possible traits
        if (data != null && data.possibleTraits != null && data.possibleTraits.Count > 0)
            trait = data.possibleTraits[UnityEngine.Random.Range(0, data.possibleTraits.Count)];
        else
            trait = PersonalityTrait.Stoic;

        // Pick Me Up Manhwa Style: 1-star heroes start with NO innate skills.
        // 2-star start with 1 basic skill, 3-star and above start with 2 skills.
        skills = new List<SkillInstance>();
        if (data != null && starRating >= 2)
        {
            AddDefaultClassSkill(data.heroClass, 1);
        }
        if (data != null && starRating >= 3)
        {
            AddDefaultClassSkill(data.heroClass, 2);
        }

        RecalculateStats(data);
    }

    // ─────────────────────────────────────────────────────
    // Stat calculation: base + (level-1) * perLevel scalar
    // ─────────────────────────────────────────────────────
    public void RecalculateStats(HeroData data)
    {
        int lvl = level - 1;
        int bHP = data != null && data.baseStats != null ? data.baseStats.hp : 50;
        int bATK = data != null && data.baseStats != null ? data.baseStats.atk : 10;
        int bDEF = data != null && data.baseStats != null ? data.baseStats.def : 5;
        int bSPD = data != null && data.baseStats != null ? data.baseStats.spd : 10;

        maxHP = Mathf.RoundToInt(bHP + lvl * (data != null ? data.hpPerLevel : 5f));
        atk = Mathf.RoundToInt(bATK + lvl * (data != null ? data.atkPerLevel : 1.5f));
        def = Mathf.RoundToInt(bDEF + lvl * (data != null ? data.defPerLevel : 0.8f));
        spd = Mathf.RoundToInt(bSPD + lvl * (data != null ? data.spdPerLevel : 0.2f));

        // Apply 15% synthesis promotion stat boost per promotion
        if (synthesisPromotions > 0)
        {
            float mult = Mathf.Pow(1.15f, synthesisPromotions);
            maxHP = Mathf.RoundToInt(maxHP * mult);
            atk = Mathf.RoundToInt(atk * mult);
            def = Mathf.RoundToInt(def * mult);
            spd = Mathf.RoundToInt(spd * mult);
        }

        currentHP = maxHP;
        critChance = data != null ? data.baseCritChance : 0.05f;
        critMult = data != null ? data.baseCritMult : 1.5f;

        // Initialize and scale STR, INT, AGI based on class and starRating
        int bSTR = 10;
        int bINT = 10;
        int bAGI = 10;

        switch (heroClass)
        {
            case HeroClass.Vanguard:
            case HeroClass.Guardian:
            case HeroClass.Gladiator:
            case HeroClass.Immortal:
            case HeroClass.Warrior:
            case HeroClass.Knight:
                bSTR = 13 + starRating;
                bINT = 8 + level / 3;
                bAGI = 10 + level / 2;
                break;
            case HeroClass.Scout:
            case HeroClass.Assassin:
            case HeroClass.Ranger:
            case HeroClass.ShadowReaper:
            case HeroClass.Rogue:
                bSTR = 9 + level / 2;
                bINT = 8 + level / 3;
                bAGI = 13 + starRating;
                break;
            case HeroClass.Acolyte:
            case HeroClass.Elementalist:
            case HeroClass.BloodMage:
            case HeroClass.HighSage:
            case HeroClass.Mage:
            case HeroClass.Priest:
                bSTR = 8 + level / 3;
                bINT = 13 + starRating;
                bAGI = 9 + level / 2;
                break;
            default:
                bSTR = 10 + level / 2;
                bINT = 10 + level / 2;
                bAGI = 10 + level / 2;
                break;
        }

        maxSTR = bSTR + lvl;
        maxINT = bINT + lvl;
        maxAGI = bAGI + lvl;

        // Apply synthesis promotion boost to sub-stats too
        if (synthesisPromotions > 0)
        {
            float mult = Mathf.Pow(1.15f, synthesisPromotions);
            maxSTR = Mathf.RoundToInt(maxSTR * mult);
            maxINT = Mathf.RoundToInt(maxINT * mult);
            maxAGI = Mathf.RoundToInt(maxAGI * mult);
        }

        currentSTR = maxSTR;
        currentINT = maxINT;
        currentAGI = maxAGI;

        // Trait modifiers
        ApplyTraitModifiers();
    }

    public int GetBaseMax(string fieldName)
    {
        switch (fieldName)
        {
            case "maxHP":  return maxHP;
            case "maxSTR": return maxSTR;
            case "maxINT": return maxINT;
            case "maxAGI": return maxAGI;
            case "currentSTR": return maxSTR;
            case "currentINT": return maxINT;
            case "currentAGI": return maxAGI;
            default: return 10;
        }
    }


    void ApplyTraitModifiers()
    {
        switch (trait)
        {
            case PersonalityTrait.Cautious:
                def = Mathf.RoundToInt(def * 1.10f);
                atk = Mathf.RoundToInt(atk * 0.95f);
                break;
                // Brave is handled in CombatManager at runtime (conditional on HP)
                // Loyal/Rebellious/Compassionate/Stoic handled in MoraleSystem

        }
    }

    public int GetMaxLevelCap()
    {
        return starRating switch
        {
            1 => 15,
            2 => 25,
            3 => 35,
            4 => 50,
            5 => 65,
            6 => 80,
            _ => 15
        };
    }

    // ─────────────────────────────────────────────────────
    // XP & levelling
    // ─────────────────────────────────────────────────────
    /// <summary>Returns true if levelled up.</summary>
    public bool AddXP(int amount, HeroData data)
    {
        int maxCap = GetMaxLevelCap();
        if (level >= maxCap)
        {
            currentXP = 0; // Cap off accumulated XP
            return false;
        }

        currentXP += amount;
        bool leveledUp = false;

        while (currentXP >= xpToNextLevel && level < maxCap)
        {
            currentXP -= xpToNextLevel;
            level++;
            xpToNextLevel = CalculateXPThreshold(level);
            leveledUp = true;
        }

        if (leveledUp)
        {
            RecalculateStats(data);
        }

        return leveledUp;
    }

    static int CalculateXPThreshold(int level)
    {
        // Simple quadratic curve: 10, 22, 38, 58 ...
        return Mathf.RoundToInt(10 * level + 2 * level * level);
    }
    // ─────────────────────────────────────────────────────
    // Morale helpers
    // ─────────────────────────────────────────────────────
    public void ModifyMorale(int delta)
    {
        // Loyal trait: dampen negative morale changes
        if (delta < 0 && trait == PersonalityTrait.Loyal)
            delta = Mathf.RoundToInt(delta * 0.6f);

        morale = Mathf.Clamp(morale + delta, 0, 100);
    }

    public bool IsRebelling()
    {
        return trait == PersonalityTrait.Rebellious && morale < 30;
    }

    // ─────────────────────────────────────────────────────
    // Death
    // ─────────────────────────────────────────────────────
    public void Die()
    {
        status = HeroStatus.Dead;
        currentHP = 0;
        dateDied = DateTime.UtcNow.ToString("o");
        isDeployed = false;
    }

    // ─────────────────────────────────────────────────────
    // Title system
    // ─────────────────────────────────────────────────────
    public void CheckAndAssignTitle()
    {
        if (missionsCompleted >= 10 && string.IsNullOrEmpty(earnedTitle))
        {
            earnedTitle = heroName + " the Enduring";
        }
        else if (floorsCleared >= 30 && !earnedTitle.Contains("Veteran"))
        {
            earnedTitle = heroName + " the Veteran";
        }
    }

    public bool HasTrait(PersonalityTrait trait) => this.trait == trait;

    public void AddHistory(string entry)
    {
        if (battleLog == null) battleLog = new List<string>();
        battleLog.Add(entry);
        if (battleLog.Count > 5) battleLog.RemoveAt(0);
    }

    public List<string> GetHistory()
    {
        if (battleLog == null) battleLog = new List<string>();
        return battleLog;
    }

    // Serialization field
    public bool essenceExtracted = false;

    // UI properties
    public bool EssenceExtracted
    {
        get => essenceExtracted;
        set => essenceExtracted = value;
    }

    public HeroStatus Status
    {
        get => status;
        set => status = value;
    }

    public HeroData data => GameManager.Instance != null ? GameManager.Instance.GetHeroData(heroDataId) : null;
    public HeroData Template => data;
    public string TemplateId => heroDataId;
    public string HeroName => heroName;
    public string Title => earnedTitle;
    public string Id => instanceId;

    // ─────────────────────────────────────────────────────
    // Display helpers
    // ─────────────────────────────────────────────────────
    public string DisplayName => string.IsNullOrEmpty(earnedTitle) ? heroName : earnedTitle;

    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;

    public int Power => atk + def + (maxHP / 10) + spd;

    // ── Pick Me Up Skill Helpers ──────────────────────────
    public List<SkillInstance> GetUnlockedSkills()
    {
        if (skills == null) skills = new List<SkillInstance>();
        return skills.Where(s => s.isUnlocked).ToList();
    }

    private void AddDefaultClassSkill(HeroClass heroClass, int slotIndex)
    {
        string skillId = "";
        if (heroClass == HeroClass.Warrior)
        {
            skillId = slotIndex == 1 ? "slash" : "whirlwind";
        }
        else if (heroClass == HeroClass.Knight)
        {
            skillId = slotIndex == 1 ? "shield_wall" : "iron_fortress";
        }
        else if (heroClass == HeroClass.Mage)
        {
            skillId = slotIndex == 1 ? "meteor_strike" : "rejuvenation";
        }
        else if (heroClass == HeroClass.Priest)
        {
            skillId = slotIndex == 1 ? "holy_heal" : "rejuvenation";
        }
        else if (heroClass == HeroClass.Rogue)
        {
            skillId = slotIndex == 1 ? "shadow_strike" : "viper_strike";
        }

        if (!string.IsNullOrEmpty(skillId))
        {
            skills.Add(new SkillInstance(skillId));
        }
    }
}

public enum HeroStatus
{
    Active,     // ready to deploy
    Fatigued,   // needs rest (morale < 20)
    Wounded,    // HP not full, needs healing
    Deployed,   // currently in a mission
    Dead,       // in Memorial Hall
    Resting,    // Tavern Passive Rest
    Training    // Training Ground Passive XP
}
