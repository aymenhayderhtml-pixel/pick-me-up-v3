using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RosterUI : MonoBehaviour
{
    public static RosterUI Instance { get; private set; }

    private static readonly string[] StarterHeroIds =
    {
        "ISLAT_HAN",
        "ENOK",
        "CHLOE",
        "GIDE",
        "HANSEN",
        "DIKA",
        "JENNA_CIRAI",
        "HAN_ISRAT",
        "AARON_DELCUT",
        "ANTARIS"
    };

    [Header("Grid")]
    [SerializeField] public Transform heroGridContent;
    [SerializeField] public GameObject heroCardPrefab; // legacy compatibility; runtime list no longer depends on it

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
    private string _selectedHeroId;
    private bool _layoutReady;

    private void Awake()
    {
        Instance = this;
        AutoWireSceneReferences();
        EnsureGameManagerExists();
        EnsureGridInfrastructure();
    }

    private void OnEnable()
    {
        GameManager.OnRosterChanged += RefreshGrid;
        GameManager.OnResourcesChanged += RefreshResources;
    }

    private void OnDisable()
    {
        GameManager.OnRosterChanged -= RefreshGrid;
        GameManager.OnResourcesChanged -= RefreshResources;
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
        WireButtons();
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
            var closeDetailBtn = detailPanel.GetComponent<DetailPanelUI>()?.btn_Close ?? FindButton(detailPanel.transform, "btn_Close");
            if (closeDetailBtn != null)
            {
                closeDetailBtn.onClick.RemoveAllListeners();
                closeDetailBtn.onClick.AddListener(CloseDetail);
            }
        }

        RefreshResources();
        RefreshGrid();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        AutoWireSceneReferences();
        EnsureGridInfrastructure();

        List<HeroInstance> source = BuildRosterSource();
        if (source.Count == 0)
        {
            RebuildEmptyGrid();
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
            return;
        }

        List<HeroInstance> filtered = currentFilter switch
        {
            RosterFilter.Active => source.Where(h => h.status != HeroStatus.Dead).ToList(),
            RosterFilter.Fallen => source.Where(h => h.status == HeroStatus.Dead).ToList(),
            _                   => source.ToList()
        };

        if (filtered.Count == 0)
        {
            RebuildEmptyGrid();
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedHeroId) || !filtered.Any(h => h.heroDataId == _selectedHeroId))
        {
            _selectedHeroId = filtered[0].heroDataId;
        }

        RebuildGrid(filtered);

        var selected = filtered.FirstOrDefault(h => h.heroDataId == _selectedHeroId) ?? filtered[0];
        OpenDetailPanel(selected);
    }

    void RebuildGrid(List<HeroInstance> heroes)
    {
        if (heroGridContent == null) return;

        foreach (Transform child in heroGridContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var hero in heroes)
        {
            var card = CreateRuntimeCard(hero, hero.heroDataId == _selectedHeroId);
            card.transform.SetParent(heroGridContent, false);
        }
    }

    private List<HeroInstance> BuildRosterSource()
    {
        var catalog = BuildTemplateRoster();
        if (catalog.Count > 0)
            return catalog;

        if (GameManager.Instance != null && GameManager.Instance.State != null && GameManager.Instance.State.roster != null)
            return GameManager.Instance.State.roster.ToList();

        return new List<HeroInstance>();
    }

    private List<HeroInstance> BuildTemplateRoster()
    {
        var gm = GameManager.Instance;
        var result = new List<HeroInstance>();
        if (gm == null) return result;

        foreach (var heroId in StarterHeroIds)
        {
            var template = gm.GetHeroData(heroId);
            if (template == null)
            {
                Debug.LogWarning($"[RosterUI] Missing hero template '{heroId}'.");
                continue;
            }

            var preview = new HeroInstance(template)
            {
                status = HeroStatus.Active,
                isDeployed = false,
                isNew = false,
                morale = 100,
                fatigue = 0,
                currentXP = 0
            };

            preview.battleLog = new List<string>();
            preview.dateObtained = string.Empty;
            preview.dateDied = string.Empty;
            preview.causeOfDeath = string.Empty;
            preview.deathFloor = 0;
            preview.deathDay = 0;
            preview.RecalculateStats(template);
            result.Add(preview);
        }

        return result;
    }

    void RebuildEmptyGrid()
    {
        if (heroGridContent == null) return;

        foreach (Transform child in heroGridContent)
            Destroy(child.gameObject);

        var empty = new GameObject("EmptyRosterState", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
        empty.transform.SetParent(heroGridContent, false);
        var text = CreateTextElement(empty.transform, "EmptyText", "No heroes yet", 24, FontStyles.Bold, TextAlignmentOptions.Center);
        text.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF);
    }

    // --- Detail Panel Open/Close ---
    public void OpenDetailPanel(HeroInstance hero)
    {
        if (detailPanel == null) return;
        if (hero == null) return;

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

    private void WireButtons()
    {
        if (btn_FilterAll != null)
        {
            btn_FilterAll.onClick.RemoveAllListeners();
            btn_FilterAll.onClick.AddListener(() => ApplyFilter(RosterFilter.All));
        }
        if (btn_FilterActive != null)
        {
            btn_FilterActive.onClick.RemoveAllListeners();
            btn_FilterActive.onClick.AddListener(() => ApplyFilter(RosterFilter.Active));
        }
        if (btn_FilterFallen != null)
        {
            btn_FilterFallen.onClick.RemoveAllListeners();
            btn_FilterFallen.onClick.AddListener(() => ApplyFilter(RosterFilter.Fallen));
        }

        if (btn_Back != null)
        {
            btn_Back.onClick.RemoveAllListeners();
            btn_Back.onClick.AddListener(OnBackButton);
        }
    }

    private void AutoWireSceneReferences()
    {
        if (heroGridContent == null)
        {
            heroGridContent = FindDeepChild(transform, "Content")
                ?? FindDeepChild(transform, "heroGridContent")
                ?? FindDeepChild(transform, "RosterGridContent");
        }

        if (detailPanel == null)
        {
            detailPanel = FindDeepChild(transform, "DetailPanel")?.gameObject;
        }

        if (txt_Gold == null)
            txt_Gold = FindText(transform, "txt_Gold");
        if (txt_Gems == null)
            txt_Gems = FindText(transform, "txt_Gems");
        if (btn_Back == null)
            btn_Back = FindButton(transform, "btn_Back");
        if (btn_FilterAll == null)
            btn_FilterAll = FindButton(transform, "btn_FilterAll");
        if (btn_FilterActive == null)
            btn_FilterActive = FindButton(transform, "btn_FilterActive");
        if (btn_FilterFallen == null)
            btn_FilterFallen = FindButton(transform, "btn_FilterFallen");
    }

    private void EnsureGridInfrastructure()
    {
        if (_layoutReady || heroGridContent == null) return;
        _layoutReady = true;

        var grid = heroGridContent.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = heroGridContent.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = new Vector2(240f, 300f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        var fitter = heroGridContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = heroGridContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private GameObject CreateRuntimeCard(HeroInstance hero, bool selected)
    {
        var card = new GameObject($"HeroCard_{hero.instanceId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
        var rect = card.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        var image = card.GetComponent<Image>();
        image.color = selected
            ? new Color32(0x34, 0x2D, 0x55, 0xFF)
            : hero.status == HeroStatus.Dead
                ? new Color32(0x1E, 0x18, 0x20, 0xFF)
                : new Color32(0x17, 0x17, 0x26, 0xFF);

        var outline = card.GetComponent<Outline>();
        outline.effectColor = selected
            ? new Color32(0xC9, 0xA8, 0x4C, 0xFF)
            : new Color32(0x00, 0x00, 0x00, 0x00);
        outline.effectDistance = selected ? new Vector2(3f, -3f) : Vector2.zero;

        var button = card.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ColorBlock.defaultColorBlock;
        button.onClick.AddListener(() => SelectHero(hero));

        var layout = card.GetComponent<LayoutElement>();
        layout.preferredWidth = 240f;
        layout.preferredHeight = 300f;
        layout.minWidth = 220f;
        layout.minHeight = 280f;

        var vertical = card.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(10, 10, 10, 10);
        vertical.spacing = 4f;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        var portrait = CreateChild(card.transform, "Portrait", typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
        var portraitLayout = portrait.GetComponent<LayoutElement>();
        portraitLayout.preferredHeight = 130f;
        portraitLayout.minHeight = 110f;
        portraitLayout.flexibleHeight = 0f;
        portrait.color = new Color32(0x2A, 0x22, 0x32, 0xFF);
        ApplyPortrait(portrait, hero.data != null ? hero.data.portrait : null);

        var starText = CreateTextElement(card.transform, "txt_Stars", $"STAR {Mathf.Max(1, hero.starRating)}", 18, FontStyles.Bold, TextAlignmentOptions.Left);
        starText.color = new Color32(0xC9, 0xA8, 0x4C, 0xFF);

        var nameText = CreateTextElement(card.transform, "txt_Name", hero.DisplayName, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        nameText.color = hero.status == HeroStatus.Dead ? new Color32(0x80, 0x70, 0x70, 0xFF) : new Color32(0xF0, 0xE8, 0xD8, 0xFF);

        var classText = CreateTextElement(card.transform, "txt_Class", HeroUtils.GetClassLabel(hero.heroClass), 14, FontStyles.Italic, TextAlignmentOptions.Center);
        classText.color = new Color32(0xA0, 0x98, 0x92, 0xFF);

        var levelText = CreateTextElement(card.transform, "txt_Level", $"Lv {hero.level}", 15, FontStyles.Normal, TextAlignmentOptions.Center);
        levelText.color = new Color32(0xD4, 0xC8, 0xB4, 0xFF);

        var statusText = CreateTextElement(card.transform, "txt_Status", hero.status == HeroStatus.Dead ? "FALLEN" : hero.isDeployed ? "DEPLOYED" : "ACTIVE", 15, FontStyles.Bold, TextAlignmentOptions.Center);
        statusText.color = hero.status == HeroStatus.Dead
            ? new Color32(0xB0, 0x30, 0x30, 0xFF)
            : hero.isDeployed
                ? new Color32(0x3A, 0x80, 0xA0, 0xFF)
                : new Color32(0x4A, 0x90, 0x60, 0xFF);

        return card;
    }

    private GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        var child = new GameObject(name, components);
        child.transform.SetParent(parent, false);
        return child;
    }

    private TMP_Text CreateTextElement(Transform parent, string name, string value, int fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var child = CreateChild(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        var text = child.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF);
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        var layout = child.GetComponent<LayoutElement>();
        layout.preferredHeight = fontSize + 8f;
        layout.minHeight = fontSize + 4f;
        return text;
    }

    private static void ApplyPortrait(Image target, Sprite portrait)
    {
        if (target == null)
            return;

        if (portrait == null || portrait.texture == null)
        {
            target.sprite = null;
            target.color = new Color32(0x2A, 0x22, 0x32, 0xFF);
            return;
        }

        target.sprite = portrait;
        target.type = Image.Type.Simple;
        target.preserveAspect = true;
        target.color = Color.white;
    }

    private void SelectHero(HeroInstance hero)
    {
        if (hero == null) return;
        _selectedHeroId = hero.heroDataId;
        OpenDetailPanel(hero);
        RefreshGrid();
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }

        return null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        return FindDeepChild(root, name)?.GetComponent<TMP_Text>();
    }

    private static Button FindButton(Transform root, string name)
    {
        return FindDeepChild(root, name)?.GetComponent<Button>();
    }

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
