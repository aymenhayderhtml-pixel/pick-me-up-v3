using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene: Synthesis (navigate from LobbyUI nav button)
/// Attach to: SynthesisUI root GameObject
/// </summary>
public class SynthesisUI : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI essenceCounterText;

    [Header("Target Slot")]
    [SerializeField] private Button targetSlotButton;
    [SerializeField] private GameObject targetHeroCard;
    [SerializeField] private TextMeshProUGUI targetNameText;
    [SerializeField] private TextMeshProUGUI targetStarText;
    [SerializeField] private TextMeshProUGUI targetClassText;
    [SerializeField] private GameObject targetEmptyLabel;

    [Header("Sacrifice Slots")]
    [SerializeField] private Button[] sacrificeSlotButtons;       // length 2
    [SerializeField] private GameObject[] sacrificeCards;         // length 2
    [SerializeField] private TextMeshProUGUI[] sacNameTexts;      // length 2
    [SerializeField] private TextMeshProUGUI[] sacStarTexts;      // length 2
    [SerializeField] private GameObject[] sacEmptyLabels;         // length 2

    [Header("Cost & Validation")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI validationText;
    [SerializeField] private Button synthesizeButton;

    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private GameObject heroPickerCardPrefab;
    [SerializeField] private TextMeshProUGUI gridHintText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultBodyText;
    [SerializeField] private TextMeshProUGUI resultStarText;
    [SerializeField] private Button resultCloseButton;

    [Header("Colors")]
    [SerializeField] private Color sacrificeRedTint = new Color(1f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private Color validColor       = new Color(0.4f, 1f, 0.5f, 1f);
    [SerializeField] private Color errorColor       = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralColor     = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    // ─── Selection mode ───────────────────────────────────────────────────────

    private enum PickMode { None, Target, Sacrifice0, Sacrifice1 }

    // ─── State ────────────────────────────────────────────────────────────────

    private PickMode _pickMode = PickMode.None;
    private HeroInstance _target;
    private HeroInstance[] _sacrifices = new HeroInstance[2];
    private int _starFilter = 0; // 0 = all

    private List<HeroInstance> _displayedHeroes = new();
    private List<GameObject> _spawnedCards = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameManager.OnResourcesChanged += RefreshEssenceCounter;
        GameManager.OnRosterChanged    += OnRosterChanged;
    }

    private void OnDisable()
    {
        GameManager.OnResourcesChanged -= RefreshEssenceCounter;
        GameManager.OnRosterChanged    -= OnRosterChanged;
    }

    private void Start()
    {
        synthesizeButton.onClick.AddListener(OnSynthesizeClicked);

        // Bulletproof dynamic click wiring to guarantee functionality under all circumstances
        if (backButton == null)
        {
            backButton = transform.Find("Header/backButton")?.GetComponent<Button>() ?? transform.Find("TopBar/btn_Back")?.GetComponent<Button>() ?? transform.Find("backButton")?.GetComponent<Button>();
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneLoader.GoToLobby());
        }

        if (resultCloseButton == null)
        {
            resultCloseButton = resultPanel.transform.Find("resultCloseButton")?.GetComponent<Button>() ?? resultPanel.transform.Find("btn_CloseResults")?.GetComponent<Button>();
        }
        if (resultCloseButton != null)
        {
            resultCloseButton.onClick.RemoveAllListeners();
            resultCloseButton.onClick.AddListener(OnResultClosed);
        }

        targetSlotButton.onClick.AddListener(() => EnterPickMode(PickMode.Target));
        sacrificeSlotButtons[0].onClick.AddListener(() => EnterPickMode(PickMode.Sacrifice0));
        sacrificeSlotButtons[1].onClick.AddListener(() => EnterPickMode(PickMode.Sacrifice1));

        resultPanel.SetActive(false);
        ClearAll();
        RefreshEssenceCounter();
        RefreshGrid();
    }

    // ─── Pick Mode ────────────────────────────────────────────────────────────

    private void EnterPickMode(PickMode mode)
    {
        _pickMode = mode;
        _starFilter = 0;

        gridHintText.text = mode == PickMode.Target
            ? "Select the hero to promote."
            : "Select a sacrifice (must match target's star tier).";

        RefreshGrid();
    }

    private void OnHeroPicked(HeroInstance hero)
    {
        switch (_pickMode)
        {
            case PickMode.Target:
                // Clear sacrifices if tier changed
                if (_target != null && _target.starRating != hero.starRating)
                    ClearSacrifices();

                _target = hero;

                // Remove from sacrifice slots if already there
                for (int i = 0; i < 2; i++)
                    if (_sacrifices[i]?.Id == hero.Id) _sacrifices[i] = null;
                break;

            case PickMode.Sacrifice0:
                if (IsAlreadySlotted(hero, excludeSlot: 0)) return;
                _sacrifices[0] = hero;
                break;

            case PickMode.Sacrifice1:
                if (IsAlreadySlotted(hero, excludeSlot: 1)) return;
                _sacrifices[1] = hero;
                break;
        }

        _pickMode = PickMode.None;
        RefreshSlots();
        RefreshValidation();
        RefreshGrid();
    }

    private bool IsAlreadySlotted(HeroInstance hero, int excludeSlot)
    {
        if (_target?.Id == hero.Id) return true;
        for (int i = 0; i < 2; i++)
            if (i != excludeSlot && _sacrifices[i]?.Id == hero.Id) return true;
        return false;
    }

    // ─── Slot Display ─────────────────────────────────────────────────────────

    private void RefreshSlots()
    {
        // Target slot
        bool hasTarget = _target != null;
        targetHeroCard.SetActive(hasTarget);
        targetEmptyLabel.SetActive(!hasTarget);
        if (hasTarget)
        {
            targetNameText.text  = _target.HeroName;
            targetStarText.text  = StarString(_target.starRating);
            targetClassText.text = _target.data.heroClass.ToString();
        }

        // Sacrifice slots
        for (int i = 0; i < 2; i++)
        {
            bool has = _sacrifices[i] != null;
            sacrificeCards[i].SetActive(has);
            sacEmptyLabels[i].SetActive(!has);
            if (has)
            {
                sacNameTexts[i].text = _sacrifices[i].HeroName;
                sacStarTexts[i].text = StarString(_sacrifices[i].starRating);
            }
        }

        // Cost
        if (_target != null)
            costText.text = $"{SynthesisSystem.GetEssenceCost(_target)} Essence";
        else
            costText.text = "— Essence";
    }

    // ─── Validation Display ───────────────────────────────────────────────────

    private void RefreshValidation()
    {
        var sacrificeList = BuildSacrificeList();
        var code = SynthesisSystem.Validate(_target, sacrificeList);

        bool ready = code == SynthesisResultCode.Success;
        synthesizeButton.interactable = ready;

        validationText.color = ready ? validColor : (_target == null ? neutralColor : errorColor);
        validationText.text  = code switch
        {
            SynthesisResultCode.Success              => "Ready to synthesize.",
            SynthesisResultCode.TargetInvalid        => "Select a valid target hero.",
            SynthesisResultCode.AlreadyMaxTier       => $"{_target?.HeroName} is already 5★ — max tier reached.",
            SynthesisResultCode.NotEnoughSacrifices  => "Select 2 sacrifice heroes.",
            SynthesisResultCode.TierMismatch         => "Sacrifices must match target's star tier.",
            SynthesisResultCode.SacrificeIsTarget    => "A hero cannot sacrifice themselves.",
            SynthesisResultCode.InsufficientEssence  =>
                $"Need {SynthesisSystem.GetEssenceCost(_target)} essence " +
                $"(have {GameManager.Instance.Essence}).",
            _ => string.Empty
        };
    }

    // ─── Grid ─────────────────────────────────────────────────────────────────

    private void RefreshGrid()
    {
        foreach (var c in _spawnedCards) Destroy(c);
        _spawnedCards.Clear();
        _displayedHeroes.Clear();

        List<HeroInstance> pool;

        if (_pickMode == PickMode.Sacrifice0 || _pickMode == PickMode.Sacrifice1)
        {
            // Only show valid sacrifices for current target
            pool = _target != null
                ? SynthesisSystem.GetValidSacrifices(_target)
                : new List<HeroInstance>();
        }
        else
        {
            // Target pick or idle: show all eligible (Active/Fatigued/Wounded, not Dead/Deployed, not 5★)
            pool = new List<HeroInstance>();
            foreach (var h in GameManager.Instance.GetFullRoster())
            {
                if (h.Status == HeroStatus.Dead || h.Status == HeroStatus.Deployed) continue;
                pool.Add(h);
            }
        }

        // Star filter
        if (_starFilter > 0)
            pool = pool.FindAll(h => h.starRating == _starFilter);

        if (pool.Count == 0)
        {
            gridHintText.text = _pickMode == PickMode.None
                ? "No heroes available."
                : "No valid heroes for this slot.";
        }
        else if (pool.Count == 2 && (_pickMode == PickMode.Sacrifice0 || _pickMode == PickMode.Sacrifice1))
        {
            gridHintText.text = "<color=#FFA500>⚠️ Alert: Only 2 valid sacrifices exist in your roster (No choice).</color>";
        }

        foreach (var hero in pool)
        {
            var card = Instantiate(heroPickerCardPrefab, gridContent);
            PopulatePickerCard(card, hero);
            _spawnedCards.Add(card);
            _displayedHeroes.Add(hero);
        }
    }

    private void PopulatePickerCard(GameObject card, HeroInstance hero)
    {
        var tmps = card.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmps.Length > 0) tmps[0].text = hero.HeroName;
        if (tmps.Length > 1) tmps[1].text = StarString(hero.starRating);
        if (tmps.Length > 2) tmps[2].text = hero.data.heroClass.ToString();

        // Red overlay: show when this hero is slotted as a sacrifice
        var overlay = card.transform.Find("RedOverlay")?.GetComponent<Image>();
        if (overlay)
        {
            bool isSlottedSacrifice =
                (_sacrifices[0]?.Id == hero.Id) ||
                (_sacrifices[1]?.Id == hero.Id);
            overlay.color = isSlottedSacrifice
                ? sacrificeRedTint
                : Color.clear;
        }

        var btn = card.GetComponentInChildren<Button>();
        if (btn)
        {
            var captured = hero;
            btn.onClick.AddListener(() => OnHeroPicked(captured));
        }
    }

    // ─── Synthesize ───────────────────────────────────────────────────────────

    private void OnSynthesizeClicked()
    {
        var result = SynthesisSystem.Execute(_target, BuildSacrificeList());

        resultPanel.SetActive(true);

        if (result.Code == SynthesisResultCode.Success)
        {
            AudioManager.PlaySynthesisSuccess();
            resultTitleText.text = "Synthesis Complete";
            resultBodyText.text  =
                $"{_target.HeroName} ascended from {result.OldStar}★ to {result.NewStar}★.\n\n" +
                $"{result.Consumed[0].HeroName} and {result.Consumed[1].HeroName} " +
                $"were consumed in the ritual.\n\n" +
                $"Essence spent: {result.EssenceSpent}.";
            resultStarText.text  = StarString(result.NewStar);

            ClearAll();
        }
        else
        {
            // Shouldn't reach here if button gating is correct; guard anyway
            resultTitleText.text = "Synthesis Failed";
            resultBodyText.text  = result.Code.ToString();
            resultStarText.text  = string.Empty;
        }
    }

    private void OnResultClosed()
    {
        resultPanel.SetActive(false);
        RefreshGrid();
        RefreshValidation();
    }

    // ─── Inspector Event Hooks ────────────────────────────────────────────────

    /// <summary>
    /// Set the star tier filter (0 = All, 1-5 = specific star rating).
    /// </summary>
    public void SetStarFilter(int star)
    {
        _starFilter = star;
        RefreshGrid();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void ClearAll()
    {
        _target       = null;
        _sacrifices   = new HeroInstance[2];
        _pickMode     = PickMode.None;
        RefreshSlots();
        RefreshValidation();
    }

    private void ClearSacrifices()
    {
        _sacrifices = new HeroInstance[2];
    }

    private List<HeroInstance> BuildSacrificeList()
    {
        var list = new List<HeroInstance>();
        foreach (var s in _sacrifices)
            if (s != null) list.Add(s);
        return list;
    }

    private void RefreshEssenceCounter()
    {
        if (essenceCounterText)
            essenceCounterText.text = $"Essence: {GameManager.Instance.Essence}";
        RefreshValidation(); // cost readout may change if essence dropped
    }

    private void OnRosterChanged()
    {
        // If target or sacrifice was removed from roster externally, clear them
        var roster = GameManager.Instance.GetFullRoster();
        if (_target != null && !roster.Contains(_target)) _target = null;
        for (int i = 0; i < 2; i++)
            if (_sacrifices[i] != null && !roster.Contains(_sacrifices[i])) _sacrifices[i] = null;

        RefreshSlots();
        RefreshValidation();
        RefreshGrid();
    }

    private static string StarString(int rating) =>
        new string('*', Mathf.Clamp(rating, 1, 5)) +
        new string('-', Mathf.Clamp(5 - rating, 0, 5));
}
