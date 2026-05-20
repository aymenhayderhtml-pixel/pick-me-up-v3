using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lightweight runtime registry for SkillData templates.
/// Keeps skill lookup centralized and future JSON-import friendly.
/// </summary>
public sealed class SkillDatabase
{
    readonly List<SkillData> _allSkills = new List<SkillData>();
    readonly Dictionary<string, SkillData> _byId = new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, SkillData> _byAssetName = new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SkillData> AllSkills => _allSkills;
    public int Count => _allSkills.Count;

    public static SkillDatabase Build(IEnumerable<SkillData> skills)
    {
        var database = new SkillDatabase();
        database.Rebuild(skills);
        return database;
    }

    public void Clear()
    {
        _allSkills.Clear();
        _byId.Clear();
        _byAssetName.Clear();
    }

    public void Rebuild(IEnumerable<SkillData> skills)
    {
        Clear();
        if (skills == null) return;

        foreach (var skill in skills)
        {
            Register(skill);
        }
    }

    public bool Register(SkillData skill)
    {
        if (skill == null) return false;

        string skillId = !string.IsNullOrWhiteSpace(skill.skillId) ? skill.skillId.Trim() : skill.name;
        if (string.IsNullOrWhiteSpace(skillId)) return false;

        if (_byId.ContainsKey(skillId))
        {
            Debug.LogWarning($"[SkillDatabase] Duplicate skill ID '{skillId}' ignored.");
            return false;
        }

        _allSkills.Add(skill);
        _byId[skillId] = skill;

        if (!string.IsNullOrWhiteSpace(skill.name))
        {
            _byAssetName[skill.name] = skill;
        }

        return true;
    }

    public bool TryGet(string skillId, out SkillData skill)
    {
        skill = null;
        if (string.IsNullOrWhiteSpace(skillId)) return false;

        if (_byId.TryGetValue(skillId, out skill))
            return true;

        return _byAssetName.TryGetValue(skillId, out skill);
    }

    public SkillData Get(string skillId)
    {
        return TryGet(skillId, out var skill) ? skill : null;
    }

    public SkillData[] GetAll()
    {
        return _allSkills.Where(s => s != null).ToArray();
    }
}
