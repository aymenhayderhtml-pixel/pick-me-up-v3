using System;
using UnityEngine;

[Serializable]
public class SkillInstance
{
    public string skillId;
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public bool isUnlocked = true;

    [NonSerialized]
    public int cooldownTurnsRemaining = 0;

    public SkillData Data => GetSkillDataRef();

    private SkillData _dataCache;

    private SkillData GetSkillDataRef()
    {
        if (_dataCache != null) return _dataCache;

        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        // Prefer the central registry if GameManager is available.
        if (GameManager.Instance != null)
        {
            var database = GameManager.Instance.SkillDatabase;
            if (database != null)
            {
                var byId = database.Get(skillId);
                if (byId != null)
                {
                    _dataCache = byId;
                    return _dataCache;
                }
            }
        }

        // Fallback: scan all Resources-loaded SkillData assets.
        // This keeps the game functional even before a dedicated Skills folder exists.
        var allSkills = Resources.LoadAll<SkillData>(string.Empty);
        foreach (var s in allSkills)
        {
            if (s == null) continue;
            if (string.Equals(s.skillId, skillId, StringComparison.OrdinalIgnoreCase))
            {
                _dataCache = s;
                return _dataCache;
            }
        }

        // Final fallback: try the conventional Resources/Skills path if assets are added later.
        _dataCache = Resources.Load<SkillData>($"Skills/{skillId}");
        return _dataCache;
    }

    public SkillInstance(string id, bool unlocked = true)
    {
        skillId = id;
        level = 1;
        currentExp = 0;
        expToNextLevel = 100;
        isUnlocked = unlocked;
        cooldownTurnsRemaining = 0;
    }

    /// <summary>
    /// Refines the skill by adding usage experience.
    /// Returns true if the skill levels up and gains more potency!
    /// </summary>
    public bool GainExp(int amount)
    {
        if (!isUnlocked) return false;
        currentExp += amount;
        if (currentExp >= expToNextLevel && level < 10)
        {
            currentExp -= expToNextLevel;
            level++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.5f);
            return true;
        }
        return false;
    }
}
