using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RosterUI : MonoBehaviour
{
    public static RosterUI Instance { get; private set; }

    [Header("Grid")]
    [SerializeField] public Transform heroGridContent;
    [SerializeField] public GameObject heroCardPrefab;

    [Header("Detail Panel")]
    [SerializeField] public GameObject detailPanel;

    [Header("Top Bar")]
    [SerializeField] public TMP_Text txt_Gold;
    [SerializeField] public TMP_Text txt_Gems;
    [SerializeField] public Button btn_Back;

    [Header("Filter Buttons")]
    [SerializeField] public Button btn_FilterAll;
    [SerializeField] public Button btn_FilterActive;
    [SerializeField] public Button btn_FilterFallen;

    public enum RosterFilter { All, Active, Fallen }
    private RosterFilter currentFilter = RosterFilter.All;

    private void Awake()
    {
        Instance = this;
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
            Debug.Log("[RosterUI] Spawned temporary DebugGameManager for standalone editor scene play.");
        }
    }

    void Start()
    {
        ApplyMobileUIAdjustments();
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        // Subscribe to GameManager events
        GameManager.OnRosterChanged  += RefreshGrid;
        GameManager.OnResourcesChanged += RefreshResources;

        // Filter buttons wiring
        if (btn_FilterAll != null)
            btn_FilterAll.onClick.AddListener(() => ApplyFilter(RosterFilter.All));
        if (btn_FilterActive != null)
            btn_FilterActive.onClick.AddListener(() => ApplyFilter(RosterFilter.Active));
        if (btn_FilterFallen != null)
            btn_FilterFallen.onClick.AddListener(() => ApplyFilter(RosterFilter.Fallen));

        if (btn_Back != null)
        {
            btn_Back.onClick.RemoveAllListeners();
            btn_Back.onClick.AddListener(OnBackButton);
        }

        // Wire Close button on detail panel
        if (detailPanel != null)
        {
            var closeDetailBtn = detailPanel.GetComponent<DetailPanelUI>()?.btn_Close;
            if (closeDetailBtn == null)
            {
                closeDetailBtn = detailPanel.transform.Find("btn_Close")?.GetComponent<Button>();
            }
            if (closeDetailBtn != null)
            {
                closeDetailBtn.onClick.RemoveAllListeners();
                closeDetailBtn.onClick.AddListener(CloseDetail);
            }
        }

        RefreshGrid();
        RefreshResources();
    }

    void OnDestroy()
    {
        GameManager.OnRosterChanged   -= RefreshGrid;
        GameManager.OnResourcesChanged -= RefreshResources;
    }

    // --- Filter and Grid Logic ---
    public void ApplyFilter(RosterFilter filter)
    {
        currentFilter = filter;
        RefreshGrid();
    }

    void RefreshGrid()
    {
        if (GameManager.Instance == null || GameManager.Instance.State == null) return;

        List<HeroInstance> source = GameManager.Instance.State.roster;

        List<HeroInstance> filtered = currentFilter switch
        {
            RosterFilter.Active => source.Where(h => h.status != HeroStatus.Dead).ToList(),
            RosterFilter.Fallen => source.Where(h => h.status == HeroStatus.Dead).ToList(),
            _                   => source.ToList()
        };

        RebuildGrid(filtered);
    }

    void RebuildGrid(List<HeroInstance> heroes)
    {
        if (heroGridContent == null) return;

        foreach (Transform child in heroGridContent)
            Destroy(child.gameObject);

        foreach (var hero in heroes)
        {
            if (heroCardPrefab != null)
            {
                var card = Instantiate(heroCardPrefab, heroGridContent);
                var cardUI = card.GetComponent<HeroCardUI>();
                if (cardUI != null)
                {
                    cardUI.Populate(hero);
                }
            }
        }
    }

    // --- Detail Panel Open/Close ---
    public void OpenDetailPanel(HeroInstance hero)
    {
        if (detailPanel == null) return;

        DetailPanelMode mode;

        if (hero.status == HeroStatus.Dead)
            mode = DetailPanelMode.Memorial;
        else if (hero.isDeployed)
            mode = DetailPanelMode.DeployedLocked;
        else
            mode = DetailPanelMode.Normal;

        var ui = detailPanel.GetComponent<DetailPanelUI>();
        if (ui != null)
        {
            ui.Populate(hero, mode);
        }
        detailPanel.SetActive(true);
    }

    public void CloseDetail()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    // --- Resources ---
    void RefreshResources()
    {
        if (GameManager.Instance == null || GameManager.Instance.State == null) return;

        if (txt_Gold != null) txt_Gold.text = GameManager.Instance.State.gold.ToString("N0");
        if (txt_Gems != null) txt_Gems.text = GameManager.Instance.State.gems.ToString("N0");
    }

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
                txt.fontSizeMax = Mathf.Max(txt.fontSizeMax, 32f);
                txt.fontSizeMin = 10f; // Let it shrink comfortably to fit the button box
                txt.enableAutoSizing = true;
                txt.fontSize *= 1.15f; // Mild clean boost to fit perfectly
            }
            else
            {
                txt.fontSizeMax = Mathf.Max(txt.fontSizeMax, 42f);
                txt.fontSizeMin = 16f;
                txt.enableAutoSizing = true;
                txt.fontSize *= 1.45f; // Full 45% legibility boost
            }
        }
    }
}
