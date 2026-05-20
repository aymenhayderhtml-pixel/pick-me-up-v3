using UnityEngine;

public enum SkillType
{
    ActiveCombat,   // Active technique triggered under combat conditions (e.g., Slash, Heal)
    PassiveBuff,    // Flat stat boost or constant combat effect (e.g., Hawk's Eye)
    Reaction        // Triggers instantly under specific conditions (e.g., Pain Tolerance on low HP, Vengeance on ally death)
}

public enum SkillTargetType
{
    Self,
    SingleEnemy,
    AllEnemies,
    LowestHpAlly,
    AllAllies
}

[CreateAssetMenu(fileName = "SO_Skill_New", menuName = "PickMeUp/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string skillId;
    public string skillName;
    [TextArea(2, 4)]
    public string description;
    public SkillType type;
    public Sprite icon;

    [Header("Execution Rules")]
    public SkillTargetType targetType;
    public float baseValueMultiplier = 1.0f; // Multiplier applied to ATK (for damage) or DEF (for shields)
    public int baseCooldownTurns = 3;

    [Header("Unlock & Evolve Conditions")]
    public int minStarRatingRequired = 1;
    public bool isHiddenInitially = false;  // Must be unlocked via achievement or training
    public string unlockAchievementQuote;   // The quote shown in game when this skill is unlocked!
}
