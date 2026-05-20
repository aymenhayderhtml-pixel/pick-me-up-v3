using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Canvas in the Battle scene.
/// BattleSceneSetup (below) handles loading squad + floor data
/// and calling CombatManager.StartBattle().
///
/// Required hierarchy:
/// Canvas
///   BattleView
///     HeroRow          ← parent for hero unit cards (left side)
///     EnemyRow         ← parent for enemy unit cards (right side)
///     txt_TurnCounter  (TMP)
///     SpeedButtons
///       btn_Speed1x    (Button)
///       btn_Speed2x    (Button)
///       btn_Speed3x    (Button)
///   CombatLogPanel
///     ScrollRect
///       txt_CombatLog  (TMP)  ← append lines here
///   ResultPanel              ← starts inactive
///     txt_ResultTitle  (TMP) ← "VICTORY" or "DEFEAT"
///     txt_Survivors    (TMP)
///     txt_Fallen       (TMP)
///     txt_GoldEarned   (TMP)
///     btn_Continue     (Button) → Lobby
///
/// UnitCard prefab (used for both heroes and enemies):
///   img_Portrait    (Image)
///   txt_Name        (TMP)
///   HPBar
///     img_HPFill    (Image — fill horizontal)
///   txt_HP          (TMP)
///   obj_DeadOverlay (GameObject) ← grey overlay, starts inactive
///   obj_LowHPGlow   (GameObject) ← red pulsing border, starts inactive
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("Battle View")]
    [SerializeField] Transform   heroRow;
    [SerializeField] Transform   enemyRow;
    [SerializeField] TMP_Text    txt_TurnCounter;
    [SerializeField] GameObject  unitCardPrefab;

    [Header("Speed Buttons")]
    [SerializeField] Button btn_Speed1x;
    [SerializeField] Button btn_Speed2x;
    [SerializeField] Button btn_Speed3x;

    [Header("Combat Log")]
    [SerializeField] TMP_Text    txt_CombatLog;
    [SerializeField] ScrollRect  logScrollRect;
    int _maxLogLines = 50;

    [Header("Result Panel")]
    [SerializeField] GameObject  resultPanel;
    [SerializeField] TMP_Text    txt_ResultTitle;
    [SerializeField] TMP_Text    txt_Survivors;
    [SerializeField] TMP_Text    txt_Fallen;
    [SerializeField] TMP_Text    txt_GoldEarned;

    // ── Unit card tracking ────────────────────────────────
    Dictionary<string, GameObject> _unitCards = new Dictionary<string, GameObject>();
    List<string> _logLines = new List<string>();

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
            Debug.Log("[BattleUI] Spawned temporary DebugGameManager for standalone editor scene play.");
        }
    }

    // ─────────────────────────────────────────────────────
    void Start()
    {
        ApplyMobileUIAdjustments();
        resultPanel.SetActive(false);

        // Subscribe to CombatManager events
        CombatManager.OnAttack       += HandleAttack;
        CombatManager.OnUnitDied     += HandleUnitDied;
        CombatManager.OnUnitLowHP    += HandleUnitLowHP;
        CombatManager.OnCombatLog    += HandleLogLine;
        CombatManager.OnBattleEnd    += HandleBattleEnd;
        CombatManager.OnTurnStart    += HandleTurnStart;

        // Speed buttons
        btn_Speed1x?.onClick.AddListener(() => SetSpeed(1f));
        btn_Speed2x?.onClick.AddListener(() => SetSpeed(2f));
        btn_Speed3x?.onClick.AddListener(() => SetSpeed(3f));

        // Build unit cards from BattleSceneSetup data
        BuildUnitCards();

        // Start battle
        BattleSceneSetup.StartPendingBattle();
    }

    void OnDestroy()
    {
        CombatManager.OnAttack    -= HandleAttack;
        CombatManager.OnUnitDied  -= HandleUnitDied;
        CombatManager.OnUnitLowHP -= HandleUnitLowHP;
        CombatManager.OnCombatLog -= HandleLogLine;
        CombatManager.OnBattleEnd -= HandleBattleEnd;
        CombatManager.OnTurnStart -= HandleTurnStart;
    }

    // ─────────────────────────────────────────────────────
    // UNIT CARD SETUP
    // ─────────────────────────────────────────────────────
    void BuildUnitCards()
    {
        // Heroes (left side)
        foreach (var hero in BattleSceneSetup.PendingSquad)
        {
            var card = Instantiate(unitCardPrefab, heroRow);
            var data = GameManager.Instance.GetHeroData(hero.heroDataId);

            SetupCard(card, hero.instanceId, hero.DisplayName,
                      hero.currentHP, hero.maxHP, data?.portrait);

            _unitCards[hero.instanceId] = card;
        }

        // Enemies (right side)
        int idx = 0;
        foreach (var enemy in BattleSceneSetup.PendingEnemies)
        {
            string id = "enemy_" + idx++;
            var card = Instantiate(unitCardPrefab, enemyRow);
            SetupCard(card, id, enemy.enemyName, enemy.hp, enemy.hp, enemy.portrait);
            _unitCards[id] = card;
        }
    }

    void SetupCard(GameObject card, string id, string unitName, int hp, int maxHP, Sprite portrait)
    {
        var img = card.transform.Find("img_Portrait")?.GetComponent<Image>();
        if (img != null && portrait != null) img.sprite = portrait;

        var nameText = card.transform.Find("txt_Name")?.GetComponent<TMP_Text>();
        if (nameText != null) nameText.text = unitName;

        UpdateHPBar(card, hp, maxHP);

        // Hide dead overlay and low HP glow by default
        card.transform.Find("obj_DeadOverlay")?.gameObject.SetActive(false);
        card.transform.Find("obj_LowHPGlow")?.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────
    // COMBAT EVENT HANDLERS
    // ─────────────────────────────────────────────────────
    void HandleTurnStart(int turn)
    {
        if (txt_TurnCounter != null)
            txt_TurnCounter.text = $"Turn {turn}";
    }

    void HandleAttack(CombatUnit attacker, CombatUnit target, int damage, bool isCrit)
    {
        if (!_unitCards.TryGetValue(target.instanceId, out var card)) return;
        UpdateHPBar(card, target.currentHP, target.maxHP);

        // Flash the card red on hit
        StartCoroutine(FlashCard(card, isCrit ? Color.yellow : Color.red));
    }

    void HandleUnitLowHP(CombatUnit unit)
    {
        if (!_unitCards.TryGetValue(unit.instanceId, out var card)) return;
        card.transform.Find("obj_LowHPGlow")?.gameObject.SetActive(true);
    }

    void HandleUnitDied(CombatUnit unit)
    {
        if (!_unitCards.TryGetValue(unit.instanceId, out var card)) return;

        // Show dead overlay (greyscale effect)
        card.transform.Find("obj_DeadOverlay")?.gameObject.SetActive(true);
        card.transform.Find("obj_LowHPGlow")?.gameObject.SetActive(false);

        // Zero HP bar
        UpdateHPBar(card, 0, 1);

        // Screen shake
        StartCoroutine(ScreenShake(unit.isHero ? 0.4f : 0.15f));
    }

    void HandleLogLine(string line)
    {
        _logLines.Add(line);
        if (_logLines.Count > _maxLogLines)
            _logLines.RemoveAt(0);

        if (txt_CombatLog != null)
        {
            txt_CombatLog.text = string.Join("\n", _logLines);
            // Scroll to bottom
            StartCoroutine(ScrollLogToBottom());
        }
    }

    void HandleBattleEnd(BattleResult result)
    {
        ShowResults(result);
    }

    // ─────────────────────────────────────────────────────
    // RESULTS PANEL
    // ─────────────────────────────────────────────────────
    void ShowResults(BattleResult result)
    {
        resultPanel.SetActive(true);

        if (txt_ResultTitle != null)
        {
            txt_ResultTitle.text  = result.victory ? "VICTORY" : "DEFEAT";
            txt_ResultTitle.color = result.victory ? new Color(1f, 0.84f, 0f) : Color.red;
        }

        if (txt_Survivors != null)
        {
            string survivorNames = result.survivors.Count > 0
                ? string.Join(", ", result.survivors.ConvertAll(h => h.DisplayName))
                : "None";
            txt_Survivors.text = $"Survivors: {survivorNames}";
        }

        if (txt_Fallen != null)
        {
            if (result.fallen.Count > 0)
            {
                List<string> lines = new List<string>();
                foreach (var fallenHero in result.fallen)
                {
                    // Match the dropped gear zone in the GameManager state
                    var zone = GameManager.Instance.State.activeSalvageZones
                        .Find(z => z.floorIndex == result.floor && z.fallenHeroName == fallenHero.DisplayName);

                    if (zone != null && zone.gearIds.Count > 0)
                    {
                        string gearList = string.Join(", ", zone.gearIds);
                        lines.Add($"{fallenHero.DisplayName} (Gears Dropped: {gearList})");
                    }
                    else
                    {
                        lines.Add($"{fallenHero.DisplayName} (No gear equipped)");
                    }
                }
                txt_Fallen.text = $"Casualties:\n" + string.Join("\n", lines);
                txt_Fallen.color = Color.red;
            }
            else
            {
                txt_Fallen.text = "No casualties. Safe deployment!";
                txt_Fallen.color = Color.white;
            }
        }

        if (txt_GoldEarned != null)
            txt_GoldEarned.text = $"Gold Earned: {result.goldEarned:N0}";

        // Wire continue button if found dynamically to guarantee scene navigation
        var continueBtn = resultPanel.transform.Find("btn_Continue")?.GetComponent<Button>();
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(OnContinueButton);
        }

        // Store result for Results scene
        BattleSceneSetup.LastResult = result;
    }

    public void OnContinueButton() => SceneLoader.GoToLobby();

    // ─────────────────────────────────────────────────────
    // SPEED CONTROL
    // ─────────────────────────────────────────────────────
    void SetSpeed(float speed)
    {
        CombatManager.Instance?.SetSpeed(speed);
        // Visual: highlight active speed button
        var img1x = btn_Speed1x != null ? btn_Speed1x.GetComponent<Image>() : null;
        if (img1x != null) img1x.color = speed == 1f ? Color.yellow : Color.white;

        var img2x = btn_Speed2x != null ? btn_Speed2x.GetComponent<Image>() : null;
        if (img2x != null) img2x.color = speed == 2f ? Color.yellow : Color.white;

        var img3x = btn_Speed3x != null ? btn_Speed3x.GetComponent<Image>() : null;
        if (img3x != null) img3x.color = speed == 3f ? Color.yellow : Color.white;
    }

    // ─────────────────────────────────────────────────────
    // UI HELPERS
    // ─────────────────────────────────────────────────────
    void UpdateHPBar(GameObject card, int currentHP, int maxHP)
    {
        var fill = card.transform.Find("HPBar/img_HPFill")?.GetComponent<Image>();
        if (fill != null)
        {
            fill.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            fill.color = fill.fillAmount > 0.5f ? Color.green
                       : fill.fillAmount > 0.25f ? Color.yellow
                       : Color.red;
        }

        var hpText = card.transform.Find("txt_HP")?.GetComponent<TMP_Text>();
        if (hpText != null) hpText.text = $"{currentHP}/{maxHP}";
    }

    IEnumerator FlashCard(GameObject card, Color flashColor)
    {
        var images = card.GetComponentsInChildren<Image>();
        var originals = new Color[images.Length];
        for (int i = 0; i < images.Length; i++) originals[i] = images[i].color;

        foreach (var img in images) img.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < images.Length; i++) images[i].color = originals[i];
    }

    IEnumerator ScreenShake(float duration)
    {
        var cam = Camera.main;
        if (cam == null) yield break;

        Vector3 origin = cam.transform.position;
        float elapsed = 0f;
        float magnitude = 0.15f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.position = new Vector3(origin.x + x, origin.y + y, origin.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = origin;
    }

    IEnumerator ScrollLogToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (logScrollRect != null)
            logScrollRect.verticalNormalizedPosition = 0f;
    }

    private void ApplyMobileUIAdjustments()
    {
        // Programmatic Mobile Notch/SafeArea Safeguard
        var topBar = transform.Find("TopBar") ?? transform.Find("TopBarPanel") ?? transform.Find("BattleView/txt_TurnCounter")?.parent;
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
}

// ─────────────────────────────────────────────────────────
// BATTLE SCENE SETUP
// Static bridge — set before loading Battle scene,
// read by BattleUI when Battle scene starts.
// ─────────────────────────────────────────────────────────
public static class BattleSceneSetup
{
    public static List<HeroInstance> PendingSquad   = new List<HeroInstance>();
    public static List<EnemyData>    PendingEnemies = new List<EnemyData>();
    public static int                PendingFloor   = 1;
    public static BattleResult       LastResult;

    /// <summary>
    /// Call this BEFORE SceneLoader.GoToBattle().
    /// SquadFormationUI calls this when the player confirms their squad.
    /// </summary>
    public static void PrepareForBattle(List<HeroInstance> squad, List<EnemyData> enemies, int floor)
    {
        PendingSquad   = squad;
        PendingEnemies = enemies;
        PendingFloor   = floor;
    }

    /// <summary>Called by BattleUI.Start() to kick off the simulation.</summary>
    public static void StartPendingBattle()
    {
        CombatManager.Instance?.StartBattle(PendingSquad, PendingEnemies, PendingFloor);
    }
}
