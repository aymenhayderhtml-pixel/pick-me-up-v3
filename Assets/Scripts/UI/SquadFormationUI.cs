using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to Canvas in the SquadFormation scene.
///
/// Required hierarchy:
/// Canvas
///   TopBar
///     txt_Title      (TMP)  ← "SQUAD DEPLOYMENT"
///     btn_Back       (Button)
///   FloorSelectorPanel
///     btn_PrevFloor  (Button)
///     btn_NextFloor  (Button)
///     txt_FloorLabel (TMP)  ← e.g. "FLOOR 3"
///     txt_EnemyInfo  (TMP)  ← e.g. "Enemies: 2x Wolf, 1x Goblin"
///   RosterPanel
///     ScrollRect
///       Content      ← assign as rosterGridContent
///   SquadPanel
///     SlotPrefab or Slot buttons (array of 5 Buttons) ← assign as squadSlots
///       img_Portrait (Image)
///       txt_Name     (TMP)
///       obj_EmptySlot (GameObject) ← placeholder overlay when slot empty
///     txt_TotalPower (TMP)
///     btn_StartBattle (Button)
/// 
/// RosterMemberCard prefab (assign as rosterCardPrefab):
///   img_Portrait   (Image)
///   txt_Name       (TMP)
///   txt_Level      (TMP)
///   txt_Power      (TMP)
///   obj_Selected   (GameObject) ← checkmark/badge if in squad
///   obj_Rebellious (GameObject) ← rebel warning badge if refusing to fight
///   btn_CardSelect (Button)
/// </summary>
public class SquadFormationUI : MonoBehaviour
{
    [Header("Floor Selector")]
    [SerializeField] Button btn_PrevFloor;
    [SerializeField] Button btn_NextFloor;
    [SerializeField] TMP_Text txt_FloorLabel;
    [SerializeField] TMP_Text txt_EnemyInfo;

    [Header("Roster Grid")]
    [SerializeField] Transform rosterGridContent;
    [SerializeField] GameObject rosterCardPrefab;

    [Header("Squad Panels")]
    [SerializeField] Button[] squadSlots; // Array of exactly 5 buttons
    [SerializeField] TMP_Text txt_TotalPower;
    [SerializeField] Button btn_StartBattle;

    // ── State ─────────────────────────────────────────────
    int _selectedFloor = 1;
    int _maxFloorReached = 1;
    List<EnemyData> _floorEnemies = new List<EnemyData>();
    List<HeroInstance> _activeRoster = new List<HeroInstance>();

    // ─────────────────────────────────────────────────────
    void Start()
    {
        var gm = GameManager.Instance;
        _maxFloorReached = gm.State.highestFloorCleared + 1;
        _selectedFloor = _maxFloorReached;

        // Set up floor navigation buttons
        btn_PrevFloor?.onClick.AddListener(() => ChangeFloor(-1));
        btn_NextFloor?.onClick.AddListener(() => ChangeFloor(1));

        // Start battle button
        btn_StartBattle?.onClick.AddListener(OnStartBattle);

        // Bulletproof dynamic click wiring to guarantee functionality under all circumstances
        var backBtn = transform.Find("TopBar/btn_Back")?.GetComponent<Button>();
        if (backBtn != null)
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(OnBackButton);
        }

