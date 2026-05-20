using System;
using System.Collections.Generic;
using UnityEngine;

// ─── Data Structures ──────────────────────────────────────────────────────────

public enum QuestType
{
    DailyLogin,
    SummonHeroes,
    ClearFloors,
    SurviveWithSquad,
    EarnGold,
    ReachFloor,
    SacrificeHeroes,
    ExtractEssence
}

public enum QuestRewardType { Gold, Gems, Essence, Stamina }

public enum QuestCycle { Daily, Milestone }

[System.Serializable]
public class QuestReward
{
    public QuestRewardType type;
    public int amount;
}

[System.Serializable]
public class Quest
{
    public string id;               // unique, stable across saves (e.g. "daily_summon_3")
    public string title;
    public string description;
    public QuestType type;
    public QuestCycle cycle;
    public int targetValue;         // e.g. clear 3 floors
    public int currentValue;        // runtime progress
    public QuestReward reward;
    public bool isCompleted;
    public bool isClaimed;

    public bool IsReady => isCompleted && !isClaimed;
    public float Progress => targetValue > 0
        ? Mathf.Clamp01((float)currentValue / targetValue)
        : 0f;
}

[System.Serializable]
public class QuestSaveData
{
    public List<Quest> quests = new List<Quest>();
    public string lastDailyReset; // ISO date string yyyy-MM-dd
}

// ─── QuestSystem ──────────────────────────────────────────────────────────────

public class QuestSystem : MonoBehaviour
{
    // ─── Events ───────────────────────────────────────────────────────────────

    public static event Action<Quest> OnQuestProgressUpdated;
    public static event Action<Quest> OnQuestCompleted;
    public static event Action<Quest> OnQuestRewardClaimed;

    // ─── State ────────────────────────────────────────────────────────────────

