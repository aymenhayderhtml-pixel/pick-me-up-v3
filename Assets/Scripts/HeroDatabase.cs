using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class HeroTemplateJsonRecord
{
    public string heroId;
    public string assetName;
    public string heroName;
    public string heroClass;
    public int starRating = 1;
    public string summonQuote;
    public string possessedSkillId;
    public float dropWeight = 1f;
    public BaseStats baseStats = new BaseStats();
    public List<PersonalityTrait> possibleTraits = new List<PersonalityTrait>();
    public float hpPerLevel = 5f;
    public float atkPerLevel = 1.5f;
    public float defPerLevel = 0.8f;
    public float spdPerLevel = 0.2f;
    public float baseCritChance = 0.05f;
    public float baseCritMult = 1.5f;
}

[Serializable]
public class HeroTemplateJsonCollection
{
    public List<HeroTemplateJsonRecord> heroes = new List<HeroTemplateJsonRecord>();
}

/// <summary>
/// Runtime lookup registry for hero templates.
/// Builds a stable ID index over ScriptableObject templates and can also
/// validate or ingest future JSON import records.
/// </summary>
public sealed class HeroDatabase
{
    readonly List<HeroData> _allTemplates = new List<HeroData>();
    readonly Dictionary<string, HeroData> _byId = new Dictionary<string, HeroData>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, HeroData> _byAssetName = new Dictionary<string, HeroData>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HeroData> AllTemplates => _allTemplates;
    public int Count => _allTemplates.Count;

    public static HeroDatabase Build(IEnumerable<HeroData> templates)
    {
        var database = new HeroDatabase();
        database.Rebuild(templates);
        return database;
    }

    public void Clear()
    {
        _allTemplates.Clear();
        _byId.Clear();
        _byAssetName.Clear();
    }

    public void Rebuild(IEnumerable<HeroData> templates)
    {
        Clear();

        if (templates == null) return;

        foreach (var template in templates)
        {
            Register(template);
        }
    }

    public bool Register(HeroData template)
    {
        if (template == null) return false;

        string heroId = template.HeroId;
        if (string.IsNullOrWhiteSpace(heroId)) return false;

        if (_byId.ContainsKey(heroId))
        {
            Debug.LogWarning($"[HeroDatabase] Duplicate hero ID '{heroId}' ignored.");
            return false;
        }

        _allTemplates.Add(template);
        _byId[heroId] = template;

        if (!string.IsNullOrWhiteSpace(template.name))
        {
            _byAssetName[template.name] = template;
        }

        return true;
    }

    public bool TryGet(string heroId, out HeroData template)
    {
        template = null;
        if (string.IsNullOrWhiteSpace(heroId)) return false;

        if (_byId.TryGetValue(heroId, out template))
        {
            return true;
        }

        return _byAssetName.TryGetValue(heroId, out template);
    }

    public HeroData Get(string heroId)
    {
        return TryGet(heroId, out var template) ? template : null;
    }

    public HeroData[] GetPoolByStars(int stars)
    {
        return _allTemplates
            .Where(h => h != null && h.starRating == stars)
            .ToArray();
    }

    public List<string> Validate()
    {
        var issues = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var template in _allTemplates)
        {
            if (template == null)
            {
                issues.Add("Null hero template reference found.");
                continue;
            }

            string heroId = template.HeroId;
            if (string.IsNullOrWhiteSpace(heroId))
            {
                issues.Add($"Hero '{template.name}' is missing a stable heroId.");
                continue;
            }

            if (!seen.Add(heroId))
            {
                issues.Add($"Duplicate heroId '{heroId}' found.");
            }
        }

        return issues;
    }

    public static HeroTemplateJsonCollection CreateJsonCollection(IEnumerable<HeroData> templates)
    {
        var collection = new HeroTemplateJsonCollection();
        if (templates == null) return collection;

        foreach (var template in templates)
        {
            if (template == null) continue;

            collection.heroes.Add(new HeroTemplateJsonRecord
            {
                heroId = template.HeroId,
                assetName = template.name,
                heroName = template.heroName,
                heroClass = template.heroClass.ToString(),
                starRating = template.starRating,
                summonQuote = template.summonQuote,
                possessedSkillId = template.possessedSkillId,
                dropWeight = template.dropWeight,
                baseStats = template.baseStats != null ? template.baseStats : new BaseStats(),
                possibleTraits = template.possibleTraits != null
                    ? new List<PersonalityTrait>(template.possibleTraits)
                    : new List<PersonalityTrait>(),
                hpPerLevel = template.hpPerLevel,
                atkPerLevel = template.atkPerLevel,
                defPerLevel = template.defPerLevel,
                spdPerLevel = template.spdPerLevel,
                baseCritChance = template.baseCritChance,
                baseCritMult = template.baseCritMult
            });
        }

        return collection;
    }

    public static bool TryParseJson(string json, out HeroTemplateJsonCollection collection)
    {
        collection = null;
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            collection = JsonUtility.FromJson<HeroTemplateJsonCollection>(json);
            if (collection == null)
            {
                collection = new HeroTemplateJsonCollection();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HeroDatabase] Failed to parse hero JSON: {e.Message}");
            return false;
        }
    }
}
