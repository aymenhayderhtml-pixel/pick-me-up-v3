#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneGenerator
{
    private static string PREFAB_DIR = "Assets/Prefabs/UI";

    [MenuItem("Tools/Build and Wire All Scenes")]
    public static void BuildAllScenes()
    {
        string sceneDir = "Assets/Scenes";
        if (!Directory.Exists(sceneDir))
        {
            Directory.CreateDirectory(sceneDir);
        }

        // Generate the missing ResultCard prefab for Summon results first
        GenerateResultCardPrefab();

        // 1. Create Bootstrap Scene
        CreateBootstrapScene(sceneDir + "/Bootstrap.unity");

        // 2. Create Lobby Scene
        CreateLobbyScene(sceneDir + "/Lobby.unity");

        // 3. Create Roster Scene
        CreateRosterScene(sceneDir + "/Roster.unity");

        // 4. Create Summon Scene
        CreateSummonScene(sceneDir + "/Summon.unity");

        // 5. Create SquadFormation Scene
        CreateSquadFormationScene(sceneDir + "/SquadFormation.unity");

        // 6. Create Battle Scene
        CreateBattleScene(sceneDir + "/Battle.unity");

        // 7. Create Memorial Scene
        CreateMemorialScene(sceneDir + "/Memorial.unity");

        // 8. Create Synthesis Scene
        CreateSynthesisScene(sceneDir + "/Synthesis.unity");

        // 9. Register in Build Settings
        RegisterScenesInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 LANDSCAPE EXTRAVAGANZA! All 8 scenes built, nested, programmatically wired for 1920x1080 landscape, and registered successfully!");
    }

    // ─── Missing Prefab Generator ─────────────────────────────────────────────

    private static void GenerateResultCardPrefab()
    {
        string path = PREFAB_DIR + "/ResultCard.prefab";
        if (File.Exists(path)) return;

        GameObject root = new GameObject("ResultCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 200);
        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        // img_Portrait
        GameObject portrait = new GameObject("img_Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portrait.transform.SetParent(root.transform, false);
        SetAnchor(portrait, AnchorType.StretchAll, Vector2.zero, new Vector2(160, 200));
        portrait.GetComponent<Image>().color = Color.grey;

        // img_RarityBorder
        GameObject border = new GameObject("img_RarityBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        border.transform.SetParent(root.transform, false);
        SetAnchor(border, AnchorType.StretchAll, Vector2.zero, new Vector2(160, 200));
        Image borderImg = border.GetComponent<Image>();
        borderImg.color = Color.white;
        borderImg.raycastTarget = false;

        // txt_Name
        GameObject nameText = CreateTMPTextObj(root, "txt_Name", "Hero Name", 16, TextAlignmentOptions.Bottom);
        SetAnchor(nameText, AnchorType.Bottom, new Vector2(0, 20), new Vector2(150, 30));

        // txt_Stars
        GameObject starsText = CreateTMPTextObj(root, "txt_Stars", "***", 14, TextAlignmentOptions.Top);
        starsText.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        SetAnchor(starsText, AnchorType.Top, new Vector2(0, -10), new Vector2(150, 25));

        // obj_NewBadge
        GameObject newBadge = new GameObject("obj_NewBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        newBadge.transform.SetParent(root.transform, false);
        SetAnchor(newBadge, AnchorType.TopLeft, new Vector2(5, -5), new Vector2(40, 20));
        newBadge.GetComponent<Image>().color = Color.red;
        CreateTMPTextObj(newBadge, "Text", "NEW", 10, TextAlignmentOptions.Center);
        newBadge.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("🎨 Spawned missing ResultCard.prefab successfully.");
    }

    // ─── Scene Builders ───────────────────────────────────────────────────────

    private static void CreateBootstrapScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // _GameManager
        GameObject gmObj = new GameObject("_GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        MoraleSystem ms = gmObj.AddComponent<MoraleSystem>();
        QuestSystem qs = gmObj.AddComponent<QuestSystem>();
        SceneLoader sl = gmObj.AddComponent<SceneLoader>();
        AudioManager am = gmObj.AddComponent<AudioManager>();

        // Bootstrap UI Canvas (for fade image)
        var (canvas, _) = CreateCanvasWithEventSystem("Bootstrap");
        canvas.transform.SetParent(gmObj.transform, false); // Make Canvas persistent as child of _GameManager
        GameObject fadeImageObj = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeImageObj.transform.SetParent(canvas.transform, false);
        SetAnchor(fadeImageObj, AnchorType.StretchAll, Vector2.zero, Vector2.zero);
        Image fadeImage = fadeImageObj.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        // Wire SceneLoader
        SerializedObject slSo = new SerializedObject(sl);
        slSo.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        slSo.ApplyModifiedProperties();

        // _Bootstrap
        GameObject bootObj = new GameObject("_Bootstrap");
        bootObj.AddComponent<Bootstrap>();

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateLobbyScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("Lobby");
        LobbyUI lobby = canvas.AddComponent<LobbyUI>();

        // TopBar (stretched to full width 1920)
        GameObject topBar = CreateUIElement("TopBar", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        TextMeshProUGUI masterName = CreateTMPText("txt_MasterName", topBar, "Welcome back, Master", 26, TextAlignmentOptions.Left);
        SetAnchor(masterName.gameObject, AnchorType.Left, new Vector2(40, 0), new Vector2(400, 80));
        TextMeshProUGUI playerLevel = CreateTMPText("txt_PlayerLevel", topBar, "Lv. 1", 20, TextAlignmentOptions.Right);
        SetAnchor(playerLevel.gameObject, AnchorType.Right, new Vector2(-40, 0), new Vector2(200, 80));

        // ResourceBar
        GameObject resBar = CreateUIElement("ResourceBar", canvas, new Vector2(1920, 80), new Vector2(0, 380));
        TextMeshProUGUI gold = CreateTMPText("txt_Gold", resBar, "1,000", 20, TextAlignmentOptions.Center);
        SetAnchor(gold.gameObject, AnchorType.Center, new Vector2(-450, 0), new Vector2(200, 50));
        TextMeshProUGUI gems = CreateTMPText("txt_Gems", resBar, "500", 20, TextAlignmentOptions.Center);
        SetAnchor(gems.gameObject, AnchorType.Center, new Vector2(-150, 0), new Vector2(200, 50));
        TextMeshProUGUI stamina = CreateTMPText("txt_Stamina", resBar, "50 / 50", 20, TextAlignmentOptions.Center);
        SetAnchor(stamina.gameObject, AnchorType.Center, new Vector2(150, 0), new Vector2(200, 50));
        TextMeshProUGUI staminaTimer = CreateTMPText("txt_StaminaTimer", resBar, "FULL", 14, TextAlignmentOptions.Center);
        SetAnchor(staminaTimer.gameObject, AnchorType.Center, new Vector2(300, 0), new Vector2(150, 40));
        TextMeshProUGUI essence = CreateTMPText("txt_Essence", resBar, "0", 20, TextAlignmentOptions.Center);
        SetAnchor(essence.gameObject, AnchorType.Center, new Vector2(450, 0), new Vector2(200, 50));

        // Alert Banner
        GameObject banner = CreateUIElement("AlertBanner", canvas, new Vector2(1200, 100), new Vector2(0, 270));
        TextMeshProUGUI alertMsg = CreateTMPText("txt_AlertMessage", banner, "Important Alert!", 20, TextAlignmentOptions.Center);
        banner.SetActive(false);

        // ─── Left Side: Squad Overview Panel ───
        GameObject squadPanel = CreateUIElement("SquadOverviewPanel", canvas, new Vector2(850, 600), new Vector2(-480, -80));
        squadPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.5f);
        TextMeshProUGUI squadTitle = CreateTMPText("txt_SquadTitle", squadPanel, "Current Squad", 22, TextAlignmentOptions.Left);
        SetAnchor(squadTitle.gameObject, AnchorType.TopLeft, new Vector2(30, -20), new Vector2(350, 50));
        TextMeshProUGUI squadPower = CreateTMPText("txt_SquadPower", squadPanel, "Power: 0", 18, TextAlignmentOptions.Right);
        SetAnchor(squadPower.gameObject, AnchorType.TopRight, new Vector2(-30, -20), new Vector2(300, 50));
        GameObject slotRow = CreateUIElement("SquadSlotRow", squadPanel, new Vector2(800, 220), new Vector2(0, -40));
        slotRow.AddComponent<HorizontalLayoutGroup>().childControlWidth = false;

        // ─── Right Side: Tower & Navigation Panels ───
        GameObject towerPanel = CreateUIElement("TowerProgressPanel", canvas, new Vector2(850, 220), new Vector2(480, 110));
        towerPanel.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.5f);
        TextMeshProUGUI floorProgress = CreateTMPText("txt_FloorProgress", towerPanel, "Floor 1", 24, TextAlignmentOptions.Left);
        SetAnchor(floorProgress.gameObject, AnchorType.Left, new Vector2(40, 0), new Vector2(250, 80));
        GameObject fillObj = new GameObject("img_TowerFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(towerPanel.transform, false);
        SetAnchor(fillObj, AnchorType.Left, new Vector2(300, 0), new Vector2(150, 30));
        Button enterBtn = CreateButton("btn_EnterTower", towerPanel, "ENTER TOWER", new Vector2(250, 70), new Vector2(250, 0));

        // Navigation Grid (Below Tower progress)
        GameObject navGrid = CreateUIElement("NavigationGrid", canvas, new Vector2(850, 350), new Vector2(480, -200));
        navGrid.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.5f);
        Button summonBtn = CreateButton("btn_NavSummon", navGrid, "SUMMON", new Vector2(250, 80), new Vector2(-280, 80));
        Button rosterBtn = CreateButton("btn_NavRoster", navGrid, "ROSTER", new Vector2(250, 80), new Vector2(0, 80));
        Button synthesisBtn = CreateButton("btn_NavSynthesis", navGrid, "FUSION", new Vector2(250, 80), new Vector2(280, 80));
        Button memorialBtn = CreateButton("btn_NavMemorial", navGrid, "MEMORIAL", new Vector2(250, 80), new Vector2(-280, -40));
        Button inventoryBtn = CreateButton("btn_NavInventory", navGrid, "INVENTORY", new Vector2(250, 80), new Vector2(0, -40));
        Button facilitiesBtn = CreateButton("btn_NavFacilities", navGrid, "FACILITIES", new Vector2(250, 80), new Vector2(280, -40));

        Button questNavBtn = CreateButton("questNavButton", navGrid, "QUEST SYSTEM", new Vector2(220, 60), new Vector2(0, -130));
        TextMeshProUGUI questBadge = CreateTMPText("questBadgeText", questNavBtn.gameObject, "0", 12, TextAlignmentOptions.Center);
        SetAnchor(questBadge.gameObject, AnchorType.TopRight, new Vector2(-5, -5), new Vector2(30, 30));

        // BottomBar
        GameObject botBar = CreateUIElement("BottomBar", canvas, new Vector2(1920, 80), new Vector2(0, -500));
        TextMeshProUGUI stat1 = CreateTMPText("txt_BottomStat1", botBar, "Heroes: 0", 18, TextAlignmentOptions.Center);
        SetAnchor(stat1.gameObject, AnchorType.Center, new Vector2(-400, 0), new Vector2(300, 50));
        TextMeshProUGUI stat2 = CreateTMPText("txt_BottomStat2", botBar, "Fallen: 0", 18, TextAlignmentOptions.Center);
        SetAnchor(stat2.gameObject, AnchorType.Center, new Vector2(0, 0), new Vector2(300, 50));
        TextMeshProUGUI stat3 = CreateTMPText("txt_BottomStat3", botBar, "Max Floor: 0", 18, TextAlignmentOptions.Center);
        SetAnchor(stat3.gameObject, AnchorType.Center, new Vector2(400, 0), new Vector2(300, 50));

        // ─── Setup QuestUI Overlay panel (Landscape format) ───
        GameObject questPanel = CreateUIElement("QuestUI", canvas, new Vector2(1400, 850), Vector2.zero);
        questPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        QuestUI questUI = questPanel.AddComponent<QuestUI>();

        TextMeshProUGUI qTitle = CreateTMPText("TitleText", questPanel, "DAILY & MILESTONE QUESTS", 26, TextAlignmentOptions.Center);
        SetAnchor(qTitle.gameObject, AnchorType.Top, new Vector2(0, -30), new Vector2(600, 60));
        Button qClose = CreateButton("CloseButton", questPanel, "X", new Vector2(60, 60), new Vector2(650, 380));
        Button dailyTab = CreateButton("DailyTabButton", questPanel, "DAILY QUESTS", new Vector2(250, 60), new Vector2(-300, 310));
        Button milestoneTab = CreateButton("MilestoneTabButton", questPanel, "MILESTONES", new Vector2(250, 60), new Vector2(300, 310));
        TextMeshProUGUI resetTimer = CreateTMPText("DailyResetTimer", questPanel, "Resets in 23:59:59", 16, TextAlignmentOptions.Center);
        SetAnchor(resetTimer.gameObject, AnchorType.Top, new Vector2(0, -90), new Vector2(300, 40));
        Transform questContent = CreateScrollView("QuestScrollRect", questPanel, new Vector2(1300, 520), new Vector2(0, -40));
        questContent.gameObject.AddComponent<VerticalLayoutGroup>();
        Button claimAll = CreateButton("ClaimAllButton", questPanel, "CLAIM ALL", new Vector2(300, 65), new Vector2(0, -380));
        TextMeshProUGUI claimAllLabel = CreateTMPText("ClaimButtonLabel", claimAll.gameObject, "CLAIM ALL READY", 16, TextAlignmentOptions.Center);

        GameObject flashPanel = CreateUIElement("rewardFlashPanel", questPanel, new Vector2(1400, 850), Vector2.zero);
        flashPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        flashPanel.AddComponent<CanvasGroup>();
        TextMeshProUGUI flashText = CreateTMPText("rewardFlashText", flashPanel, "Rewards Claimed!", 28, TextAlignmentOptions.Center);
        flashPanel.SetActive(false);
        questPanel.SetActive(false); // Hide the quest panel by default so it's not active in the Editor!

        // Wire LobbyUI references
        SerializedObject lobbySo = new SerializedObject(lobby);
        lobbySo.FindProperty("txt_MasterName").objectReferenceValue = masterName;
        lobbySo.FindProperty("txt_PlayerLevel").objectReferenceValue = playerLevel;
        lobbySo.FindProperty("txt_Gold").objectReferenceValue = gold;
        lobbySo.FindProperty("txt_Gems").objectReferenceValue = gems;
        lobbySo.FindProperty("txt_Stamina").objectReferenceValue = stamina;
        lobbySo.FindProperty("txt_StaminaTimer").objectReferenceValue = staminaTimer;
        lobbySo.FindProperty("txt_Essence").objectReferenceValue = essence;
        lobbySo.FindProperty("alertBanner").objectReferenceValue = banner;
        lobbySo.FindProperty("txt_AlertMessage").objectReferenceValue = alertMsg;
        lobbySo.FindProperty("txt_SquadTitle").objectReferenceValue = squadTitle;
        lobbySo.FindProperty("txt_SquadPower").objectReferenceValue = squadPower;
        lobbySo.FindProperty("squadSlotRow").objectReferenceValue = slotRow.transform;
        lobbySo.FindProperty("squadSlotCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/HeroCard.prefab");
        lobbySo.FindProperty("txt_FloorProgress").objectReferenceValue = floorProgress;
        lobbySo.FindProperty("img_TowerFill").objectReferenceValue = fillObj.GetComponent<Image>();
        lobbySo.FindProperty("btn_EnterTower").objectReferenceValue = enterBtn;
        lobbySo.FindProperty("btn_NavSummon").objectReferenceValue = summonBtn;
        lobbySo.FindProperty("btn_NavRoster").objectReferenceValue = rosterBtn;
        lobbySo.FindProperty("btn_NavSynthesis").objectReferenceValue = synthesisBtn;
        lobbySo.FindProperty("btn_NavMemorial").objectReferenceValue = memorialBtn;
        lobbySo.FindProperty("btn_NavInventory").objectReferenceValue = inventoryBtn;
        lobbySo.FindProperty("btn_NavFacilities").objectReferenceValue = facilitiesBtn;
        lobbySo.FindProperty("questUIPanel").objectReferenceValue = questPanel;
        lobbySo.FindProperty("questNavButton").objectReferenceValue = questNavBtn;
        lobbySo.FindProperty("questBadgeText").objectReferenceValue = questBadge;
        lobbySo.FindProperty("txt_BottomStat1").objectReferenceValue = stat1;
        lobbySo.FindProperty("txt_BottomStat2").objectReferenceValue = stat2;
        lobbySo.FindProperty("txt_BottomStat3").objectReferenceValue = stat3;
        lobbySo.ApplyModifiedProperties();

        // Wire QuestUI references
        SerializedObject questSo = new SerializedObject(questUI);
        questSo.FindProperty("titleText").objectReferenceValue = qTitle;
        questSo.FindProperty("closeButton").objectReferenceValue = qClose;
        questSo.FindProperty("dailyTabButton").objectReferenceValue = dailyTab;
        questSo.FindProperty("milestoneTabButton").objectReferenceValue = milestoneTab;
        questSo.FindProperty("dailyResetTimerText").objectReferenceValue = resetTimer;
        questSo.FindProperty("questContent").objectReferenceValue = questContent;
        questSo.FindProperty("questRowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/QuestRow.prefab");
        questSo.FindProperty("claimAllButton").objectReferenceValue = claimAll;
        questSo.FindProperty("claimAllLabel").objectReferenceValue = claimAllLabel;
        questSo.FindProperty("rewardFlashPanel").objectReferenceValue = flashPanel;
        questSo.FindProperty("rewardFlashText").objectReferenceValue = flashText;
        questSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateRosterScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("Roster");
        RosterUI roster = canvas.AddComponent<RosterUI>();

        // TopBar (stretched to full width 1920 with premium dark glass background)
        GameObject topBar = CreateUIElement("TopBar", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        topBar.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 0.95f);

        // Back Button (Pinned perfectly to Left edge)
        Button backBtn = CreateButton("btn_Back", topBar, "Back", new Vector2(120, 60), Vector2.zero);
        SetAnchor(backBtn.gameObject, AnchorType.Left, new Vector2(40, 0), new Vector2(120, 60));

        // Filter Buttons (Placed beautifully in the center of TopBar, zero overlap!)
        GameObject filters = CreateUIElement("Filters", topBar, new Vector2(900, 80), Vector2.zero);
        SetAnchor(filters.gameObject, AnchorType.Center, new Vector2(0, 0), new Vector2(900, 80));
        Button filterAll = CreateButton("btn_FilterAll", filters, "ALL HEROES", new Vector2(250, 60), new Vector2(-280, 0));
        Button filterActive = CreateButton("btn_FilterActive", filters, "ACTIVE", new Vector2(250, 60), new Vector2(0, 0));
        Button filterDead = CreateButton("btn_FilterDead", filters, "FALLEN", new Vector2(250, 60), new Vector2(280, 0));

        // Resources (Pinned perfectly to Right edge)
        TextMeshProUGUI gold = CreateTMPText("txt_Gold", topBar, "100,000", 20, TextAlignmentOptions.Left);
        SetAnchor(gold.gameObject, AnchorType.Right, new Vector2(-300, 0), new Vector2(200, 60));
        TextMeshProUGUI gems = CreateTMPText("txt_Gems", topBar, "50,000", 20, TextAlignmentOptions.Left);
        SetAnchor(gems.gameObject, AnchorType.Right, new Vector2(-80, 0), new Vector2(200, 60));

        // Left Side: Hero Grid Scroll (wider in landscape)
        Transform gridContent = CreateScrollView("HeroGrid", canvas, new Vector2(1050, 740), new Vector2(-380, -60));
        gridContent.gameObject.AddComponent<GridLayoutGroup>().cellSize = new Vector2(160, 200);

        // Right Side: Docked Detail Panel (fits landscape beautifully)
        GameObject detail = CreateUIElement("DetailPanel", canvas, new Vector2(650, 820), new Vector2(530, -50));
        detail.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        DetailPanelUI detailUI = detail.AddComponent<DetailPanelUI>();

        RawImage detailPortrait = CreateUIElement("img_Portrait", detail, new Vector2(220, 280), new Vector2(-180, 220)).AddComponent<RawImage>();
        TextMeshProUGUI detailName = CreateTMPText("txt_Name", detail, "Hero Name", 28, TextAlignmentOptions.Left);
        SetAnchor(detailName.gameObject, AnchorType.Center, new Vector2(120, 300), new Vector2(300, 60));
        TextMeshProUGUI detailStars = CreateTMPText("txt_Stars", detail, "*****", 20, TextAlignmentOptions.Left);
        SetAnchor(detailStars.gameObject, AnchorType.Center, new Vector2(120, 240), new Vector2(300, 50));
        TextMeshProUGUI detailClass = CreateTMPText("txt_Class", detail, "Class: Warrior", 20, TextAlignmentOptions.Left);
        SetAnchor(detailClass.gameObject, AnchorType.Center, new Vector2(120, 180), new Vector2(300, 50));

        // Stats grid on detail panel
        TextMeshProUGUI detailLevel = CreateTMPText("txt_Level", detail, "Lv. 1", 18, TextAlignmentOptions.Left);
        SetAnchor(detailLevel.gameObject, AnchorType.Center, new Vector2(-180, 20), new Vector2(250, 40));
        TextMeshProUGUI detailHP = CreateTMPText("txt_HP", detail, "HP: 100/100", 18, TextAlignmentOptions.Left);
        SetAnchor(detailHP.gameObject, AnchorType.Center, new Vector2(-180, -30), new Vector2(250, 40));
        TextMeshProUGUI detailATK = CreateTMPText("txt_ATK", detail, "ATK: 15", 18, TextAlignmentOptions.Left);
        SetAnchor(detailATK.gameObject, AnchorType.Center, new Vector2(-180, -80), new Vector2(250, 40));
        TextMeshProUGUI detailDEF = CreateTMPText("txt_DEF", detail, "DEF: 5", 18, TextAlignmentOptions.Left);
        SetAnchor(detailDEF.gameObject, AnchorType.Center, new Vector2(-180, -130), new Vector2(250, 40));
        TextMeshProUGUI detailSPD = CreateTMPText("txt_SPD", detail, "SPD: 10", 18, TextAlignmentOptions.Left);
        SetAnchor(detailSPD.gameObject, AnchorType.Center, new Vector2(-180, -180), new Vector2(250, 40));

        TextMeshProUGUI detailMorale = CreateTMPText("txt_Morale", detail, "Morale: 100", 18, TextAlignmentOptions.Left);
        SetAnchor(detailMorale.gameObject, AnchorType.Center, new Vector2(120, 20), new Vector2(300, 40));
        TextMeshProUGUI detailTrait = CreateTMPText("txt_Trait", detail, "Trait: Loyal", 18, TextAlignmentOptions.Left);
        SetAnchor(detailTrait.gameObject, AnchorType.Center, new Vector2(120, -30), new Vector2(300, 40));
        TextMeshProUGUI detailStatus = CreateTMPText("txt_Status", detail, "ACTIVE", 20, TextAlignmentOptions.Left);
        SetAnchor(detailStatus.gameObject, AnchorType.Center, new Vector2(120, -80), new Vector2(300, 40));

        TextMeshProUGUI detailHistory = CreateTMPText("txt_History", detail, "History info", 16, TextAlignmentOptions.Left);
        SetAnchor(detailHistory.gameObject, AnchorType.Center, new Vector2(0, -270), new Vector2(580, 100));

        Button closeDetail = CreateButton("btn_Close", detail, "CLOSE DETAIL", new Vector2(250, 60), new Vector2(0, -350));

        // Wire DetailPanelUI references programmatically
        SerializedObject detailSo = new SerializedObject(detailUI);
        detailSo.FindProperty("Portrait").objectReferenceValue = detailPortrait;
        detailSo.FindProperty("txt_NameText").objectReferenceValue = detailName;
        detailSo.FindProperty("txt_LevelXP").objectReferenceValue = detailLevel;
        detailSo.FindProperty("btn_Close").objectReferenceValue = closeDetail;
        detailSo.ApplyModifiedProperties();

        detail.SetActive(false);

        // Wire RosterUI references
        SerializedObject rosterSo = new SerializedObject(roster);
        rosterSo.FindProperty("heroGridContent").objectReferenceValue = gridContent;
        rosterSo.FindProperty("heroCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/HeroCard.prefab");
        rosterSo.FindProperty("detailPanel").objectReferenceValue = detail;
        rosterSo.FindProperty("txt_Gold").objectReferenceValue = gold;
        rosterSo.FindProperty("txt_Gems").objectReferenceValue = gems;
        rosterSo.FindProperty("btn_FilterAll").objectReferenceValue = filterAll;
        rosterSo.FindProperty("btn_FilterActive").objectReferenceValue = filterActive;
        rosterSo.FindProperty("btn_FilterFallen").objectReferenceValue = filterDead;
        rosterSo.ApplyModifiedProperties();

        // Wire buttons clicks using standard Unity Events in code
        closeDetail.onClick.AddListener(roster.CloseDetail);
        backBtn.onClick.AddListener(roster.OnBackButton);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateSummonScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("Summon");
        SummonUI summon = canvas.AddComponent<SummonUI>();

        // Top Bar
        GameObject topBar = CreateUIElement("TopBar", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        TextMeshProUGUI gems = CreateTMPText("txt_Gems", topBar, "500", 20, TextAlignmentOptions.Right);
        SetAnchor(gems.gameObject, AnchorType.Right, new Vector2(-180, 0), new Vector2(250, 80));
        TextMeshProUGUI gold = CreateTMPText("txt_Gold", topBar, "1,000", 20, TextAlignmentOptions.Left);
        SetAnchor(gold.gameObject, AnchorType.Left, new Vector2(180, 0), new Vector2(250, 80));
        Button backBtn = CreateButton("btn_Back", topBar, "Back", new Vector2(120, 60), new Vector2(-880, 0));

        // Pull Buttons Panel
        GameObject summonPanel = CreateUIElement("SummonPanel", canvas, new Vector2(1000, 350), new Vector2(0, -100));
        Button pull1x = CreateButton("btn_Pull1x", summonPanel, "SUMMON 1X", new Vector2(300, 80), new Vector2(-220, 50));
        Button pull10x = CreateButton("btn_Pull10x", summonPanel, "SUMMON 10X", new Vector2(300, 80), new Vector2(220, 50));

        // Pity Bar
        GameObject pityBar = CreateUIElement("PityBar", summonPanel, new Vector2(800, 30), new Vector2(0, -80));
        GameObject pityFillObj = new GameObject("img_PityFill", typeof(RectTransform), typeof(Image));
        pityFillObj.transform.SetParent(pityBar.transform, false);
        pityFillObj.GetComponent<Image>().type = Image.Type.Filled;
        pityFillObj.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        TextMeshProUGUI pityText = CreateTMPText("txt_PityCount", pityBar, "0 / 90", 14, TextAlignmentOptions.Center);

        // Result Panel overlay (Landscape size)
        GameObject results = CreateUIElement("ResultPanel", canvas, new Vector2(1400, 850), Vector2.zero);
        results.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        Transform resultGrid = CreateScrollView("ResultGrid", results, new Vector2(1200, 550), new Vector2(0, 80));
        resultGrid.gameObject.AddComponent<GridLayoutGroup>().cellSize = new Vector2(160, 200);
        TextMeshProUGUI summary = CreateTMPText("txt_ResultSummary", results, "Summary info", 20, TextAlignmentOptions.Center);
        SetAnchor(summary.gameObject, AnchorType.Bottom, new Vector2(0, -250), new Vector2(800, 50));
        Button closeResults = CreateButton("btn_CloseResults", results, "CONTINUE", new Vector2(300, 70), new Vector2(-200, -360));
        Button summonAgain = CreateButton("btn_SummonAgain", results, "SUMMON AGAIN", new Vector2(300, 70), new Vector2(200, -360));
        results.SetActive(false);

        // Notification Toast
        TextMeshProUGUI notif = CreateTMPText("NotificationText", canvas, "Alert!", 22, TextAlignmentOptions.Center);
        notif.gameObject.SetActive(false);

        // Wire SummonUI references
        SerializedObject summonSo = new SerializedObject(summon);
        summonSo.FindProperty("txt_Gems").objectReferenceValue = gems;
        summonSo.FindProperty("txt_Gold").objectReferenceValue = gold;
        summonSo.FindProperty("btn_Pull1x").objectReferenceValue = pull1x;
        summonSo.FindProperty("btn_Pull10x").objectReferenceValue = pull10x;
        summonSo.FindProperty("img_PityFill").objectReferenceValue = pityFillObj.GetComponent<Image>();
        summonSo.FindProperty("txt_PityCount").objectReferenceValue = pityText;
        summonSo.FindProperty("resultPanel").objectReferenceValue = results;
        summonSo.FindProperty("resultGridContent").objectReferenceValue = resultGrid;
        summonSo.FindProperty("resultCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/ResultCard.prefab");
        summonSo.FindProperty("txt_ResultSummary").objectReferenceValue = summary;
        summonSo.FindProperty("txt_Notification").objectReferenceValue = notif;
        summonSo.ApplyModifiedProperties();

        // Wire button actions in code
        backBtn.onClick.AddListener(summon.OnBackButton);
        closeResults.onClick.AddListener(summon.OnCloseResults);
        summonAgain.onClick.AddListener(summon.OnSummonAgain);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateSquadFormationScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("SquadFormation");
        SquadFormationUI squadUI = canvas.AddComponent<SquadFormationUI>();

        // TopBar (stretched to full width 1920)
        GameObject topBar = CreateUIElement("TopBar", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        TextMeshProUGUI title = CreateTMPText("txt_Title", topBar, "SQUAD DEPLOYMENT", 26, TextAlignmentOptions.Center);
        Button backBtn = CreateButton("btn_Back", topBar, "Back", new Vector2(120, 60), new Vector2(-880, 0));

        // ─── Left Side: Roster Selection Grid (Wider scroll in landscape) ───
        Transform rosterGrid = CreateScrollView("RosterPanel", canvas, new Vector2(980, 760), new Vector2(-420, -80));
        rosterGrid.gameObject.AddComponent<GridLayoutGroup>().cellSize = new Vector2(160, 200);

        // ─── Right Side: Squad Deployment Slots & Controls ───
        GameObject squadPanel = CreateUIElement("SquadPanel", canvas, new Vector2(800, 760), new Vector2(490, -80));
        squadPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.5f);

        // Floor Selector (docked on top right)
        GameObject floorPanel = CreateUIElement("FloorSelectorPanel", squadPanel, new Vector2(740, 180), new Vector2(0, 250));
        Button prevFloor = CreateButton("btn_PrevFloor", floorPanel, "◀", new Vector2(80, 60), new Vector2(-280, 20));
        Button nextFloor = CreateButton("btn_NextFloor", floorPanel, "▶", new Vector2(80, 60), new Vector2(280, 20));
        TextMeshProUGUI floorLabel = CreateTMPText("txt_FloorLabel", floorPanel, "FLOOR 1", 24, TextAlignmentOptions.Center);
        TextMeshProUGUI enemyInfo = CreateTMPText("txt_EnemyInfo", floorPanel, "Enemies: None", 16, TextAlignmentOptions.Center);
        SetAnchor(enemyInfo.gameObject, AnchorType.Bottom, new Vector2(0, 15), new Vector2(600, 40));

        // Horizontal layout row for the 5 squad slots
        Button[] slots = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            float posX = -280f + (i * 140f);
            slots[i] = CreateButton("SquadSlot_" + i, squadPanel, "EMPTY", new Vector2(120, 160), new Vector2(posX, 0));
            // add sub-objects
            CreateUIElement("img_Portrait", slots[i].gameObject, new Vector2(120, 160), Vector2.zero).AddComponent<Image>();
            CreateTMPText("txt_Name", slots[i].gameObject, "", 12, TextAlignmentOptions.Bottom);
            CreateUIElement("obj_EmptySlot", slots[i].gameObject, new Vector2(120, 160), Vector2.zero);
        }
        TextMeshProUGUI totalPower = CreateTMPText("txt_TotalPower", squadPanel, "Total Power: 0", 20, TextAlignmentOptions.Center);
        SetAnchor(totalPower.gameObject, AnchorType.Bottom, new Vector2(0, 150), new Vector2(500, 40));
        Button startBattle = CreateButton("btn_StartBattle", squadPanel, "START AUTO-BATTLE", new Vector2(400, 75), new Vector2(0, -240));

        // Wire SquadFormationUI references
        SerializedObject squadSo = new SerializedObject(squadUI);
        squadSo.FindProperty("btn_PrevFloor").objectReferenceValue = prevFloor;
        squadSo.FindProperty("btn_NextFloor").objectReferenceValue = nextFloor;
        squadSo.FindProperty("txt_FloorLabel").objectReferenceValue = floorLabel;
        squadSo.FindProperty("txt_EnemyInfo").objectReferenceValue = enemyInfo;
        squadSo.FindProperty("rosterGridContent").objectReferenceValue = rosterGrid;
        squadSo.FindProperty("rosterCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/HeroCard.prefab");

        SerializedProperty slotsProp = squadSo.FindProperty("squadSlots");
        slotsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        squadSo.FindProperty("txt_TotalPower").objectReferenceValue = totalPower;
        squadSo.FindProperty("btn_StartBattle").objectReferenceValue = startBattle;
        squadSo.ApplyModifiedProperties();

        // Wire back button in code
        backBtn.onClick.AddListener(squadUI.OnBackButton);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateBattleScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // _BattleManager
        GameObject bmObj = new GameObject("_BattleManager");
        CombatManager cm = bmObj.AddComponent<CombatManager>();

        var (canvas, _) = CreateCanvasWithEventSystem("Battle");
        BattleUI battleUI = canvas.AddComponent<BattleUI>();

        // BattleView Panel (wider spacing across landscape screen)
        GameObject viewPanel = CreateUIElement("BattleView", canvas, new Vector2(1800, 580), new Vector2(0, 140));
        GameObject heroRow = CreateUIElement("HeroRow", viewPanel, new Vector2(650, 450), new Vector2(-480, 20));
        heroRow.AddComponent<HorizontalLayoutGroup>().childControlWidth = false;
        GameObject enemyRow = CreateUIElement("EnemyRow", viewPanel, new Vector2(650, 450), new Vector2(480, 20));
        enemyRow.AddComponent<HorizontalLayoutGroup>().childControlWidth = false;
        TextMeshProUGUI turnCounter = CreateTMPText("txt_TurnCounter", viewPanel, "TURN 1", 24, TextAlignmentOptions.Center);
        SetAnchor(turnCounter.gameObject, AnchorType.Top, new Vector2(0, -10), new Vector2(300, 50));

        // Speed buttons Stacked dynamically in bottom center of battle panel
        GameObject speedPanel = CreateUIElement("SpeedButtons", viewPanel, new Vector2(300, 80), new Vector2(0, -220));
        Button speed1x = CreateButton("btn_Speed1x", speedPanel, "1X", new Vector2(80, 60), new Vector2(-90, 0));
        Button speed2x = CreateButton("btn_Speed2x", speedPanel, "2X", new Vector2(80, 60), new Vector2(0, 0));
        Button speed3x = CreateButton("btn_Speed3x", speedPanel, "3X", new Vector2(80, 60), new Vector2(90, 0));

        // Bottom Side: Combat Log
        Transform logContent = CreateScrollView("CombatLogPanel", canvas, new Vector2(1000, 250), new Vector2(0, -320));
        TextMeshProUGUI logText = CreateTMPText("txt_CombatLog", logContent.gameObject, "Battle simulation starting...", 14, TextAlignmentOptions.Left);
        logText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect logScroll = logContent.parent.parent.GetComponent<ScrollRect>();

        // Result Panel overlay (Landscape size)
        GameObject results = CreateUIElement("ResultPanel", canvas, new Vector2(1100, 750), Vector2.zero);
        results.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.95f);
        TextMeshProUGUI titleText = CreateTMPText("txt_ResultTitle", results, "VICTORY", 38, TextAlignmentOptions.Center);
        SetAnchor(titleText.gameObject, AnchorType.Top, new Vector2(0, -50), new Vector2(500, 70));
        TextMeshProUGUI survivors = CreateTMPText("txt_Survivors", results, "Survivors", 20, TextAlignmentOptions.Center);
        SetAnchor(survivors.gameObject, AnchorType.Center, new Vector2(0, 100), new Vector2(800, 50));
        TextMeshProUGUI fallen = CreateTMPText("txt_Fallen", results, "Fallen", 20, TextAlignmentOptions.Center);
        SetAnchor(fallen.gameObject, AnchorType.Center, new Vector2(0, 20), new Vector2(800, 50));
        TextMeshProUGUI gold = CreateTMPText("txt_GoldEarned", results, "Gold Earned", 20, TextAlignmentOptions.Center);
        SetAnchor(gold.gameObject, AnchorType.Center, new Vector2(0, -60), new Vector2(800, 50));
        Button continueBtn = CreateButton("btn_Continue", results, "CONTINUE", new Vector2(300, 70), new Vector2(0, -250));
        results.SetActive(false);

        // Wire BattleUI references
        SerializedObject battleSo = new SerializedObject(battleUI);
        battleSo.FindProperty("heroRow").objectReferenceValue = heroRow.transform;
        battleSo.FindProperty("enemyRow").objectReferenceValue = enemyRow.transform;
        battleSo.FindProperty("txt_TurnCounter").objectReferenceValue = turnCounter;
        battleSo.FindProperty("unitCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/UnitBattleCard.prefab");
        battleSo.FindProperty("btn_Speed1x").objectReferenceValue = speed1x;
        battleSo.FindProperty("btn_Speed2x").objectReferenceValue = speed2x;
        battleSo.FindProperty("btn_Speed3x").objectReferenceValue = speed3x;
        battleSo.FindProperty("txt_CombatLog").objectReferenceValue = logText;
        battleSo.FindProperty("logScrollRect").objectReferenceValue = logScroll;
        battleSo.FindProperty("resultPanel").objectReferenceValue = results;
        battleSo.FindProperty("txt_ResultTitle").objectReferenceValue = titleText;
        battleSo.FindProperty("txt_Survivors").objectReferenceValue = survivors;
        battleSo.FindProperty("txt_Fallen").objectReferenceValue = fallen;
        battleSo.FindProperty("txt_GoldEarned").objectReferenceValue = gold;
        battleSo.ApplyModifiedProperties();

        // Wire continue button click in code
        continueBtn.onClick.AddListener(battleUI.OnContinueButton);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateMemorialScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("Memorial");
        MemorialUI memorial = canvas.AddComponent<MemorialUI>();

        // Header
        GameObject header = CreateUIElement("Header", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        TextMeshProUGUI title = CreateTMPText("TitleText", header, "VALHALLA MEMORIAL", 26, TextAlignmentOptions.Center);
        TextMeshProUGUI essence = CreateTMPText("essenceCounterText", header, "Essence: 0", 20, TextAlignmentOptions.Right);
        SetAnchor(essence.gameObject, AnchorType.Right, new Vector2(-180, 0), new Vector2(250, 80));
        Button backBtn = CreateButton("backButton", header, "Back", new Vector2(120, 60), new Vector2(-880, 0));

        // Left Side: Graves List Grid
        Transform grid = CreateScrollView("Grid", canvas, new Vector2(1050, 800), new Vector2(-380, -60));
        grid.gameObject.AddComponent<GridLayoutGroup>().cellSize = new Vector2(160, 200);

        // Right Side: Graves Detail view sheet (Fits beautifully)
        GameObject detail = CreateUIElement("DetailPanel", canvas, new Vector2(650, 820), new Vector2(530, -50));
        detail.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.98f);
        TextMeshProUGUI detName = CreateTMPText("heroNameText", detail, "Hero Name", 30, TextAlignmentOptions.Center);
        SetAnchor(detName.gameObject, AnchorType.Top, new Vector2(0, -60), new Vector2(580, 60));
        TextMeshProUGUI detStars = CreateTMPText("starRatingText", detail, "*****", 22, TextAlignmentOptions.Center);
        SetAnchor(detStars.gameObject, AnchorType.Top, new Vector2(0, -120), new Vector2(580, 50));
        TextMeshProUGUI detClass = CreateTMPText("classText", detail, "Warrior", 20, TextAlignmentOptions.Center);
        SetAnchor(detClass.gameObject, AnchorType.Top, new Vector2(0, -170), new Vector2(580, 50));
        TextMeshProUGUI detEpitaph = CreateTMPText("epitaphText", detail, "\"Beloved hero\"", 18, TextAlignmentOptions.Center);
        SetAnchor(detEpitaph.gameObject, AnchorType.Top, new Vector2(0, -220), new Vector2(580, 50));
        TextMeshProUGUI detHistory = CreateTMPText("historyText", detail, "Combat achievements logs", 16, TextAlignmentOptions.Left);
        SetAnchor(detHistory.gameObject, AnchorType.Center, new Vector2(0, -100), new Vector2(580, 200));

        Button extractBtn = CreateButton("extractButton", detail, "EXTRACT SOUL ESSENCE", new Vector2(350, 70), new Vector2(0, -250));
        TextMeshProUGUI extractLabel = CreateTMPText("extractButtonLabel", extractBtn.gameObject, "EXTRACT ESSENCE", 16, TextAlignmentOptions.Center);
        GameObject extractedLabel = CreateUIElement("extractedLabel", detail, new Vector2(300, 60), new Vector2(0, -250));
        CreateTMPText("Text", extractedLabel, "SOUL EXTRACTED", 18, TextAlignmentOptions.Center);
        extractedLabel.SetActive(false);
        Button closeDetail = CreateButton("closeDetailButton", detail, "CLOSE DETAIL", new Vector2(250, 60), new Vector2(0, -340));
        detail.SetActive(false);

        // Confirm Extract Overlay Panel (centered pop-up)
        GameObject confirm = CreateUIElement("ConfirmPanel", canvas, new Vector2(800, 420), Vector2.zero);
        confirm.AddComponent<Image>().color = new Color(0.12f, 0.05f, 0.05f, 0.98f);
        TextMeshProUGUI confirmText = CreateTMPText("confirmText", confirm, "Are you sure you want to extract soul?", 20, TextAlignmentOptions.Center);
        SetAnchor(confirmText.gameObject, AnchorType.Center, new Vector2(0, 80), new Vector2(700, 100));
        Button yesBtn = CreateButton("confirmYesButton", confirm, "YES, EXTRACT", new Vector2(250, 65), new Vector2(-150, -80));
        Button noBtn = CreateButton("confirmNoButton", confirm, "NO, CANCEL", new Vector2(250, 65), new Vector2(150, -80));
        confirm.SetActive(false);

        // Wire MemorialUI references
        SerializedObject memorialSo = new SerializedObject(memorial);
        memorialSo.FindProperty("titleText").objectReferenceValue = title;
        memorialSo.FindProperty("essenceCounterText").objectReferenceValue = essence;
        memorialSo.FindProperty("gridContent").objectReferenceValue = grid;
        memorialSo.FindProperty("tombstoneCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/TombstoneCard.prefab");
        memorialSo.FindProperty("detailPanel").objectReferenceValue = detail;
        memorialSo.FindProperty("heroNameText").objectReferenceValue = detName;
        memorialSo.FindProperty("starRatingText").objectReferenceValue = detStars;
        memorialSo.FindProperty("classText").objectReferenceValue = detClass;
        memorialSo.FindProperty("epitaphText").objectReferenceValue = detEpitaph;
        memorialSo.FindProperty("historyText").objectReferenceValue = detHistory;
        memorialSo.FindProperty("extractButton").objectReferenceValue = extractBtn;
        memorialSo.FindProperty("extractButtonLabel").objectReferenceValue = extractLabel;
        memorialSo.FindProperty("extractedLabel").objectReferenceValue = extractedLabel;
        memorialSo.FindProperty("closeDetailButton").objectReferenceValue = closeDetail;
        memorialSo.FindProperty("confirmExtractPanel").objectReferenceValue = confirm;
        memorialSo.FindProperty("confirmText").objectReferenceValue = confirmText;
        memorialSo.FindProperty("confirmYesButton").objectReferenceValue = yesBtn;
        memorialSo.FindProperty("confirmNoButton").objectReferenceValue = noBtn;
        memorialSo.FindProperty("backButton").objectReferenceValue = backBtn;
        memorialSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CreateSynthesisScene(string path)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var (canvas, _) = CreateCanvasWithEventSystem("Synthesis");
        SynthesisUI synth = canvas.AddComponent<SynthesisUI>();

        // Header
        GameObject header = CreateUIElement("Header", canvas, new Vector2(1920, 120), new Vector2(0, 480));
        TextMeshProUGUI essence = CreateTMPText("essenceCounterText", header, "Essence: 0", 20, TextAlignmentOptions.Right);
        SetAnchor(essence.gameObject, AnchorType.Right, new Vector2(-180, 0), new Vector2(250, 80));
        Button backBtn = CreateButton("backButton", header, "Back", new Vector2(120, 60), new Vector2(-880, 0));

        // Left Panel: Target and Sacrifice Slots (Elegantly docked in landscape)
        GameObject leftPanel = CreateUIElement("LeftPanel", canvas, new Vector2(850, 800), new Vector2(-480, -60));
        leftPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.5f);

        Button targetSlot = CreateButton("TargetSlotButton", leftPanel, "TARGET HERO", new Vector2(180, 220), new Vector2(-220, 150));
        GameObject targetCard = CreateUIElement("TargetHeroCard", targetSlot.gameObject, new Vector2(180, 220), Vector2.zero);
        TextMeshProUGUI tarName = CreateTMPText("txt_Name", targetCard, "", 12, TextAlignmentOptions.Bottom);
        TextMeshProUGUI tarStar = CreateTMPText("txt_Stars", targetCard, "", 12, TextAlignmentOptions.Top);
        TextMeshProUGUI tarClass = CreateTMPText("txt_Class", targetCard, "", 12, TextAlignmentOptions.Center);
        GameObject targetEmptyLabel = CreateUIElement("targetEmptyLabel", targetSlot.gameObject, new Vector2(180, 220), Vector2.zero);

        Button[] sacSlots = new Button[2];
        GameObject[] sacCards = new GameObject[2];
        TextMeshProUGUI[] sacNames = new TextMeshProUGUI[2];
        TextMeshProUGUI[] sacStars = new TextMeshProUGUI[2];
        GameObject[] sacEmptyLabels = new GameObject[2];

        for (int i = 0; i < 2; i++)
        {
            float posX = 100f + (i * 200f);
            sacSlots[i] = CreateButton("SacrificeSlot_" + i, leftPanel, "SACRIFICE", new Vector2(160, 200), new Vector2(posX, 150));
            sacCards[i] = CreateUIElement("SacCard", sacSlots[i].gameObject, new Vector2(160, 200), Vector2.zero);
            sacNames[i] = CreateTMPText("txt_Name", sacCards[i], "", 11, TextAlignmentOptions.Bottom);
            sacStars[i] = CreateTMPText("txt_Stars", sacCards[i], "", 11, TextAlignmentOptions.Top);
            sacEmptyLabels[i] = CreateUIElement("sacEmptyLabel", sacSlots[i].gameObject, new Vector2(160, 200), Vector2.zero);
        }

        TextMeshProUGUI costText = CreateTMPText("txt_Cost", leftPanel, "0 Essence Cost", 20, TextAlignmentOptions.Center);
        SetAnchor(costText.gameObject, AnchorType.Center, new Vector2(0, -60), new Vector2(500, 50));
        TextMeshProUGUI valText = CreateTMPText("txt_Validation", leftPanel, "Requirements not met", 16, TextAlignmentOptions.Center);
        SetAnchor(valText.gameObject, AnchorType.Center, new Vector2(0, -130), new Vector2(600, 50));
        Button synthBtn = CreateButton("btn_Synthesize", leftPanel, "FUSE & ASCEND HERO", new Vector2(400, 75), new Vector2(0, -260));

        // Right Panel: Roster Selection Grid (Fits scroll picker on the right)
        GameObject rightPanel = CreateUIElement("RightPanel", canvas, new Vector2(900, 800), new Vector2(450, -60));
        rightPanel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.5f);
        Transform grid = CreateScrollView("RosterPanel", rightPanel, new Vector2(840, 640), new Vector2(0, -40));
        grid.gameObject.AddComponent<GridLayoutGroup>().cellSize = new Vector2(160, 200);
        TextMeshProUGUI hintText = CreateTMPText("gridHintText", rightPanel, "Select Target Hero first", 16, TextAlignmentOptions.Center);
        SetAnchor(hintText.gameObject, AnchorType.Top, new Vector2(0, -20), new Vector2(700, 50));

        // Result Panel overlay
        GameObject results = CreateUIElement("ResultPanel", canvas, new Vector2(1100, 750), Vector2.zero);
        results.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.04f, 0.98f);
        TextMeshProUGUI resTitle = CreateTMPText("resultTitleText", results, "Ascension Successful!", 34, TextAlignmentOptions.Center);
        SetAnchor(resTitle.gameObject, AnchorType.Top, new Vector2(0, -60), new Vector2(800, 70));
        TextMeshProUGUI resBody = CreateTMPText("resultBodyText", results, "ascended to higher tier!", 22, TextAlignmentOptions.Center);
        SetAnchor(resBody.gameObject, AnchorType.Center, new Vector2(0, 40), new Vector2(800, 100));
        TextMeshProUGUI resStar = CreateTMPText("resultStarText", results, "*****", 26, TextAlignmentOptions.Center);
        SetAnchor(resStar.gameObject, AnchorType.Center, new Vector2(0, -60), new Vector2(400, 50));
        Button closeResults = CreateButton("resultCloseButton", results, "CONTINUE", new Vector2(300, 70), new Vector2(0, -250));
        results.SetActive(false);

        // Wire SynthesisUI references
        SerializedObject synthSo = new SerializedObject(synth);
        synthSo.FindProperty("essenceCounterText").objectReferenceValue = essence;
        synthSo.FindProperty("targetSlotButton").objectReferenceValue = targetSlot;
        synthSo.FindProperty("targetHeroCard").objectReferenceValue = targetCard;
        synthSo.FindProperty("targetNameText").objectReferenceValue = tarName;
        synthSo.FindProperty("targetStarText").objectReferenceValue = tarStar;
        synthSo.FindProperty("targetClassText").objectReferenceValue = tarClass;
        synthSo.FindProperty("targetEmptyLabel").objectReferenceValue = targetEmptyLabel;

        SerializedProperty sacSlotsProp = synthSo.FindProperty("sacrificeSlotButtons");
        sacSlotsProp.arraySize = 2;
        SerializedProperty sacCardsProp = synthSo.FindProperty("sacrificeCards");
        sacCardsProp.arraySize = 2;
        SerializedProperty sacNamesProp = synthSo.FindProperty("sacNameTexts");
        sacNamesProp.arraySize = 2;
        SerializedProperty sacStarsProp = synthSo.FindProperty("sacStarTexts");
        sacStarsProp.arraySize = 2;
        SerializedProperty sacEmptiesProp = synthSo.FindProperty("sacEmptyLabels");
        sacEmptiesProp.arraySize = 2;

        for (int i = 0; i < 2; i++)
        {
            sacSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = sacSlots[i];
            sacCardsProp.GetArrayElementAtIndex(i).objectReferenceValue = sacCards[i];
            sacNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = sacNames[i];
            sacStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = sacStars[i];
            sacEmptiesProp.GetArrayElementAtIndex(i).objectReferenceValue = sacEmptyLabels[i];
        }

        synthSo.FindProperty("costText").objectReferenceValue = costText;
        synthSo.FindProperty("validationText").objectReferenceValue = valText;
        synthSo.FindProperty("synthesizeButton").objectReferenceValue = synthBtn;
        synthSo.FindProperty("gridContent").objectReferenceValue = grid;
        synthSo.FindProperty("heroPickerCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "/HeroPickerCard.prefab");
        synthSo.FindProperty("gridHintText").objectReferenceValue = hintText;
        synthSo.FindProperty("resultPanel").objectReferenceValue = results;
        synthSo.FindProperty("resultTitleText").objectReferenceValue = resTitle;
        synthSo.FindProperty("resultBodyText").objectReferenceValue = resBody;
        synthSo.FindProperty("resultStarText").objectReferenceValue = resStar;
        synthSo.FindProperty("resultCloseButton").objectReferenceValue = closeResults;
        synthSo.FindProperty("backButton").objectReferenceValue = backBtn;
        synthSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, path);
    }

    // ─── Hierarchy Helpers ───────────────────────────────────────────────────

    private static (GameObject canvas, EventSystem eventSystem) CreateCanvasWithEventSystem(string sceneName)
    {
        // Canvas
        GameObject canvasObj = new GameObject(sceneName + "Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Premium Landscape standard
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es = esObj.GetComponent<EventSystem>();
        }

        return (canvasObj, es);
    }

    private static GameObject CreateUIElement(string name, GameObject parent, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return obj;
    }

    private static TextMeshProUGUI CreateTMPText(string name, GameObject parent, string text, float size, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold; // Make all standard texts bold by default
        tmp.alignment = alignment;
        tmp.color = Color.white;
        // Fill parent standard bounds
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = parent.GetComponent<RectTransform>().sizeDelta;
        rect.anchoredPosition = Vector2.zero;
        return tmp;
    }

    private static GameObject CreateTMPTextObj(GameObject parent, string name, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold; // Make all standard texts bold by default
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return obj;
    }

    private static Button CreateButton(string name, GameObject parent, string label, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        if (!string.IsNullOrEmpty(label))
        {
            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20; // Increased to 20 for superb landscape visibility
            tmp.fontStyle = FontStyles.Bold; // Bold labels
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
        }

        return obj.GetComponent<Button>();
    }

    private static Transform CreateScrollView(string name, GameObject parent, Vector2 size, Vector2 pos)
    {
        GameObject scrollObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent.transform, false);
        RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.sizeDelta = size;
        scrollRect.anchoredPosition = pos;
        Image bg = scrollObj.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.8f);

        ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;
        Image viewImg = viewport.GetComponent<Image>();
        viewImg.color = Color.white;
        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 300f);

        sr.viewport = viewRect;
        sr.content = contentRect;

        return contentRect.transform;
    }

    private enum AnchorType { Center, TopLeft, TopRight, BottomLeft, BottomRight, Top, Bottom, Left, Right, StretchAll }

    private static void SetAnchor(GameObject obj, AnchorType type, Vector2 pos, Vector2 size = default)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (size != default) rect.sizeDelta = size;

        switch (type)
        {
            case AnchorType.Center:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.TopLeft:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.TopRight:
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.BottomLeft:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.BottomRight:
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.Top:
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.Bottom:
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.Left:
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.Right:
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = pos;
                break;
            case AnchorType.StretchAll:
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                break;
        }
    }

    private static void RegisterScenesInBuildSettings()
    {
        string[] paths = {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Roster.unity",
            "Assets/Scenes/Summon.unity",
            "Assets/Scenes/SquadFormation.unity",
            "Assets/Scenes/Battle.unity",
            "Assets/Scenes/Memorial.unity",
            "Assets/Scenes/Synthesis.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string p in paths)
        {
            if (File.Exists(p))
            {
                buildScenes.Add(new EditorBuildSettingsScene(p, true));
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("🏁 Correctly registered all 8 scenes in Build Settings.");
    }
}
#endif
