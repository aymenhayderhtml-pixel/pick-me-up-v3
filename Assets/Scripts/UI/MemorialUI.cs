using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene: Memorial (standalone scene, navigated to from LobbyUI)
/// Attach to: MemorialUI (root Canvas or Panel)
/// </summary>
public class MemorialUI : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI essenceCounterText;

    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private GameObject tombstoneCardPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI starRatingText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI epitaphText;
    [SerializeField] private TextMeshProUGUI historyText;
    [SerializeField] private Button extractButton;
    [SerializeField] private TextMeshProUGUI extractButtonLabel;
    [SerializeField] private GameObject extractedLabel;
    [SerializeField] private Button closeDetailButton;

    [Header("Confirm Extract Panel")]
    [SerializeField] private GameObject confirmExtractPanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("Tuning")]
    [Tooltip("Essence awarded per star tier on extraction")]
    [SerializeField] private int[] essencePerStarTier = { 0, 10, 20, 40, 80, 150 }; // index = star rating
    [Tooltip("Seconds between each tombstone card appearing on load")]
    [SerializeField] private float cardRevealStagger = 0.08f;

    // ─── State ────────────────────────────────────────────────────────────────

    private List<HeroInstance> _fallen = new();
    private HeroInstance _selectedHero;
    private readonly List<GameObject> _spawnedCards = new();

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        detailPanel.SetActive(false);
        confirmExtractPanel.SetActive(false);

        // Bulletproof dynamic click wiring to guarantee functionality under all circumstances
        if (backButton == null)
        {
            backButton = transform.Find("TopBar/btn_Back")?.GetComponent<Button>() ?? transform.Find("btn_Back")?.GetComponent<Button>();
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneLoader.GoToLobby());
        }

        if (closeDetailButton == null)
        {
            closeDetailButton = detailPanel.transform.Find("btn_Close")?.GetComponent<Button>() ?? detailPanel.transform.Find("closeDetailButton")?.GetComponent<Button>();
        }
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.RemoveAllListeners();
            closeDetailButton.onClick.AddListener(CloseDetail);
        }

        extractButton.onClick.AddListener(OnExtractClicked);
        confirmYesButton.onClick.AddListener(OnConfirmExtract);
        confirmNoButton.onClick.AddListener(() => confirmExtractPanel.SetActive(false));

        RefreshEssenceCounter();
        LoadFallen();
        StartCoroutine(RevealCards());
    }

    // ─── Grid ─────────────────────────────────────────────────────────────────

    private void LoadFallen()
    {
        foreach (var card in _spawnedCards) Destroy(card);
        _spawnedCards.Clear();

        _fallen = GameManager.Instance.GetFallenHeroes(); // returns roster filtered by Dead status

        if (_fallen.Count == 0)
        {
            // Spawn a single "no fallen heroes" placeholder card
            var empty = Instantiate(tombstoneCardPrefab, gridContent);
            var nameTmp = empty.GetComponentInChildren<TextMeshProUGUI>();
            if (nameTmp) nameTmp.text = "None have fallen yet.";
            _spawnedCards.Add(empty);
            return;
        }

        // Sort: most recently dead first (history list tail is most recent event)
        _fallen.Sort((a, b) => string.Compare(b.Id, a.Id, System.StringComparison.Ordinal));

        foreach (var hero in _fallen)
        {
            var card = Instantiate(tombstoneCardPrefab, gridContent);
            card.SetActive(false); // revealed by coroutine
            PopulateTombstoneCard(card, hero);
            _spawnedCards.Add(card);
        }
    }

    private void PopulateTombstoneCard(GameObject card, HeroInstance hero)
    {
        var labels = card.GetComponentsInChildren<TextMeshProUGUI>(true);
        // Convention: 0=Name, 1=Stars, 2=Epitaph (matches prefab order)
        if (labels.Length > 0) labels[0].text = hero.HeroName;
        if (labels.Length > 1) labels[1].text = StarString(hero.data.starRating);
        if (labels.Length > 2) labels[2].text = FirstLine(GenerateEpitaph(hero));

        // Essence-extracted icon
        var extractedIcon = card.transform.Find("EssenceExtractedIcon");
        if (extractedIcon) extractedIcon.gameObject.SetActive(hero.EssenceExtracted);

        var btn = card.GetComponentInChildren<Button>();
        if (btn)
        {
            var captured = hero;
            btn.onClick.AddListener(() => OpenDetail(captured));
        }
    }

    private IEnumerator RevealCards()
    {
        var wait = new WaitForSeconds(cardRevealStagger);
        foreach (var card in _spawnedCards)
        {
            card.SetActive(true);
            yield return wait;
        }
    }

    // ─── Detail Panel ─────────────────────────────────────────────────────────

    private void OpenDetail(HeroInstance hero)
    {
        _selectedHero = hero;
        detailPanel.SetActive(true);

        heroNameText.text = hero.HeroName;
        starRatingText.text = StarString(hero.data.starRating);
        classText.text = hero.data.heroClass.ToString();
        epitaphText.text = GenerateEpitaph(hero);
        historyText.text = BuildHistoryLog(hero);

        bool alreadyExtracted = hero.EssenceExtracted;
        extractButton.gameObject.SetActive(!alreadyExtracted);
        extractedLabel.SetActive(alreadyExtracted);

        if (!alreadyExtracted)
        {
            int yield = EssenceYield(hero);
            extractButtonLabel.text = $"Extract Essence (+{yield})";
        }
    }

    private void CloseDetail()
    {
        detailPanel.SetActive(false);
        confirmExtractPanel.SetActive(false);
        _selectedHero = null;
    }

    // ─── Essence Extraction ───────────────────────────────────────────────────

    private void OnExtractClicked()
    {
        if (_selectedHero == null) return;

        int yield = EssenceYield(_selectedHero);
        confirmText.text =
            $"Extract the essence of {_selectedHero.HeroName}?\n" +
            $"You will receive {yield} Essence.\n\n" +
            $"<size=80%><color=#888>This cannot be undone.</color></size>";

        confirmExtractPanel.SetActive(true);
    }

    private void OnConfirmExtract()
    {
        if (_selectedHero == null) return;

        int yield = EssenceYield(_selectedHero);
        GameManager.Instance.AddEssence(yield);
        _selectedHero.EssenceExtracted = true;
        _selectedHero.AddHistory($"Essence extracted. {yield} essence returned to the tower.");

        GameManager.Instance.SaveGame();

        // Track Quests
        if (GameManager.Instance.QuestSystem != null)
        {
            GameManager.Instance.QuestSystem.Track(QuestType.ExtractEssence, 1);
        }

        confirmExtractPanel.SetActive(false);

        // Refresh detail panel state
        extractButton.gameObject.SetActive(false);
        extractedLabel.SetActive(true);

        // Refresh card in grid (update extracted icon)
        int idx = _fallen.IndexOf(_selectedHero);
        if (idx >= 0 && idx < _spawnedCards.Count)
            PopulateTombstoneCard(_spawnedCards[idx], _selectedHero);

        RefreshEssenceCounter();
    }

    private int EssenceYield(HeroInstance hero)
    {
        int star = Mathf.Clamp(hero.data.starRating, 1, essencePerStarTier.Length - 1);
        return essencePerStarTier[star];
    }

    // ─── Epitaph Generation ───────────────────────────────────────────────────

    private static string GenerateEpitaph(HeroInstance hero)
    {
        // Epitaph is deterministic from hero GUID + traits — no randomness at read time.
        // Priority: title → trait-flavored → class-generic fallback.

        if (!string.IsNullOrEmpty(hero.Title))
            return $"\"{hero.Title}\"";

        if (hero.HasTrait(PersonalityTrait.Brave))
            return $"Died with sword raised. Fear was never in their vocabulary.";

        if (hero.HasTrait(PersonalityTrait.Loyal))
            return $"Fell so that others might not. Loyalty was their last word.";

        if (hero.HasTrait(PersonalityTrait.Compassionate))
            return $"Gave too much, too freely. Even death could not take their kindness.";

        if (hero.HasTrait(PersonalityTrait.Stoic))
            return $"They made no sound when the end came. That was always their way.";

        if (hero.HasTrait(PersonalityTrait.Cautious))
            return $"Calculated every risk but one. The last one.";

        if (hero.HasTrait(PersonalityTrait.Rebellious))
            return $"Never followed orders. Never backed down. Never surrendered.";

        return hero.data.heroClass switch
        {
            HeroClass.Warrior  => "Stood at the front and never left it.",
            HeroClass.Knight   => "The shield held until the arm behind it did not.",
            HeroClass.Mage     => "The spell was worth casting. Even then.",
            HeroClass.Priest   => "Healed everyone but could not save themselves.",
            HeroClass.Rogue    => "Seen by no one. Mourned by all.",
            _                  => "They were here. That is enough."
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string BuildHistoryLog(HeroInstance hero)
    {
        var history = hero.GetHistory();
        if (history == null || history.Count == 0)
            return "<color=#666>No recorded history.</color>";

        var sb = new System.Text.StringBuilder();
        for (int i = history.Count - 1; i >= 0; i--) // newest first
        {
            sb.AppendLine($"<color=#999>—</color> {history[i]}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string StarString(int rating) =>
        new string('*', Mathf.Clamp(rating, 1, 5)) +
        new string('-', Mathf.Clamp(5 - rating, 0, 5));

    private static string FirstLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        int nl = s.IndexOf('\n');
        return nl < 0 ? s : s[..nl];
    }

    private void RefreshEssenceCounter()
    {
        if (essenceCounterText)
            essenceCounterText.text = $"Essence: {GameManager.Instance.Essence}";
    }
}
