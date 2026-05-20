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

    public SkillData Data => GameManager.Instance != null ? GetSkillDataRef() : null;

    private SkillData _dataCache;

    private SkillData GetSkillDataRef()
    {
        if (_dataCache != null) return _dataCache;
        // Search in Resources folder
        var loaded = Resources.Load<SkillData>($"Skills/{skillId}");
        if (loaded == null)
        {
            // Fallback: search all loaded skill assets
            var allSkills = Resources.LoadAll<SkillData>("Skills");
            foreach (var s in allSkills)
            {
                if (s.skillId == skillId)
                {
                    _dataCache = s;
                    return _dataCache;
                }
            }
        }
        _dataCache = loaded;
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
