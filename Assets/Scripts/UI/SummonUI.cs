using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Canvas in the Summon scene.
///
/// Required hierarchy:
/// Canvas
///   TopBar
///     txt_Gems    (TMP)
///     txt_Gold    (TMP)
///     btn_Back    (Button)
///   SummonPanel
///     btn_Pull1x  (Button)
///     btn_Pull10x (Button)
///     txt_Cost1x  (TMP)
///     txt_Cost10x (TMP)
///     PityBar
///       img_PityFill   (Image — fill type horizontal)
///       txt_PityCount  (TMP)
///   ResultPanel         ← starts inactive
///     ResultGrid
///       Content         ← assign as resultGridContent
///     txt_ResultSummary (TMP)
///     btn_CloseResults  (Button)
///     btn_SummonAgain   (Button)
///   NotificationText    (TMP) ← for "Not enough gems" toast
/// 
/// ResultCard prefab:
///   img_Portrait  (Image)
///   txt_Name      (TMP)
///   txt_Stars     (TMP)
///   obj_NewBadge  (GameObject) ← "NEW" badge, show/hide
///   img_RarityBorder (Image)
/// </summary>
public class SummonUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TMP_Text txt_Gems;
    [SerializeField] TMP_Text txt_Gold;

    [Header("Summon Buttons")]
    [SerializeField] Button   btn_Pull1x;
    [SerializeField] Button   btn_Pull10x;

    [Header("Pity Bar")]
    [SerializeField] Image    img_PityFill;     // set Image type to Filled, horizontal
    [SerializeField] TMP_Text txt_PityCount;

    [Header("Result Panel")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] Transform  resultGridContent;
    [SerializeField] GameObject resultCardPrefab;
    [SerializeField] TMP_Text   txt_ResultSummary;

    [Header("Notification")]
    [SerializeField] TMP_Text   txt_Notification;

    // ── State ─────────────────────────────────────────────
    bool _isSummoning = false;
    List<HeroInstance> _lastResults = new List<HeroInstance>();

    private bool _allRevealed = false;
    private Button _closeBtnInstance;
    private TMP_Text _closeBtnText;
    private GameObject _summonAgainBtnObj;

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
            Debug.Log("[SummonUI] Spawned temporary DebugGameManager for standalone editor scene play.");
        }
    }

    // ─────────────────────────────────────────────────────
    void Start()
    {
        ApplyMobileUIAdjustments();
        resultPanel.SetActive(false);
        if (txt_Notification != null) txt_Notification.gameObject.SetActive(false);

        btn_Pull1x?.onClick.AddListener(OnPull1x);
        btn_Pull10x?.onClick.AddListener(OnPull10x);

        // Bulletproof dynamic click wiring to guarantee functionality under all circumstances
        var backBtn = transform.Find("TopBar/btn_Back")?.GetComponent<Button>();
        if (backBtn != null)
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(OnBackButton);
        }

        var closeResultsBtn = resultPanel.transform.Find("btn_CloseResults")?.GetComponent<Button>();
        if (closeResultsBtn != null)
        {
            closeResultsBtn.onClick.RemoveAllListeners();
            closeResultsBtn.onClick.AddListener(OnCloseResultsClicked);
        }

        var summonAgainBtn = resultPanel.transform.Find("btn_SummonAgain")?.GetComponent<Button>();
        if (summonAgainBtn != null)
        {
            summonAgainBtn.onClick.RemoveAllListeners();
            summonAgainBtn.onClick.AddListener(OnSummonAgain);
        }

        GameManager.OnResourcesChanged += RefreshResources;
        
        // Xianxia Summon hooks
        GachaCardFlip.OnSealShattered += HandleSealShattered;
        GachaCardFlip.OnAuraHum += HandleAuraHum;

        RefreshResources();
        RefreshPityBar();
    }

    void OnDestroy()
    {
        GameManager.OnResourcesChanged -= RefreshResources;
        GachaCardFlip.OnSealShattered -= HandleSealShattered;
        GachaCardFlip.OnAuraHum -= HandleAuraHum;
    }

    // ─────────────────────────────────────────────────────
    // PULL BUTTONS
    // ─────────────────────────────────────────────────────
    void OnPull1x()
    {
        if (_isSummoning) return;
        var gm = GameManager.Instance;

        if (gm.State.gems < GachaSystem.COST_GEMS_1X)
        {
            ShowNotification($"Not enough gems. Need {GachaSystem.COST_GEMS_1X} 💎");
            return;
        }

        _isSummoning = true;
        SetButtonsInteractable(false);

        AudioManager.PlaySummonPull();

        var result = GachaSystem.Pull1x(gm);
        if (result != null)
        {
            _lastResults = new List<HeroInstance> { result };
            StartCoroutine(ShowResultsWithDelay());
        }
        else
        {
            _isSummoning = false;
            SetButtonsInteractable(true);
        }

        RefreshPityBar();
    }

    void OnPull10x()
    {
        if (_isSummoning) return;
        var gm = GameManager.Instance;

        if (gm.State.gems < GachaSystem.COST_GEMS_10X)
        {
            ShowNotification($"Not enough gems. Need {GachaSystem.COST_GEMS_10X} 💎");
            return;
        }

        _isSummoning = true;
        SetButtonsInteractable(false);

        AudioManager.PlaySummonPull();

        _lastResults = GachaSystem.Pull10x(gm);
        StartCoroutine(ShowResultsWithDelay());
        RefreshPityBar();
    }

    // ─────────────────────────────────────────────────────
    // RESULT DISPLAY
    // ─────────────────────────────────────────────────────
    IEnumerator ShowResultsWithDelay()
    {
        // Brief delay for dramatic effect
        yield return new WaitForSeconds(0.5f);
        ShowResults();
    }

    void ShowResults()
    {
        // Clear old cards
        foreach (Transform child in resultGridContent)
            Destroy(child.gameObject);

        // Sort highest rarity first
        _lastResults.Sort((a, b) => b.starRating.CompareTo(a.starRating));

        // Find close and summon again buttons dynamically
        _closeBtnInstance = resultPanel.transform.Find("btn_CloseResults")?.GetComponent<Button>();
        _closeBtnText = _closeBtnInstance?.GetComponentInChildren<TMP_Text>();
        _summonAgainBtnObj = resultPanel.transform.Find("btn_SummonAgain")?.gameObject;

        if (_closeBtnText != null) _closeBtnText.text = "Break All Seals";
        if (_summonAgainBtnObj != null) _summonAgainBtnObj.SetActive(false); // Hide until fully revealed
        _allRevealed = false;

        // Spawn result cards with staggered ink-drop reveal
        StartCoroutine(SpawnResultCards());

        // Summary line: "3× 5★  2× 4★  5× 3★"
        var counts = new Dictionary<int, int>();
        foreach (var h in _lastResults)
        {
            if (!counts.ContainsKey(h.starRating)) counts[h.starRating] = 0;
            counts[h.starRating]++;
        }

        string summary = "";
        for (int s = 5; s >= 1; s--)
            if (counts.ContainsKey(s))
                summary += $"{counts[s]}× {new string('*', s)}   ";

        if (txt_ResultSummary != null)
            txt_ResultSummary.text = $"Pity: {GameManager.Instance.State.pityCounter}/{GachaSystem.PITY_THRESHOLD}   {summary}";

        resultPanel.SetActive(true);
    }

    IEnumerator SpawnResultCards()
    {
        float delay = 0f;
        foreach (var hero in _lastResults)
        {
            var card = Instantiate(resultCardPrefab, resultGridContent);
            SetupResultCard(card, hero);

            // Dynamically attach and initialize our custom Xianxia talisman controller
            var flip = card.AddComponent<GachaCardFlip>();
            flip.Initialize(hero.starRating);

            // Execute "Ink-Drop" staggered drop entry bounce
            StartCoroutine(flip.PlayInkDropEntry(delay));
            delay += 0.08f; 
        }
        yield break;
    }

    void SetupResultCard(GameObject card, HeroInstance hero)
    {
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

        var newBadge = card.transform.Find("obj_NewBadge")?.gameObject;
        if (newBadge != null) newBadge.SetActive(hero.isNew);

        var border = card.transform.Find("img_RarityBorder")?.GetComponent<Image>();
        if (border != null) border.color = GetRarityColor(hero.starRating);
    }

    // ─────────────────────────────────────────────────────
    // CLOSE / SUMMON AGAIN
    // ─────────────────────────────────────────────────────
    public void OnCloseResults()
    {
        resultPanel.SetActive(false);
        _isSummoning = false;
        SetButtonsInteractable(true);

        // Mark heroes as no longer new
        foreach (var h in _lastResults)
            h.isNew = false;

        GameManager.Instance.SaveGame();
    }

    public void OnSummonAgain()
    {
        resultPanel.SetActive(false);
        _isSummoning = false;
        SetButtonsInteractable(true);

        // Try same pull count
        if (_lastResults.Count == 1) OnPull1x();
        else OnPull10x();
    }

    // ─────────────────────────────────────────────────────
    // PITY BAR
    // ─────────────────────────────────────────────────────
    void RefreshPityBar()
    {
        int pity = GameManager.Instance.State.pityCounter;
        if (img_PityFill  != null) img_PityFill.fillAmount = pity / (float)GachaSystem.PITY_THRESHOLD;
        if (txt_PityCount != null) txt_PityCount.text = $"{pity} / {GachaSystem.PITY_THRESHOLD}";
    }

    // ─────────────────────────────────────────────────────
    // RESOURCES
    // ─────────────────────────────────────────────────────
    void RefreshResources()
    {
        var gm = GameManager.Instance;
        if (txt_Gems != null) txt_Gems.text = gm.State.gems.ToString("N0");
        if (txt_Gold != null) txt_Gold.text = gm.State.gold.ToString("N0");
    }

    // ─────────────────────────────────────────────────────
    // NOTIFICATION TOAST
    // ─────────────────────────────────────────────────────
    void ShowNotification(string message)
    {
        if (txt_Notification == null) return;
        StopCoroutine(nameof(HideNotification));
        txt_Notification.text = message;
        txt_Notification.gameObject.SetActive(true);
        StartCoroutine(nameof(HideNotification));
    }

    IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(2.5f);
        if (txt_Notification != null)
            txt_Notification.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────
    void SetButtonsInteractable(bool state)
    {
        if (btn_Pull1x  != null) btn_Pull1x.interactable  = state;
        if (btn_Pull10x != null) btn_Pull10x.interactable = state;
    }

    Color GetRarityColor(int stars) => stars switch
    {
        5 => new Color(1f,   0.84f, 0f),
        4 => new Color(0.75f, 0.5f, 1f),
        3 => new Color(0.37f, 0.63f, 1f),
        2 => new Color(0.37f, 1f,   0.5f),
        _ => new Color(0.67f, 0.67f, 0.67f)
    };

    public void OnBackButton() => SceneLoader.GoToLobby();

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
                    pos.y -= 75f; // Push top bar away from notches and status bar
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

    // ─────────────────────────────────────────────────────
    // XIANXIA BREAKTHROUGH COMPONENT METHODS
    // ─────────────────────────────────────────────────────

    private void OnCloseResultsClicked()
    {
        if (!_allRevealed)
        {
            AudioManager.PlayClick();
            BreakAllSeals();
        }
        else
        {
            AudioManager.PlayClick();
            OnCloseResults();
        }
    }

    private void BreakAllSeals()
    {
        var flips = resultGridContent.GetComponentsInChildren<GachaCardFlip>();
        float stagger = 0f;
        foreach (var flip in flips)
        {
            if (flip.CurrentState == TalismanState.Sealed)
            {
                StartCoroutine(DelayedFlip(flip, stagger));
                stagger += 0.10f; // Elegant symmetric Qi cascade delay
            }
        }
    }

    private IEnumerator DelayedFlip(GachaCardFlip flip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (flip != null) flip.BreakSeal();
    }

    private void HandleSealShattered(Transform cardTransform, int rarity)
    {
        // Bind sound triggers based on Xianxia rarity tiers
        if (rarity == 5)
        {
            // Supreme Immortal breakthrough sound (using synthesis fanfare)
            AudioManager.PlaySynthesisSuccess();
            // Blinding camera shockwave
            TriggerScreenShake(0.45f, 0.22f);
        }
        else if (rarity == 4)
        {
            // Ascended tier: claim reward chime
            AudioManager.PlayClaimReward();
            TriggerScreenShake(0.18f, 0.08f);
        }
        else
        {
            // Mortal tier: clean paper slide click
            AudioManager.PlayClick();
        }

        // Always check if all cards have been revealed
        CheckAllRevealedState();
    }

    private void HandleAuraHum(int rarity)
    {
        // Re-use pull clip as dynamic ambient rising hum
        if (rarity >= 4)
        {
            AudioManager.PlaySummonPull();
        }
    }

    private void CheckAllRevealedState()
    {
        var flips = resultGridContent.GetComponentsInChildren<GachaCardFlip>();
        bool anySealed = false;
        foreach (var f in flips)
        {
            if (f.CurrentState != TalismanState.Awakened)
            {
                anySealed = true;
                break;
            }
        }

        if (!anySealed)
        {
            _allRevealed = true;
            if (_closeBtnText != null) _closeBtnText.text = "Close";
            if (_summonAgainBtnObj != null) _summonAgainBtnObj.SetActive(true);
        }
    }

    public void TriggerScreenShake(float duration, float magnitude)
    {
        StartCoroutine(DoScreenShake(duration, magnitude));
    }

    private IEnumerator DoScreenShake(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            cam.transform.position = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        cam.transform.position = originalPos;
    }
}
