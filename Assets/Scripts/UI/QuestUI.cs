/*
 * QuestUI.cs
 * Scene: Accessible from LobbyUI via nav button (push onto Lobby canvas or separate scene)
 * Attach to: QuestUI root Panel (can live as a hidden panel on the Lobby Canvas)
 *
 * Canvas Hierarchy:
 * QuestUI (Panel, hidden by default)
 *   ├── Header
 *   │     ├── TitleText (TMP)
 *   │     └── CloseButton (Button)
 *   ├── TabRow
 *   │     ├── DailyTabButton (Button)
 *   │     └── MilestoneTabButton (Button)
 *   ├── DailyResetTimer (TMP) — "Resets in HH:MM:SS", Daily tab only
 *   ├── QuestScrollRect (ScrollRect)
 *   │     └── QuestContent (VerticalLayoutGroup)
 *   │           └── [QuestRow prefab, instantiated at runtime]
 *   └── ClaimAllButton (Button) — claims all ready quests in current tab
 *
 * QuestRow prefab (Assets/Prefabs/UI/QuestRow.prefab):
 *   ├── TitleText (TMP)
 *   ├── DescriptionText (TMP)
 *   ├── ProgressBar (Slider, non-interactable)
 *   ├── ProgressText (TMP) — "2 / 3"
 *   ├── RewardText (TMP) — "+50 Gems"
 *   ├── ClaimButton (Button)
 *   │     └── ClaimButtonLabel (TMP) — "Claim" / "Claimed" / "In Progress"
 *   └── CompletedOverlay (Image, low alpha green tint, hidden until claimed)
 *
 * Dependencies: QuestSystem, GameManager
 * Events consumed:
 *   QuestSystem.OnQuestProgressUpdated
 *   QuestSystem.OnQuestCompleted
 *   QuestSystem.OnQuestRewardClaimed
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button dailyTabButton;
    [SerializeField] private Button milestoneTabButton;
    [SerializeField] private Color tabActiveColor   = new Color(0.9f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Daily Timer")]
    [SerializeField] private TextMeshProUGUI dailyResetTimerText;

    [Header("Quest List")]
    [SerializeField] private Transform questContent;
    [SerializeField] private GameObject questRowPrefab;

    [Header("Claim All")]
    [SerializeField] private Button claimAllButton;
    [SerializeField] private TextMeshProUGUI claimAllLabel;

    [Header("Reward Flash")]
    [SerializeField] private GameObject rewardFlashPanel;   // full-screen flash, optional
    [SerializeField] private TextMeshProUGUI rewardFlashText;
    [SerializeField] private float flashDuration = 1.2f;

    // ─── State ────────────────────────────────────────────────────────────────

    private enum Tab { Daily, Milestone }
    private Tab _activeTab = Tab.Daily;

    private readonly List<GameObject> _spawnedRows = new();
    // Maps quest id → spawned row root for targeted refreshes
    private readonly Dictionary<string, GameObject> _rowMap = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        QuestSystem.OnQuestProgressUpdated += OnProgressUpdated;
        QuestSystem.OnQuestCompleted       += OnQuestCompleted;
        QuestSystem.OnQuestRewardClaimed   += OnRewardClaimed;

        RefreshAll();
        StartCoroutine(TimerTick());
    }

    private void OnDisable()
    {
        QuestSystem.OnQuestProgressUpdated -= OnProgressUpdated;
        QuestSystem.OnQuestCompleted       -= OnQuestCompleted;
        QuestSystem.OnQuestRewardClaimed   -= OnRewardClaimed;

        StopAllCoroutines();
    }

    private void Start()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        dailyTabButton.onClick.AddListener(() => SwitchTab(Tab.Daily));
        milestoneTabButton.onClick.AddListener(() => SwitchTab(Tab.Milestone));
        claimAllButton.onClick.AddListener(ClaimAll);

        if (rewardFlashPanel) rewardFlashPanel.SetActive(false);

        SwitchTab(Tab.Daily);
    }

    // ─── Tab ──────────────────────────────────────────────────────────────────

    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;

        SetTabColor(dailyTabButton,     tab == Tab.Daily);
        SetTabColor(milestoneTabButton, tab == Tab.Milestone);

        if (dailyResetTimerText)
            dailyResetTimerText.gameObject.SetActive(tab == Tab.Daily);

        RebuildList();
    }

    private void SetTabColor(Button btn, bool active)
    {
        var img = btn.GetComponent<Image>();
        if (img) img.color = active ? tabActiveColor : tabInactiveColor;
    }

    // ─── List Build ───────────────────────────────────────────────────────────

    private void RebuildList()
    {
        foreach (var row in _spawnedRows) Destroy(row);
        _spawnedRows.Clear();
        _rowMap.Clear();

        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null)
            return;

        var quests = _activeTab == Tab.Daily
            ? GameManager.Instance.QuestSystem.GetDailyQuests()
            : GameManager.Instance.QuestSystem.GetMilestoneQuests();

        // Sort: claimable first, then in-progress, then claimed
        quests.Sort((a, b) =>
        {
            int ScoreOf(Quest q) => q.IsReady ? 0 : (!q.isClaimed ? 1 : 2);
            return ScoreOf(a).CompareTo(ScoreOf(b));
        });

        foreach (var quest in quests)
        {
            var row = Instantiate(questRowPrefab, questContent);
            PopulateRow(row, quest);
            _spawnedRows.Add(row);
            _rowMap[quest.id] = row;
        }

        RefreshClaimAll();
    }

    private void PopulateRow(GameObject row, Quest quest)
    {
        var tmps = row.GetComponentsInChildren<TextMeshProUGUI>(true);
        // Prefab TMP order: 0=Title, 1=Description, 2=ProgressText, 3=RewardText, 4=ClaimButtonLabel
        if (tmps.Length > 0) tmps[0].text = quest.title;
        if (tmps.Length > 1) tmps[1].text = quest.description;
        if (tmps.Length > 2) tmps[2].text = $"{quest.currentValue} / {quest.targetValue}";
        if (tmps.Length > 3) tmps[3].text = RewardString(quest.reward);

        // Progress bar
        var slider = row.GetComponentInChildren<Slider>();
        if (slider)
        {
            slider.minValue    = 0;
            slider.maxValue    = 1;
            slider.value       = quest.Progress;
            slider.interactable = false;
        }

        // Claim button
        var btn = row.GetComponentInChildren<Button>();
        if (btn)
        {
            btn.interactable = quest.IsReady;
            if (tmps.Length > 4)
                tmps[4].text = quest.isClaimed ? "Claimed"
                             : quest.isCompleted ? "Claim"
                             : "In Progress";

            btn.onClick.RemoveAllListeners();
            var captured = quest;
            btn.onClick.AddListener(() => OnClaimClicked(captured));
        }

        // Completed overlay
        var overlay = row.transform.Find("CompletedOverlay");
        if (overlay) overlay.gameObject.SetActive(quest.isClaimed);
    }

    // ─── Claim ────────────────────────────────────────────────────────────────

    private void OnClaimClicked(Quest quest)
    {
        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null)
            return;

        bool claimed = GameManager.Instance.QuestSystem.ClaimReward(quest.id);
        if (!claimed) return;

        AudioManager.PlayClaimReward();
        ShowRewardFlash(quest.reward);
        RefreshRow(quest);
        RefreshClaimAll();
    }

    private void ClaimAll()
    {
        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null)
            return;

        var quests = _activeTab == Tab.Daily
            ? GameManager.Instance.QuestSystem.GetDailyQuests()
            : GameManager.Instance.QuestSystem.GetMilestoneQuests();

        int claimCount = 0;
        QuestReward lastReward = null;

        foreach (var q in quests)
        {
            if (!q.IsReady) continue;
            GameManager.Instance.QuestSystem.ClaimReward(q.id);
            RefreshRow(q);
            lastReward = q.reward;
            claimCount++;
        }

        if (claimCount > 0 && lastReward != null)
        {
            AudioManager.PlayClaimReward();
            ShowRewardFlash(lastReward, claimCount);
        }

        RefreshClaimAll();
    }

    private void RefreshClaimAll()
    {
        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null)
            return;

        var quests = _activeTab == Tab.Daily
            ? GameManager.Instance.QuestSystem.GetDailyQuests()
            : GameManager.Instance.QuestSystem.GetMilestoneQuests();

        int ready = quests.FindAll(q => q.IsReady).Count;
        claimAllButton.interactable = ready > 0;
        if (claimAllLabel) claimAllLabel.text = ready > 1 ? $"Claim All ({ready})" : "Claim All";
    }

    // ─── Targeted Row Refresh ─────────────────────────────────────────────────

    private void RefreshRow(Quest quest)
    {
        if (_rowMap.TryGetValue(quest.id, out var row))
            PopulateRow(row, quest);
    }

    private void RefreshAll()
    {
        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null)
            return;

        foreach (var kvp in _rowMap)
        {
            var quest = GameManager.Instance.QuestSystem.GetQuest(kvp.Key);
            if (quest != null) PopulateRow(kvp.Value, quest);
        }
        RefreshClaimAll();
    }

    // ─── Event Handlers ───────────────────────────────────────────────────────

    private void OnProgressUpdated(Quest quest) => RefreshRow(quest);

    private void OnQuestCompleted(Quest quest)
    {
        RefreshRow(quest);
        RefreshClaimAll();
        // Pulse the row to draw attention
        if (_rowMap.TryGetValue(quest.id, out var row))
            StartCoroutine(PulseRow(row));
    }

    private void OnRewardClaimed(Quest quest) => RefreshRow(quest);

    // ─── Daily Reset Timer ────────────────────────────────────────────────────

    private IEnumerator TimerTick()
    {
        while (true)
        {
            if (dailyResetTimerText && _activeTab == Tab.Daily)
            {
                var now       = DateTime.Now;
                var midnight  = now.Date.AddDays(1);
                var remaining = midnight - now;
                dailyResetTimerText.text =
                    $"Resets in {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // ─── Reward Flash ─────────────────────────────────────────────────────────

    private void ShowRewardFlash(QuestReward reward, int multiplier = 1)
    {
        if (!rewardFlashPanel) return;

        string label = multiplier > 1
            ? $"+{reward.amount * multiplier} {reward.type} (×{multiplier} quests)"
            : $"+{reward.amount} {reward.type}";

        if (rewardFlashText) rewardFlashText.text = label;
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        rewardFlashPanel.SetActive(true);
        var canvasGroup = rewardFlashPanel.GetComponent<CanvasGroup>();

        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / flashDuration);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(flashDuration);
        }

        rewardFlashPanel.SetActive(false);
    }

    // ─── Row Pulse ────────────────────────────────────────────────────────────

    private IEnumerator PulseRow(GameObject row)
    {
        var img = row.GetComponent<Image>();
        if (!img) yield break;

        Color original = img.color;
        Color highlight = new Color(0.9f, 0.8f, 0.2f, 0.4f);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(original, highlight, t / 0.25f <= 1f
                ? t / 0.25f
                : 2f - t / 0.25f);
            yield return null;
        }

        img.color = original;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string RewardString(QuestReward reward) => reward.type switch
    {
        QuestRewardType.Gold    => $"+{reward.amount} Gold",
        QuestRewardType.Gems    => $"+{reward.amount} Gems",
        QuestRewardType.Essence => $"+{reward.amount} Essence",
        QuestRewardType.Stamina => $"+{reward.amount} Stamina",
        _                       => string.Empty
    };
}
