using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Canvas in the Lobby scene.
/// This is the hub — every other screen navigates from here.
///
/// Required hierarchy:
/// Canvas
///   TopBar
///     txt_MasterName   (TMP)  ← "Welcome back, [Name]"
///     txt_PlayerLevel  (TMP)  ← "Lv. 7"
///     img_PlayerAvatar (Image)
///   ResourceBar
///     txt_Gold         (TMP)
///     txt_Gems         (TMP)
///     txt_Stamina      (TMP)  ← "48 / 50"
///     txt_StaminaTimer (TMP)  ← "Next in 3:42"
///     txt_Essence      (TMP)
///   AlertBanner              ← shows important notifications (starts inactive)
///     txt_AlertMessage (TMP)
///     btn_AlertClose   (Button)
///   SquadOverviewPanel
///     txt_SquadTitle   (TMP)  ← "Current Squad" or "No Squad Deployed"
///     txt_SquadPower   (TMP)  ← "Total Power: 12,450"
///     SquadSlotRow             ← horizontal layout, 5 slots
///       (5x) SquadSlotCard (use same prefab as roster card)
///   TowerProgressPanel
///     txt_FloorProgress (TMP) ← "Floor 12 / 100"
///     img_TowerFill     (Image — fill vertical)
///     btn_EnterTower    (Button) → SquadFormation
///   NavigationGrid
///     btn_NavSummon     (Button) → Summon
///     btn_NavRoster     (Button) → Roster
///     btn_NavSynthesis  (Button) → Synthesis (future)
///     btn_NavMemorial   (Button) → Memorial
///     btn_NavInventory  (Button) → Inventory (future)
///     btn_NavFacilities (Button) → Facilities (future)
///   BottomBar
///     txt_BottomStat1   (TMP)  ← "Heroes: 12"
///     txt_BottomStat2   (TMP)  ← "Fallen: 3"
///     txt_BottomStat3   (TMP)  ← "Highest Floor: 12"
///
/// SquadSlotCard prefab (simplified version):
///   img_Portrait  (Image)
///   txt_Name      (TMP)
///   txt_Stars     (TMP)
///   obj_EmptySlot (GameObject) ← shown when slot is empty
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TMP_Text txt_MasterName;
    [SerializeField] TMP_Text txt_PlayerLevel;
    [SerializeField] Image    img_PlayerAvatar;

    [Header("Resource Bar")]
    [SerializeField] TMP_Text txt_Gold;
    [SerializeField] TMP_Text txt_Gems;
    [SerializeField] TMP_Text txt_Stamina;
    [SerializeField] TMP_Text txt_StaminaTimer;
    [SerializeField] TMP_Text txt_Essence;

    [Header("Alert Banner")]
    [SerializeField] GameObject alertBanner;
    [SerializeField] TMP_Text   txt_AlertMessage;

    [Header("Squad Overview")]
    [SerializeField] TMP_Text   txt_SquadTitle;
    [SerializeField] TMP_Text   txt_SquadPower;
    [SerializeField] Transform  squadSlotRow;
    [SerializeField] GameObject squadSlotCardPrefab;

    [Header("Tower Progress")]
    [SerializeField] TMP_Text   txt_FloorProgress;
    [SerializeField] Image      img_TowerFill;
    [SerializeField] Button     btn_EnterTower;

    [Header("Navigation Buttons")]
    [SerializeField] Button btn_NavSummon;
    [SerializeField] Button btn_NavRoster;
    [SerializeField] Button btn_NavSynthesis;
    [SerializeField] Button btn_NavMemorial;
    [SerializeField] Button btn_NavInventory;
    [SerializeField] Button btn_NavFacilities;

    [Header("Quests Panel")]
    [SerializeField] private GameObject questUIPanel;      // drag QuestUI root here
    [SerializeField] private Button questNavButton;
    [SerializeField] private TMP_Text questBadgeText;      // claimable count badge
    [SerializeField] private FacilityUI facilityUI;        // drag FacilityUI root overlay here

    [Header("Bottom Bar")]
    [SerializeField] TMP_Text txt_BottomStat1;
    [SerializeField] TMP_Text txt_BottomStat2;
    [SerializeField] TMP_Text txt_BottomStat3;

    // ── Stamina regen timer ───────────────────────────────
    const float STAMINA_REGEN_SECONDS = 300f; // 5 minutes per stamina
    float _staminaTimer = 0f;

    private void Awake()
    {
        EnsureGameManagerExists();
    }

    private void EnsureGameManagerExists()
    {
        if (GameManager.Instance == null)
        {
            var existing = FindAnyObjectByType<GameManager>();
            if (existing != null) return;

            GameObject go = new GameObject("DebugGameManager");
            go.AddComponent<MoraleSystem>();
            go.AddComponent<QuestSystem>();
            go.AddComponent<FacilityManager>();
            go.AddComponent<GameManager>();
            Debug.Log("[LobbyUI] Spawned temporary DebugGameManager for standalone editor scene play.");
        }
    }

    // ─────────────────────────────────────────────────────
    void Start()
    {
        ApplyMobileUIAdjustments();
        // Wire navigation buttons
        btn_NavSummon?.onClick.AddListener(() => { AudioManager.PlayClick(); SceneLoader.GoToSummon(); });
        btn_NavRoster?.onClick.AddListener(() => { AudioManager.PlayClick(); SceneLoader.GoToRoster(); });
        btn_NavMemorial?.onClick.AddListener(() => { AudioManager.PlayClick(); SceneLoader.GoToMemorial(); });
        btn_NavSynthesis?.onClick.AddListener(() => { AudioManager.PlayClick(); SceneLoader.GoToSynthesis(); });
        btn_NavInventory?.onClick.AddListener(() => { AudioManager.PlayClick(); OnComingSoon(); });
        btn_NavFacilities?.onClick.AddListener(() => {
            AudioManager.PlayClick();
            if (facilityUI != null)
                facilityUI.Open(RefreshAll);
            else
                OnComingSoon();
        });
        btn_EnterTower?.onClick.AddListener(() => { AudioManager.PlayClick(); OnEnterTower(); });

        questNavButton?.onClick.AddListener(() => { AudioManager.PlayClick(); OpenQuestPanel(); });
        if (questUIPanel) questUIPanel.SetActive(false);

        alertBanner?.SetActive(false);

        // Subscribe to events
        GameManager.OnResourcesChanged += RefreshResources;
        GameManager.OnRosterChanged    += RefreshAll;
        CombatManager.OnBattleEnd      += OnBattleEnded;

        QuestSystem.OnQuestRewardClaimed   += OnQuestRewardClaimedOrCompleted;
        QuestSystem.OnQuestCompleted       += OnQuestRewardClaimedOrCompleted;

        RefreshAll();
        CheckAlerts();
    }

    void OnDestroy()
    {
        GameManager.OnResourcesChanged -= RefreshResources;
        GameManager.OnRosterChanged    -= RefreshAll;
        CombatManager.OnBattleEnd      -= OnBattleEnded;

        QuestSystem.OnQuestRewardClaimed   -= OnQuestRewardClaimedOrCompleted;
        QuestSystem.OnQuestCompleted       -= OnQuestRewardClaimedOrCompleted;
    }

    // ─────────────────────────────────────────────────────
    // STAMINA REGEN TICKER
    // ─────────────────────────────────────────────────────
    void Update()
    {
        var state = GameManager.Instance.State;
        if (state.stamina >= state.maxStamina)
        {
            if (txt_StaminaTimer != null) txt_StaminaTimer.text = "FULL";
            return;
        }

        _staminaTimer += Time.deltaTime;
        if (_staminaTimer >= STAMINA_REGEN_SECONDS)
        {
            _staminaTimer = 0f;
            GameManager.Instance.AddStamina(1);
        }

        // Show countdown
        float remaining = STAMINA_REGEN_SECONDS - _staminaTimer;
        int mins = Mathf.FloorToInt(remaining / 60f);
        int secs = Mathf.FloorToInt(remaining % 60f);
        if (txt_StaminaTimer != null)
            txt_StaminaTimer.text = $"Next in {mins}:{secs:D2}";
    }

    // ─────────────────────────────────────────────────────
    // FULL REFRESH
    // ─────────────────────────────────────────────────────
    void RefreshAll()
    {
        RefreshTopBar();
        RefreshResources();
        RefreshSquadOverview();
        RefreshTowerProgress();
        RefreshBottomBar();
        RefreshQuestBadge();
    }

    private void OpenQuestPanel()
    {
        if (questUIPanel) questUIPanel.SetActive(true);
    }

    private void RefreshQuestBadge()
    {
        if (GameManager.Instance == null || GameManager.Instance.QuestSystem == null) return;
        int count = GameManager.Instance.QuestSystem.GetClaimableCount();
        if (questBadgeText != null)
        {
            questBadgeText.gameObject.SetActive(count > 0);
            questBadgeText.text = count.ToString();
        }
    }

    private void OnQuestRewardClaimedOrCompleted(Quest quest)
    {
        RefreshQuestBadge();
    }

    // ─────────────────────────────────────────────────────
    // TOP BAR
    // ─────────────────────────────────────────────────────
    void RefreshTopBar()
    {
        var state = GameManager.Instance.State;
        if (txt_MasterName  != null) txt_MasterName.text  = $"Welcome back, {state.masterName}.";
        if (txt_PlayerLevel != null) txt_PlayerLevel.text = $"Lv. {state.playerLevel}";
    }

    // ─────────────────────────────────────────────────────
    // RESOURCES
    // ─────────────────────────────────────────────────────
    void RefreshResources()
    {
        var state = GameManager.Instance.State;
        if (txt_Gold    != null) txt_Gold.text    = state.gold.ToString("N0");
        if (txt_Gems    != null) txt_Gems.text    = state.gems.ToString("N0");
        if (txt_Stamina != null) txt_Stamina.text = $"{state.stamina} / {state.maxStamina}";
        if (txt_Essence != null) txt_Essence.text = state.essence.ToString("N0");
    }

    // ─────────────────────────────────────────────────────
    // SQUAD OVERVIEW
    // ─────────────────────────────────────────────────────
    void RefreshSquadOverview()
    {
        var squad = GameManager.Instance.GetCurrentSquad();

        if (txt_SquadTitle != null)
            txt_SquadTitle.text = squad.Count > 0 ? "Current Squad" : "No Squad Deployed";

        if (txt_SquadPower != null)
            txt_SquadPower.text = squad.Count > 0
                ? $"Total Power: {GameManager.Instance.GetSquadPower():N0}"
                : "";

        if (squadSlotRow == null) return;

        // Clear existing slot cards
        foreach (Transform child in squadSlotRow)
            Destroy(child.gameObject);

        // Show 5 slots
        for (int i = 0; i < 5; i++)
        {
            var card = Instantiate(squadSlotCardPrefab, squadSlotRow);
            bool filled = i < squad.Count;

            card.transform.Find("obj_EmptySlot")?.gameObject.SetActive(!filled);

            if (filled)
            {
                var hero = squad[i];
                var data = GameManager.Instance.GetHeroData(hero.heroDataId);

                var portrait = card.transform.Find("img_Portrait")?.GetComponent<Image>();
                if (portrait != null && data?.portrait != null)
                    portrait.sprite = data.portrait;

                var nameText = card.transform.Find("txt_Name")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = hero.heroName;

                var starsText = card.transform.Find("txt_Stars")?.GetComponent<TMP_Text>();
                if (starsText != null)
                {
                    starsText.text  = new string('*', hero.starRating);
                    starsText.color = GetRarityColor(hero.starRating);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // TOWER PROGRESS
    // ─────────────────────────────────────────────────────
    void RefreshTowerProgress()
    {
        int floor = GameManager.Instance.State.highestFloorCleared;
        int maxFloor = 100;

        if (txt_FloorProgress != null)
            txt_FloorProgress.text = $"Floor {floor} / {maxFloor}";

        if (img_TowerFill != null)
            img_TowerFill.fillAmount = (float)floor / maxFloor;
    }

    // ─────────────────────────────────────────────────────
    // BOTTOM BAR STATS
    // ─────────────────────────────────────────────────────
    void RefreshBottomBar()
    {
        var state = GameManager.Instance.State;
        int alive  = GameManager.Instance.GetActiveRoster().Count;
        int dead   = GameManager.Instance.GetMemorialHeroes().Count;

        if (txt_BottomStat1 != null) txt_BottomStat1.text = $"Heroes: {alive}";
        if (txt_BottomStat2 != null) txt_BottomStat2.text = $"Fallen: {dead}";
        if (txt_BottomStat3 != null) txt_BottomStat3.text = $"Highest Floor: {state.highestFloorCleared}";
    }

    // ─────────────────────────────────────────────────────
    // ALERT SYSTEM
    // Checks for important states and shows a banner
    // ─────────────────────────────────────────────────────
    void CheckAlerts()
    {
        var activeRoster = GameManager.Instance.GetActiveRoster();

        // Alert: no heroes
        if (activeRoster.Count == 0)
        {
            ShowAlert("Your roster is empty. Visit the Summon Chamber to call new heroes.");
            return;
        }

        // Alert: heroes with critically low morale
        var criticalHeroes = activeRoster.FindAll(h => h.morale < 20);
        if (criticalHeroes.Count > 0)
        {
            ShowAlert($"{criticalHeroes[0].DisplayName} has critically low morale and may refuse orders.");
            return;
        }

        // Alert: last battle had casualties
        if (BattleSceneSetup.LastResult != null && BattleSceneSetup.LastResult.fallen.Count > 0)
        {
            int count = BattleSceneSetup.LastResult.fallen.Count;
            ShowAlert($"{count} hero{(count > 1 ? "es have" : " has")} fallen. Visit the Memorial Hall.");
            BattleSceneSetup.LastResult = null; // clear after showing once
            return;
        }

        alertBanner?.SetActive(false);
    }

    void ShowAlert(string message)
    {
        if (alertBanner == null) return;
        if (txt_AlertMessage != null) txt_AlertMessage.text = message;
        alertBanner.SetActive(true);
    }

    public void OnAlertClose() => alertBanner?.SetActive(false);

    // ─────────────────────────────────────────────────────
    // NAVIGATION ACTIONS
    // ─────────────────────────────────────────────────────
    void OnEnterTower()
    {
        // Check stamina
        if (GameManager.Instance.State.stamina < 5)
        {
            ShowAlert("Not enough Stamina to enter the Tower. (Need 5)");
            return;
        }
        SceneLoader.GoToSquadForm();
    }

    void OnSynthesisComingSoon() =>
        ShowAlert("Synthesis Chamber — coming soon.");

    void OnComingSoon() =>
        ShowAlert("This feature is coming soon.");

    // ─────────────────────────────────────────────────────
    // BATTLE RESULT CALLBACK
    // Called when returning from battle
    // ─────────────────────────────────────────────────────
    void OnBattleEnded(BattleResult result)
    {
        RefreshAll();
        CheckAlerts();
    }

    // ─────────────────────────────────────────────────────
    // COLOR HELPER
    // ─────────────────────────────────────────────────────
    Color GetRarityColor(int stars) => stars switch
    {
        5 => new Color(1f,    0.84f, 0f),
        4 => new Color(0.75f, 0.5f,  1f),
        3 => new Color(0.37f, 0.63f, 1f),
        2 => new Color(0.37f, 1f,    0.5f),
        _ => new Color(0.67f, 0.67f, 0.67f)
    };

    private void ApplyMobileUIAdjustments()
    {
        // Programmatic Mobile Notch/SafeArea Safeguard
        var topBar = transform.Find("TopBar") ?? transform.Find("TopBarPanel");
        if (topBar != null)
        {
            var rect = topBar.GetComponent<RectTransform>();
            if (rect != null)
            {
                float screenRatio = (float)Screen.height / Screen.width;
                if (screenRatio > 1.8f || screenRatio < 0.6f) 
                {
                    var pos = rect.anchoredPosition;
                    pos.y -= 75f; // Push the top elements and Back buttons down away from status bar / camera notch
                    rect.anchoredPosition = pos;
                }
            }
        }

        // Programmatically boost all TextMeshPro texts on Canvas to ensure absolute readability
        var allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in allTexts)
        {
            Button parentButton = txt.GetComponentInParent<Button>();
            if (parentButton != null)
            {
                // Strict button text constraint: let it auto-scale down to 10f to guarantee no cutoffs
                txt.fontSizeMax = Mathf.Max(txt.fontSizeMax, 32f);
                txt.fontSizeMin = 10f; // Let it shrink comfortably to fit the button box
                txt.enableAutoSizing = true;
                txt.fontSize *= 1.15f; // Mild clean boost to fit perfectly
            }
            else
            {
                // General UI labels: huge readability boost
                txt.fontSizeMax = Mathf.Max(txt.fontSizeMax, 42f);
                txt.fontSizeMin = 16f;
                txt.enableAutoSizing = true;
                txt.fontSize *= 1.45f; // Full 45% legibility boost
            }
        }
    }
}
