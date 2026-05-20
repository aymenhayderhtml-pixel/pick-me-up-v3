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
    public string heroName;
    public HeroClass heroClass;
    public int starRating;          // 1–5
    public Sprite portrait;         // card art
    public string summonQuote;      // summon quote
    public string possessedSkillId; // maps to SkillData ScriptableObject


    [Header("Gacha")]
    public float dropWeight;

    [Header("Stats")]
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
}
