using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the Facility Overlay Panel in the Lobby scene.
/// This script handles visual mapping and player interactions with the Tavern,
/// Training Grounds, Progressive Stamina refills, and Monthly card claims.
/// </summary>
public class FacilityUI : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject mainPanel;        // The facility root panel overlay
    [SerializeField] private GameObject heroSelectPanel;   // Roster select overlay for slot assignments

    [Header("Tavern Slot UI")]
    [SerializeField] private TMP_Text txt_TavernStatus;    // "No Hero Resting" or "[HeroName] (Resting)"
    [SerializeField] private Button btn_TavernAssign;      // Opens selection if empty, recalls if filled
    [SerializeField] private Button btn_BuyMeal;           // Spend 200 Gold for instant 50 Morale recovery
    [SerializeField] private TMP_Text txt_MealCost;        // "Cost: 200 Gold"

    [Header("Training Slot UI")]
    [SerializeField] private TMP_Text txt_TrainingStatus;  // "No Hero Training" or "[HeroName] (Training)"
    [SerializeField] private Button btn_TrainingAssign;    // Opens selection if empty, recalls if filled

    [Header("Subscription & Stamina")]
    [SerializeField] private Button btn_ClaimSubCard;      // Spend subscription claim
    [SerializeField] private TMP_Text txt_SubCardStatus;   // Displays subscription status
    [SerializeField] private Button btn_StaminaRefill;     // Progressive gem stamina refills
    [SerializeField] private TMP_Text txt_StaminaRefills;  // Displays daily stamina refills count (e.g., "Refills: 1/3")

    [Header("Hero Selector Grid")]
    [SerializeField] private Transform selectorContent;   // Grid container for candidates
    [SerializeField] private GameObject selectorCardPrefab; // Prefab with img_Portrait, txt_Name, btn_Select
    [SerializeField] private Button btn_CloseSelector;

    // ── State ─────────────────────────────────────────────
    private string _assigningSlotType = ""; // "rest" or "train"
    private System.Action _onRefreshLobbyCallback;

    // ─────────────────────────────────────────────────────
    public void Open(System.Action onRefreshLobby)
    {
        _onRefreshLobbyCallback = onRefreshLobby;
        mainPanel.SetActive(true);
        heroSelectPanel.SetActive(false);
        RefreshAll();
    }

    public void Close()
    {
        mainPanel.SetActive(false);
        heroSelectPanel.SetActive(false);
        _onRefreshLobbyCallback?.Invoke();
    }

    private void Start()
    {
        // Wire main facility buttons
        btn_BuyMeal?.onClick.AddListener(OnBuyMeal);
        btn_TavernAssign?.onClick.AddListener(() => OnAssignSlotClick("rest"));
        btn_TrainingAssign?.onClick.AddListener(() => OnAssignSlotClick("train"));
        btn_ClaimSubCard?.onClick.AddListener(OnClaimSubCard);
        btn_StaminaRefill?.onClick.AddListener(OnStaminaRefill);
        btn_CloseSelector?.onClick.AddListener(() => heroSelectPanel.SetActive(false));
    }

    // ─────────────────────────────────────────────────────
    // REFRESH ENGINE
    // ─────────────────────────────────────────────────────
    private void RefreshAll()
    {
        RefreshTavernSlot();
        RefreshTrainingSlot();
        RefreshUtilityActions();
    }

    private void RefreshTavernSlot()
    {
        var restingHero = GameManager.Instance.State.roster.FirstOrDefault(h => h.status == HeroStatus.Resting);

        if (restingHero != null)
        {
            txt_TavernStatus.text = $"{restingHero.DisplayName} (Lv.{restingHero.level})\nMorale: {restingHero.morale}/100";
            var assignText = btn_TavernAssign.GetComponentInChildren<TMP_Text>();
            if (assignText != null) assignText.text = "Recall";

            if (btn_BuyMeal != null) btn_BuyMeal.interactable = (restingHero.morale < 100 && GameManager.Instance.State.gold >= 200);
            if (txt_MealCost != null) txt_MealCost.text = "Cost: 200 Gold (+50 Morale)";
        }
        else
        {
            txt_TavernStatus.text = "Tavern Slot Empty\n(Passive +5 Morale/Hour)";
            var assignText = btn_TavernAssign.GetComponentInChildren<TMP_Text>();
            if (assignText != null) assignText.text = "Assign Rest";

            if (btn_BuyMeal != null) btn_BuyMeal.interactable = false;
            if (txt_MealCost != null) txt_MealCost.text = "Select a hero first";
        }
    }

    private void RefreshTrainingSlot()
    {
        var trainingHero = GameManager.Instance.State.roster.FirstOrDefault(h => h.status == HeroStatus.Training);

        if (trainingHero != null)
        {
            txt_TrainingStatus.text = $"{trainingHero.DisplayName} (Lv.{trainingHero.level})\nXP: {trainingHero.currentXP} (Passive +10 XP/Hour)";
            var assignText = btn_TrainingAssign.GetComponentInChildren<TMP_Text>();
            if (assignText != null) assignText.text = "Recall";
        }
        else
        {
            txt_TrainingStatus.text = "Training Ground Empty\n(Passive +10 XP/Hour)";
            var assignText = btn_TrainingAssign.GetComponentInChildren<TMP_Text>();
            if (assignText != null) assignText.text = "Assign Train";
        }
    }

    private void RefreshUtilityActions()
    {
        var state = GameManager.Instance.State;

        // Subscription display
        if (txt_SubCardStatus != null)
        {
            string todayString = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            bool alreadyClaimed = state.lastSubscriptionClaimDate == todayString;

            txt_SubCardStatus.text = state.isSubscriptionActive
                ? $"Monthly Card: ACTIVE {(alreadyClaimed ? "[CLAIMED]" : "[READY]")}"
                : "Monthly Card: INACTIVE";
        }
        if (btn_ClaimSubCard != null)
        {
            string todayString = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            bool alreadyClaimed = state.lastSubscriptionClaimDate == todayString;

            btn_ClaimSubCard.interactable = state.isSubscriptionActive && !alreadyClaimed;
        }

        // Progressive Stamina display
        if (txt_StaminaRefills != null)
        {
            txt_StaminaRefills.text = $"Daily Gem Refills: {state.dailyStaminaRefillsCount} / 3";
        }
        if (btn_StaminaRefill != null)
        {
            btn_StaminaRefill.interactable = state.dailyStaminaRefillsCount < 3;
        }
    }

    // ─────────────────────────────────────────────────────
    // PLAYER INTERACTIONS
    // ─────────────────────────────────────────────────────
    private void OnBuyMeal()
    {
        var restingHero = GameManager.Instance.State.roster.FirstOrDefault(h => h.status == HeroStatus.Resting);
        if (restingHero == null) return;

        if (GameManager.Instance.FacilityManager.BuyInstantMeal(restingHero))
        {
            AudioManager.PlayClick();
            RefreshAll();
            _onRefreshLobbyCallback?.Invoke();
        }
    }

    private void OnClaimSubCard()
    {
        var state = GameManager.Instance.State;
        if (!state.isSubscriptionActive) return;

        string todayString = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (state.lastSubscriptionClaimDate != todayString)
        {
            AudioManager.PlayClick();
            GameManager.Instance.FacilityManager.ClaimSubscriptionDailyGems();
            RefreshAll();
            _onRefreshLobbyCallback?.Invoke();
        }
    }

    private void OnStaminaRefill()
    {
        if (GameManager.Instance.RefillStaminaWithGems())
        {
            AudioManager.PlayClick();
            RefreshAll();
            _onRefreshLobbyCallback?.Invoke();
        }
    }

    private void OnAssignSlotClick(string type)
    {
        AudioManager.PlayClick();
        _assigningSlotType = type;

        // Check if there is already a hero assigned to this slot type
        var existingHero = GameManager.Instance.State.roster.FirstOrDefault(h => 
            (type == "rest" && h.status == HeroStatus.Resting) ||
            (type == "train" && h.status == HeroStatus.Training)
        );

        if (existingHero != null)
        {
            // Recall hero back to Active roster
            if (existingHero.status == HeroStatus.Resting)
            {
                GameManager.Instance.FacilityManager.RemoveHeroFromRest(existingHero);
            }
            else if (existingHero.status == HeroStatus.Training)
            {
                GameManager.Instance.FacilityManager.RemoveHeroFromTraining(existingHero);
            }
            RefreshAll();
            _onRefreshLobbyCallback?.Invoke();
            return;
        }

        // Otherwise open selector roster grid
        heroSelectPanel.SetActive(true);
        BuildSelectorGrid();
    }

    // ─────────────────────────────────────────────────────
    // HERO SELECTOR GRID BUILDER
    // ─────────────────────────────────────────────────────
    private void BuildSelectorGrid()
    {
        foreach (Transform child in selectorContent)
            Destroy(child.gameObject);

        // Fetch candidate heroes: Alive, not resting, not training, not dead, not deployed
        var candidates = GameManager.Instance.State.roster.Where(h =>
            h.status == HeroStatus.Active &&
            !h.isDeployed
        ).ToList();

        foreach (var hero in candidates)
        {
            var card = Instantiate(selectorCardPrefab, selectorContent);
            var data = GameManager.Instance.GetHeroData(hero.heroDataId);

            // Portrait
            var portrait = card.transform.Find("img_Portrait")?.GetComponent<Image>();
            if (portrait != null && data?.portrait != null) portrait.sprite = data.portrait;

            // Name
            var nameText = card.transform.Find("txt_Name")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = $"{hero.DisplayName} (Lv.{hero.level})";

            // Select action
            var btn = card.GetComponent<Button>();
            var capturedHero = hero;
            btn?.onClick.AddListener(() => SelectHeroForSlot(capturedHero));
        }
    }

    private void SelectHeroForSlot(HeroInstance hero)
    {
        AudioManager.PlayClick();
        var mgr = GameManager.Instance.FacilityManager;

        if (_assigningSlotType == "rest")
        {
            mgr.AssignHeroToRest(hero);
        }
        else if (_assigningSlotType == "train")
        {
            mgr.AssignHeroToTraining(hero);
        }

        heroSelectPanel.SetActive(false);
        RefreshAll();
        _onRefreshLobbyCallback?.Invoke();
    }
}