        // Keep current squad if it's already populated, or initialize empty
        RefreshUI();
    }

    // ─────────────────────────────────────────────────────
    // FLOOR NAVIGATION
    // ─────────────────────────────────────────────────────
    void ChangeFloor(int delta)
    {
        _selectedFloor = Mathf.Clamp(_selectedFloor + delta, 1, _maxFloorReached);
        RefreshUI();
    }

    void RefreshUI()
    {
        RefreshFloorDetails();
        RefreshRoster();
        RefreshSquadSlots();
    }

    void RefreshFloorDetails()
    {
        var floorAsset = Resources.Load<TowerFloor>($"Floors/SO_Floor_{_selectedFloor}");
        if (txt_FloorLabel != null)
        {
            if (floorAsset != null)
                txt_FloorLabel.text = $"FLOOR {_selectedFloor}: {floorAsset.floorTitle.ToUpper()}";
            else
                txt_FloorLabel.text = $"FLOOR {_selectedFloor}";
        }

        // Determine and load enemies for this floor
        _floorEnemies = GetEnemiesForFloor(_selectedFloor);

        if (txt_EnemyInfo != null)
        {
            if (_floorEnemies.Count > 0)
            {
                var enemyCounts = new Dictionary<string, int>();
                foreach (var enemy in _floorEnemies)
                {
                    if (enemy == null) continue;
                    if (!enemyCounts.ContainsKey(enemy.enemyName)) enemyCounts[enemy.enemyName] = 0;
                    enemyCounts[enemy.enemyName]++;
                }
                string details = string.Join(", ", enemyCounts.Select(kv => $"{kv.Value}x {kv.Key}"));
                txt_EnemyInfo.text = $"Enemies: {details}";
            }
            else
            {
                txt_EnemyInfo.text = "No enemies configured. Check Resources/Enemies.";
            }
        }

        // Enable/disable navigation buttons
        if (btn_PrevFloor != null) btn_PrevFloor.interactable = _selectedFloor > 1;
        if (btn_NextFloor != null) btn_NextFloor.interactable = _selectedFloor < _maxFloorReached;
    }

    // ─────────────────────────────────────────────────────
    // ROSTER PANEL
    // ─────────────────────────────────────────────────────
    void RefreshRoster()
    {
        // Clear grid
        foreach (Transform child in rosterGridContent)
            Destroy(child.gameObject);

        // Active (alive) roster only
        _activeRoster = GameManager.Instance.GetActiveRoster();

        foreach (var hero in _activeRoster)
        {
            var card = Instantiate(rosterCardPrefab, rosterGridContent);
            SetupRosterCard(card, hero);
        }
    }

    void SetupRosterCard(GameObject card, HeroInstance hero)
    {
        var data = GameManager.Instance.GetHeroData(hero.heroDataId);

        // Portrait
        var portrait = card.transform.Find("img_Portrait")?.GetComponent<Image>();
        if (portrait != null && data?.portrait != null)
            portrait.sprite = data.portrait;

        // Name
        var nameText = card.transform.Find("txt_Name")?.GetComponent<TMP_Text>();
        if (nameText != null) nameText.text = hero.DisplayName;

        // Level
        var levelText = card.transform.Find("txt_Level")?.GetComponent<TMP_Text>();
        if (levelText != null) levelText.text = $"Lv.{hero.level}";

        // Power
        var powerText = card.transform.Find("txt_Power")?.GetComponent<TMP_Text>();
        if (powerText != null) powerText.text = $"PWR: {hero.Power}";

        // Status Indicators
        bool inSquad = GameManager.Instance.State.currentSquad.Contains(hero.instanceId);
        bool rebelling = hero.IsRebelling();

        var selectedOverlay = card.transform.Find("obj_Selected")?.gameObject;
        if (selectedOverlay != null) selectedOverlay.SetActive(inSquad);

        var rebelliousOverlay = card.transform.Find("obj_Rebellious")?.gameObject;
        if (rebelliousOverlay != null) rebelliousOverlay.SetActive(rebelling);

        // Click Handler
        var btn = card.transform.Find("btn_CardSelect")?.GetComponent<Button>() ?? card.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            if (rebelling)
            {
                // Can't select rebelling heroes
                btn.interactable = false;
            }
            else
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => ToggleHeroInSquad(hero.instanceId));
            }
        }
    }

    void ToggleHeroInSquad(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.State.currentSquad.Contains(instanceId))
        {
            gm.RemoveFromSquad(instanceId);
        }
        else
        {
            var hero = gm.GetHeroByInstanceId(instanceId);
            if (hero != null)
            {
                string blockReason = TraitSystem.GetDeployBlockReason(hero);
                if (blockReason != null)
                {
                    Debug.LogWarning($"[SquadFormationUI] Deployment Blocked: {blockReason}");
                    return;
                }
            }

            if (gm.State.currentSquad.Count >= 5)
            {
                Debug.LogWarning("[SquadFormationUI] Squad is full (max 5 heroes).");
                return;
            }
            gm.AddToSquad(instanceId);
        }

        RefreshUI();
    }

    // ─────────────────────────────────────────────────────
    // SQUAD DETAILS
    // ─────────────────────────────────────────────────────
    void RefreshSquadSlots()
    {
        var gm = GameManager.Instance;
        var currentSquad = gm.GetCurrentSquad();

        for (int i = 0; i < squadSlots.Length; i++)
        {
            if (i >= squadSlots.Length) break;
            var slotBtn = squadSlots[i];
            if (slotBtn == null) continue;

            var portrait = slotBtn.transform.Find("img_Portrait")?.GetComponent<Image>();
            var nameText = slotBtn.transform.Find("txt_Name")?.GetComponent<TMP_Text>();
            var emptyOverlay = slotBtn.transform.Find("obj_EmptySlot")?.gameObject;

            slotBtn.onClick.RemoveAllListeners();

            if (i < currentSquad.Count)
            {
                var hero = currentSquad[i];
                var data = gm.GetHeroData(hero.heroDataId);

                if (portrait != null)
                {
                    portrait.gameObject.SetActive(true);
                    if (data != null && data.portrait != null) portrait.sprite = data.portrait;
                }
                if (nameText != null)
                {
                    nameText.gameObject.SetActive(true);
                    nameText.text = hero.DisplayName;
                }
                if (emptyOverlay != null) emptyOverlay.SetActive(false);

                // Clicking the slot removes the hero
                var capturedId = hero.instanceId;
                slotBtn.onClick.AddListener(() => ToggleHeroInSquad(capturedId));
            }
            else
            {
                // Empty placeholder slot
                if (portrait != null) portrait.gameObject.SetActive(false);
                if (nameText != null) nameText.gameObject.SetActive(false);
                if (emptyOverlay != null) emptyOverlay.SetActive(true);
            }
        }

        // Update squad metrics
        int totalPower = gm.GetSquadPower();
        if (txt_TotalPower != null)
            txt_TotalPower.text = $"Squad Power: {totalPower}";

        // At least 1 deployed hero required to venture out
        if (btn_StartBattle != null)
            btn_StartBattle.interactable = currentSquad.Count > 0;
    }

    // ─────────────────────────────────────────────────────
    // ENEMY DYNAMIC LOADING TACTICS
    // ─────────────────────────────────────────────────────
    List<EnemyData> GetEnemiesForFloor(int floor)
    {
        // Try loading floor asset from Resources/Floors/SO_Floor_<floor>
        var floorAsset = Resources.Load<TowerFloor>($"Floors/SO_Floor_{floor}");
        if (floorAsset != null && floorAsset.enemyPool != null && floorAsset.enemyPool.Count > 0)
        {
            return new List<EnemyData>(floorAsset.enemyPool);
        }

        // Fallback to procedurally generated floors if asset not found
        var enemies = new List<EnemyData>();
        
        // Dynamically load references to our created SO assets in Resources
        var goblin = Resources.Load<EnemyData>("Enemies/SO_Enemy_Goblin");
        var wolf = Resources.Load<EnemyData>("Enemies/SO_Enemy_Wolf");
        var boss = Resources.Load<EnemyData>("Enemies/SO_Enemy_GoblinBoss");
        var skeleton = Resources.Load<EnemyData>("Enemies/SO_Enemy_Skeleton");
        var witch = Resources.Load<EnemyData>("Enemies/SO_Enemy_Witch");

        // Fallback checks
        if (goblin == null || wolf == null || boss == null)
        {
            goblin = ScriptableObject.CreateInstance<EnemyData>();
            goblin.enemyName = "Goblin"; goblin.hp = 30; goblin.atk = 8; goblin.def = 3; goblin.spd = 8;
            
            wolf = ScriptableObject.CreateInstance<EnemyData>();
            wolf.enemyName = "Wolf"; wolf.hp = 25; wolf.atk = 12; wolf.def = 2; wolf.spd = 14;

            boss = ScriptableObject.CreateInstance<EnemyData>();
            boss.enemyName = "Goblin Boss"; boss.hp = 80; boss.atk = 15; boss.def = 8; boss.spd = 10; boss.isBoss = true;
        }
        if (skeleton == null)
        {
            skeleton = ScriptableObject.CreateInstance<EnemyData>();
            skeleton.enemyName = "Skeleton"; skeleton.hp = 40; skeleton.atk = 8; skeleton.def = 8; skeleton.spd = 6;
        }
        if (witch == null)
        {
            witch = ScriptableObject.CreateInstance<EnemyData>();
            witch.enemyName = "Witch"; witch.hp = 18; witch.atk = 16; witch.def = 1; witch.spd = 15;
        }

        // Populating floor challenge scales
        if (floor == 1)
        {
            enemies.Add(goblin);
        }
        else if (floor == 2)
        {
            enemies.Add(goblin);
            enemies.Add(goblin);
        }
        else if (floor == 3)
        {
            enemies.Add(wolf);
            enemies.Add(goblin);
        }
        else if (floor == 4)
        {
            enemies.Add(wolf);
            enemies.Add(skeleton);
        }
        else if (floor % 5 == 0) // Boss floor every 5 levels
        {
            enemies.Add(boss);
            if (floor >= 10) enemies.Add(witch);
        }
        else
        {
            // Procedurally generated high level encounters with all enemy types
            int challengeIndex = floor % 4;
            switch (challengeIndex)
            {
                case 0:
                    enemies.Add(wolf);
                    enemies.Add(goblin);
                    enemies.Add(skeleton);
                    break;
                case 1:
                    enemies.Add(wolf);
                    enemies.Add(witch);
                    enemies.Add(goblin);
                    break;
                case 2:
                    enemies.Add(goblin);
                    enemies.Add(skeleton);
                    enemies.Add(witch);
                    break;
                default:
                    enemies.Add(wolf);
                    enemies.Add(skeleton);
                    enemies.Add(wolf);
                    break;
            }
        }

        return enemies;
    }

    // ─────────────────────────────────────────────────────
    // BATTLE ACTIVATOR
    // ─────────────────────────────────────────────────────
    void OnStartBattle()
    {
        var gm = GameManager.Instance;
        var squad = gm.GetCurrentSquad();

        if (squad.Count == 0 || _floorEnemies.Count == 0) return;

        // Prepare static scene setup parameters
        BattleSceneSetup.PrepareForBattle(squad, _floorEnemies, _selectedFloor);

        // Load battle scene via black fade
        SceneLoader.GoToBattle();
    }

    public void OnBackButton() => SceneLoader.GoToLobby();
}
