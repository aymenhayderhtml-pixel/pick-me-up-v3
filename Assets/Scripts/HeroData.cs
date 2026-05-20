using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BaseStats
{
    public int hp;
    public int maxHp;
    public int atk;
    public int def;
    public int spd;
}

public enum HeroClass
{
    Warrior,
    Knight,
    Mage,
    Priest,
    Rogue,
    Novice,
    Vanguard,
    Scout,
    Acolyte,
    Guardian,
    Gladiator,
    Assassin,
    Ranger,
    Elementalist,
    BloodMage,
    Immortal,
    ShadowReaper,
    HighSage
}

public enum PersonalityTrait
{
    Brave,          // +15% ATK when HP < 30%
    Cautious,       // +10% DEF, -5% ATK
    Loyal,          // morale drops slower
    Rebellious,     // may disobey if morale < 30
    Compassionate,  // morale drops extra on ally death
    Stoic           // morale unaffected by ally death
}

[CreateAssetMenu(fileName = "SO_Hero_New", menuName = "PickMeUp/HeroData")]
public class HeroData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable ID used by save data, databases, and future JSON imports.")]
    [SerializeField] private string heroId;
    public string heroName;
    public HeroClass heroClass;
    public int starRating;          // 1–5
    public Sprite portrait;         // card art
    public string summonQuote;      // summon quote
    public string possessedSkillId; // maps to SkillData ScriptableObject

    [Header("Primary Stats")]
    [Tooltip("Primary hero stat used by the game from now on.")]
    public int strength;
    [Tooltip("Primary hero stat used by the game from now on.")]
    public int intelligence;
    [Tooltip("Primary hero stat used by the game from now on.")]
    public int hp;
    [Tooltip("Primary hero stat used by the game from now on.")]
    public int agility;


    [Header("Gacha")]
    public float dropWeight;

    [Header("Stats")]
    [Tooltip("Legacy combat stat fallback. Kept for migration compatibility.")]
    public BaseStats baseStats;

    [Header("Traits")]
    public List<PersonalityTrait> possibleTraits = new List<PersonalityTrait>();

    [Header("Scaling (per level)")]
    public float hpPerLevel = 5f;
    public float atkPerLevel = 1.5f;
    public float defPerLevel = 0.8f;
    public float spdPerLevel = 0.2f;

    [Header("Crit Properties")]
    public float baseCritChance = 0.05f;
    public float baseCritMult = 1.5f;

    public string HeroId => string.IsNullOrWhiteSpace(heroId) ? name : heroId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(heroName) ? HeroId : heroName;
    public int Strength => strength > 0 ? strength : baseStats != null ? baseStats.atk : 0;
    public int Intelligence => intelligence > 0 ? intelligence : baseStats != null ? baseStats.def : 0;
    public int Vitality => hp > 0 ? hp : baseStats != null ? baseStats.hp : 0;
    public int Agility => agility > 0 ? agility : baseStats != null ? baseStats.spd : 0;

    public void SetHeroId(string value)
    {
        heroId = value;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(heroId))
        {
            heroId = name;
        }

        if (baseStats == null)
        {
            baseStats = new BaseStats();
        }

        if (strength <= 0 && baseStats.atk > 0) strength = baseStats.atk;
        if (intelligence <= 0 && baseStats.def > 0) intelligence = baseStats.def;
        if (hp <= 0 && baseStats.hp > 0) hp = baseStats.hp;
        if (agility <= 0 && baseStats.spd > 0) agility = baseStats.spd;
    }
}