    private QuestSaveData _saveData = new QuestSaveData();
    public IReadOnlyList<Quest> AllQuests => _saveData.quests;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnHeroSummoned   += _ => Track(QuestType.SummonHeroes, 1);
        GameManager.OnHeroDied       += _ => { }; // handled via ExtractEssence explicitly
        GameManager.OnRosterChanged  += CheckDailyLogin;
    }

    private void OnDisable()
    {
        GameManager.OnHeroSummoned  -= _ => Track(QuestType.SummonHeroes, 1);
        GameManager.OnRosterChanged -= CheckDailyLogin;
    }

    // ─── Initialization ───────────────────────────────────────────────────────

    /// <summary>
    /// Call from GameManager.Start() after save is loaded.
    /// Passing in saved QuestSaveData restores progress; null starts fresh.
    /// </summary>
    public void Initialize(QuestSaveData saved)
    {
        if (saved != null && saved.quests?.Count > 0)
        {
            _saveData = saved;
        }
        else
        {
            _saveData = new QuestSaveData();
            BuildDefaultQuests();
        }
        CheckDailyReset();
        EnsureMilestoneQuests(); // add any new milestones added post-launch
    }

    private void BuildDefaultQuests()
    {
        _saveData.quests = new List<Quest>
        {
            // ── Daily ──
            MakeQuest("daily_login",      "Daily Login",
                "Open the game today.",
                QuestType.DailyLogin, QuestCycle.Daily, 1,
                QuestRewardType.Gems, 10),

            MakeQuest("daily_summon_3",   "Answer the Call",
                "Summon 3 heroes today.",
                QuestType.SummonHeroes, QuestCycle.Daily, 3,
                QuestRewardType.Gold, 200),

            MakeQuest("daily_floor_2",    "Into the Dark",
                "Clear 2 tower floors today.",
                QuestType.ClearFloors, QuestCycle.Daily, 2,
                QuestRewardType.Stamina, 3),

            MakeQuest("daily_gold_500",   "Spoils of War",
                "Earn 500 gold from battles today.",
                QuestType.EarnGold, QuestCycle.Daily, 500,
                QuestRewardType.Gold, 150),

            MakeQuest("daily_survive",    "No One Left Behind",
                "Complete a battle with your full squad alive.",
                QuestType.SurviveWithSquad, QuestCycle.Daily, 1,
                QuestRewardType.Gems, 15),

            // ── Milestones ──
            MakeQuest("mile_floor_5",     "Depth Five",
                "Reach floor 5 of the tower.",
                QuestType.ReachFloor, QuestCycle.Milestone, 5,
                QuestRewardType.Gems, 50),

            MakeQuest("mile_floor_10",    "Into the Abyss",
                "Reach floor 10 of the tower.",
                QuestType.ReachFloor, QuestCycle.Milestone, 10,
                QuestRewardType.Gems, 100),

            MakeQuest("mile_summon_10",   "The Gathering",
                "Summon 10 heroes in total.",
                QuestType.SummonHeroes, QuestCycle.Milestone, 10,
                QuestRewardType.Essence, 30),

            MakeQuest("mile_sacrifice_3", "The Ritual Begins",
                "Sacrifice 3 heroes in Synthesis.",
                QuestType.SacrificeHeroes, QuestCycle.Milestone, 3,
                QuestRewardType.Gems, 30),

            MakeQuest("mile_extract_5",   "From Dust",
                "Extract essence from 5 fallen heroes.",
                QuestType.ExtractEssence, QuestCycle.Milestone, 5,
                QuestRewardType.Essence, 50),
        };

        _saveData.lastDailyReset = TodayString();
    }

    // ─── Daily Reset ──────────────────────────────────────────────────────────

    private void CheckDailyReset()
    {
        if (_saveData.lastDailyReset == TodayString()) return;

        foreach (var q in _saveData.quests)
        {
            if (q.cycle != QuestCycle.Daily) continue;
            q.currentValue = 0;
            q.isCompleted  = false;
            q.isClaimed    = false;
        }

        _saveData.lastDailyReset = TodayString();
        GameManager.Instance.SaveGame();

#if UNITY_EDITOR
        Debug.Log("[QuestSystem] Daily quests reset.");
#endif
    }

    private void CheckDailyLogin()
    {
        // Fires on roster change (i.e. any session activity) — counts as login
        Track(QuestType.DailyLogin, 1);
    }

    // ─── Public Tracking API ──────────────────────────────────────────────────

    /// <summary>Add progress to all quests of matching type.</summary>
    public void Track(QuestType type, int amount)
    {
        foreach (var q in _saveData.quests)
        {
            if (q.type != type)        continue;
            if (q.isCompleted)         continue;
            if (q.cycle == QuestCycle.Daily && q.isClaimed) continue;

            q.currentValue = Mathf.Min(q.currentValue + amount, q.targetValue);
            OnQuestProgressUpdated?.Invoke(q);

            if (q.currentValue >= q.targetValue)
                CompleteQuest(q);
        }

        GameManager.Instance.SaveGame();
    }

    /// <summary>
    /// For ReachFloor quests: pass the highest floor reached this session.
    /// Only completes quests whose targetValue <= floorReached.
    /// </summary>
    public void TrackFloorReached(int floorNumber)
    {
        foreach (var q in _saveData.quests)
        {
            if (q.type != QuestType.ReachFloor) continue;
            if (q.isCompleted) continue;
            if (floorNumber < q.targetValue) continue;

            q.currentValue = floorNumber;
            CompleteQuest(q);
        }

        GameManager.Instance.SaveGame();
    }

    /// <summary>
    /// For SurviveWithSquad: pass squad size and survivor count from BattleResult.
    /// </summary>
    public void TrackBattleResult(BattleResult result, int deployedSquadSize)
    {
        // Floor cleared
        Track(QuestType.ClearFloors, result.victory ? 1 : 0);

        // Gold earned
        if (result.victory && result.goldEarned > 0)
            Track(QuestType.EarnGold, result.goldEarned);

        // Flawless squad survival
        if (result.victory
            && result.survivors != null
            && result.survivors.Count >= deployedSquadSize)
        {
            Track(QuestType.SurviveWithSquad, 1);
        }
    }

    /// <summary>Claim reward for a completed quest. Returns false if not ready.</summary>
    public bool ClaimReward(string questId)
    {
        var q = GetQuest(questId);
        if (q == null || !q.IsReady) return false;

        switch (q.reward.type)
        {
            case QuestRewardType.Gold:    GameManager.Instance.AddGold(q.reward.amount);       break;
            case QuestRewardType.Gems:    GameManager.Instance.AddGems(q.reward.amount);       break;
            case QuestRewardType.Essence: GameManager.Instance.AddEssence(q.reward.amount);    break;
            case QuestRewardType.Stamina: GameManager.Instance.AddStamina(q.reward.amount);    break;
        }

        q.isClaimed = true;
        OnQuestRewardClaimed?.Invoke(q);
        GameManager.Instance.SaveGame();

        return true;
    }

    // ─── Queries ──────────────────────────────────────────────────────────────

    public Quest GetQuest(string id) =>
        _saveData.quests.Find(q => q.id == id);

    public List<Quest> GetDailyQuests() =>
        _saveData.quests.FindAll(q => q.cycle == QuestCycle.Daily);

    public List<Quest> GetMilestoneQuests() =>
        _saveData.quests.FindAll(q => q.cycle == QuestCycle.Milestone);

    public int GetClaimableCount() =>
        _saveData.quests.FindAll(q => q.IsReady).Count;

    public QuestSaveData GetSaveData() => _saveData;

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void CompleteQuest(Quest q)
    {
        q.isCompleted = true;
        OnQuestCompleted?.Invoke(q);

#if UNITY_EDITOR
        Debug.Log($"[QuestSystem] Quest completed: {q.title}");
#endif
    }

    /// <summary>
    /// Adds milestone quests that didn't exist in an older save.
    /// Safe to call every boot — skips quests whose id already exists.
    /// </summary>
    private void EnsureMilestoneQuests()
    {
        TryAddQuest(MakeQuest("mile_floor_5", "Depth Five",
            "Reach floor 5 of the tower.",
            QuestType.ReachFloor, QuestCycle.Milestone, 5,
            QuestRewardType.Gems, 50));

        TryAddQuest(MakeQuest("mile_floor_10", "Into the Abyss",
            "Reach floor 10 of the tower.",
            QuestType.ReachFloor, QuestCycle.Milestone, 10,
            QuestRewardType.Gems, 100));
    }

    private void TryAddQuest(Quest q)
    {
        if (_saveData.quests.Exists(e => e.id == q.id)) return;
        _saveData.quests.Add(q);
    }

    private static Quest MakeQuest(
        string id, string title, string description,
        QuestType type, QuestCycle cycle, int target,
        QuestRewardType rewardType, int rewardAmount)
    {
        return new Quest
        {
            id           = id,
            title        = title,
            description  = description,
            type         = type,
            cycle        = cycle,
            targetValue  = target,
            currentValue = 0,
            isCompleted  = false,
            isClaimed    = false,
            reward       = new QuestReward { type = rewardType, amount = rewardAmount }
        };
    }

    private static string TodayString() =>
        DateTime.Now.ToString("yyyy-MM-dd");
}
