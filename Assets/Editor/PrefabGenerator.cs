#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class PrefabGenerator
{
    [MenuItem("Tools/Generate UI Prefabs")]
    public static void Generate()
    {
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        GenerateHeroCard(dir);
        GenerateTombstoneCard(dir);
        GenerateHeroPickerCard(dir);
        GenerateQuestRow(dir);
        GenerateUnitBattleCard(dir);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 All 5 UI Prefabs generated successfully!");
    }

    private static void GenerateHeroCard(string dir)
    {
        GameObject root = new GameObject("HeroCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 200);
        Image bg = root.GetComponent<Image>();
        bg.color = HexColor("#1A1A2E");
        bg.raycastTarget = false;

        // StarText
        GameObject star = CreateTMPText(root, "StarText", "*****", 18, TextAlignmentOptions.TopLeft);
        SetAnchor(star, AnchorType.TopLeft, new Vector2(5, -5));

        // NameText
        GameObject nameText = CreateTMPText(root, "NameText", "Hero Name", 22, TextAlignmentOptions.Center);
        nameText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        SetAnchor(nameText, AnchorType.Center, new Vector2(0, 20));

        // ClassText
        GameObject classText = CreateTMPText(root, "ClassText", "Class", 16, TextAlignmentOptions.Center);
        classText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        classText.GetComponent<TextMeshProUGUI>().color = Color.gray;
        SetAnchor(classText, AnchorType.Center, new Vector2(0, -10));

        // MoraleBar Slider
        GameObject sliderObj = CreateSlider(root, "MoraleBar", HexColor("#550000"), HexColor("#00FF00"));
        SetAnchor(sliderObj, AnchorType.Bottom, new Vector2(0, 30), new Vector2(140, 15));

        // StatusText
        GameObject statusText = CreateTMPText(root, "StatusText", "Active", 14, TextAlignmentOptions.Bottom);
        SetAnchor(statusText, AnchorType.Bottom, new Vector2(0, 5));

        // SelectButton
        GameObject selectBtnObj = CreateTransparentButton(root, "SelectButton", new Vector2(160, 200));

        SaveAndDestroy(root, dir + "/HeroCard.prefab");
    }

    private static void GenerateTombstoneCard(string dir)
    {
        GameObject root = new GameObject("TombstoneCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 200);
        Image bg = root.GetComponent<Image>();
        bg.color = HexColor("#0D0D0D");
        bg.raycastTarget = false;

        // tmps order must be: 0=Name, 1=Stars, 2=Epitaph
        GameObject nameText = CreateTMPText(root, "NameText", "Fallen Hero", 20, TextAlignmentOptions.Center);
        nameText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        nameText.GetComponent<TextMeshProUGUI>().color = HexColor("#F5F5F5");
        SetAnchor(nameText, AnchorType.Top, new Vector2(0, -25));

        GameObject starText = CreateTMPText(root, "StarText", "*****", 16, TextAlignmentOptions.Center);
        starText.GetComponent<TextMeshProUGUI>().color = HexColor("#FFD700");
        SetAnchor(starText, AnchorType.Top, new Vector2(0, -55));

        GameObject epitaphText = CreateTMPText(root, "EpitaphText", "\"Beloved champion.\"", 13, TextAlignmentOptions.Center);
        epitaphText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        epitaphText.GetComponent<TextMeshProUGUI>().color = Color.gray;
        epitaphText.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;
        epitaphText.GetComponent<TextMeshProUGUI>().maxVisibleLines = 2;
        SetAnchor(epitaphText, AnchorType.Center, new Vector2(0, -20), new Vector2(140, 50));

        // EssenceExtractedIcon
        GameObject icon = new GameObject("EssenceExtractedIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        Image iconImg = icon.GetComponent<Image>();
        iconImg.color = HexColor("#FFFFFF");
        iconImg.raycastTarget = false;
        SetAnchor(icon, AnchorType.TopRight, new Vector2(-10, -10), new Vector2(24, 24));
        icon.SetActive(false); // hidden by default

        // SelectButton
        GameObject selectBtnObj = CreateTransparentButton(root, "SelectButton", new Vector2(160, 200));

        SaveAndDestroy(root, dir + "/TombstoneCard.prefab");
    }

    private static void GenerateHeroPickerCard(string dir)
    {
        GameObject root = new GameObject("HeroPickerCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 200);
        Image bg = root.GetComponent<Image>();
        bg.color = HexColor("#1E1E2E");
        bg.raycastTarget = false;

        // tmps order: 0=Name, 1=Stars, 2=Class
        GameObject nameText = CreateTMPText(root, "NameText", "Hero Name", 18, TextAlignmentOptions.Center);
        SetAnchor(nameText, AnchorType.Center, new Vector2(0, 30));

        GameObject starText = CreateTMPText(root, "StarText", "***", 14, TextAlignmentOptions.Center);
        starText.GetComponent<TextMeshProUGUI>().color = HexColor("#FFD700");
        SetAnchor(starText, AnchorType.Center, new Vector2(0, 5));

        GameObject classText = CreateTMPText(root, "ClassText", "Warrior", 13, TextAlignmentOptions.Center);
        classText.GetComponent<TextMeshProUGUI>().color = Color.gray;
        SetAnchor(classText, AnchorType.Center, new Vector2(0, -20));

        // RedOverlay
        GameObject overlay = new GameObject("RedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(root.transform, false);
        Image overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(1f, 0.2f, 0.2f, 0f); // default alpha 0
        overlayImg.raycastTarget = false;
        SetAnchor(overlay, AnchorType.StretchAll, Vector2.zero, new Vector2(160, 200));

        // SelectButton
        GameObject selectBtnObj = CreateTransparentButton(root, "SelectButton", new Vector2(160, 200));

        SaveAndDestroy(root, dir + "/HeroPickerCard.prefab");
    }

    private static void GenerateQuestRow(string dir)
    {
        GameObject root = new GameObject("QuestRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 120); // min height 120px
        Image bg = root.GetComponent<Image>();
        bg.color = HexColor("#16213E");
        bg.raycastTarget = false;

        // Prefab TMP order: 0=Title, 1=Description, 2=ProgressText, 3=RewardText, 4=ClaimButtonLabel
        GameObject titleText = CreateTMPText(root, "TitleText", "Quest Title", 18, TextAlignmentOptions.Left);
        titleText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        SetAnchor(titleText, AnchorType.TopLeft, new Vector2(20, -20), new Vector2(300, 25));

        GameObject descText = CreateTMPText(root, "DescriptionText", "Quest description goes here.", 13, TextAlignmentOptions.Left);
        descText.GetComponent<TextMeshProUGUI>().color = Color.gray;
        descText.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
        SetAnchor(descText, AnchorType.TopLeft, new Vector2(20, -50), new Vector2(300, 40));

        GameObject progressText = CreateTMPText(root, "ProgressText", "0 / 1", 13, TextAlignmentOptions.Right);
        SetAnchor(progressText, AnchorType.TopRight, new Vector2(-160, -20), new Vector2(100, 25));

        GameObject rewardText = CreateTMPText(root, "RewardText", "+50 Gems", 14, TextAlignmentOptions.Left);
        rewardText.GetComponent<TextMeshProUGUI>().color = HexColor("#FFD700");
        SetAnchor(rewardText, AnchorType.BottomLeft, new Vector2(20, 15), new Vector2(200, 25));

        // ProgressBar (Slider, non-interactable)
        GameObject sliderObj = CreateSlider(root, "ProgressBar", HexColor("#333333"), HexColor("#FFD700"));
        SetAnchor(sliderObj, AnchorType.BottomLeft, new Vector2(240, 20), new Vector2(180, 15));

        // ClaimButton
        GameObject claimBtn = new GameObject("ClaimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimBtn.transform.SetParent(root.transform, false);
        Image btnImg = claimBtn.GetComponent<Image>();
        btnImg.color = HexColor("#3F51B5");
        SetAnchor(claimBtn, AnchorType.Right, new Vector2(-20, 0), new Vector2(120, 45));

        // ClaimButtonLabel (child of ClaimButton) -> index 4
        GameObject btnLabel = CreateTMPText(claimBtn, "ClaimButtonLabel", "In Progress", 14, TextAlignmentOptions.Center);
        SetAnchor(btnLabel, AnchorType.StretchAll, Vector2.zero, new Vector2(120, 45));

        // CompletedOverlay
        GameObject completed = new GameObject("CompletedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        completed.transform.SetParent(root.transform, false);
        Image compImg = completed.GetComponent<Image>();
        compImg.color = new Color(0f, 1f, 0f, 30f / 255f); // alpha 30
        compImg.raycastTarget = false;
        SetAnchor(completed, AnchorType.StretchAll, Vector2.zero, new Vector2(600, 120));
        completed.SetActive(false); // hidden by default

        SaveAndDestroy(root, dir + "/QuestRow.prefab");
    }

    private static void GenerateUnitBattleCard(string dir)
    {
        GameObject root = new GameObject("UnitBattleCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 150);
        Image bg = root.GetComponent<Image>();
        bg.color = HexColor("#0D1B2A");
        bg.raycastTarget = false;

        // NameText
        GameObject nameText = CreateTMPText(root, "NameText", "Warrior", 16, TextAlignmentOptions.Center);
        nameText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        SetAnchor(nameText, AnchorType.Top, new Vector2(0, -15), new Vector2(110, 25));

        // HpBarSlider (Slider, non-interactable)
        GameObject sliderObj = CreateSlider(root, "HpBarSlider", HexColor("#3D0000"), HexColor("#FF0000"));
        SetAnchor(sliderObj, AnchorType.Center, new Vector2(0, -10), new Vector2(100, 20));

        // HpText (TextMeshProUGUI centered over bar)
        GameObject hpText = CreateTMPText(sliderObj, "HpText", "100/100", 12, TextAlignmentOptions.Center);
        SetAnchor(hpText, AnchorType.StretchAll, Vector2.zero, new Vector2(100, 20));

        // DeadOverlay
        GameObject deadOverlay = new GameObject("DeadOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        deadOverlay.transform.SetParent(root.transform, false);
        Image deadImg = deadOverlay.GetComponent<Image>();
        deadImg.color = new Color(0f, 0f, 0f, 160f / 255f); // black alpha 160
        deadImg.raycastTarget = true; // block interaction when dead
        SetAnchor(deadOverlay, AnchorType.StretchAll, Vector2.zero, new Vector2(120, 150));
        deadOverlay.SetActive(false); // hidden by default

        SaveAndDestroy(root, dir + "/UnitBattleCard.prefab");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static GameObject CreateTMPText(GameObject parent, string name, string text, float size, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold; // Make prefab texts bold for high clarity
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return obj;
    }

    private static GameObject CreateSlider(GameObject parent, string name, Color bgCol, Color fillCol)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent.transform, false);
        Slider slider = sliderObj.GetComponent<Slider>();
        slider.interactable = false;

        // Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = bgCol;
        bgImg.raycastTarget = false;
        SetAnchor(bgObj, AnchorType.StretchAll, Vector2.zero, Vector2.zero);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        SetAnchor(fillArea, AnchorType.StretchAll, Vector2.zero, Vector2.zero);

        // Fill
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillCol;
        fillImg.raycastTarget = false;
        SetAnchor(fillObj, AnchorType.StretchAll, Vector2.zero, Vector2.zero);

        slider.fillRect = fillObj.GetComponent<RectTransform>();
        return sliderObj;
    }

    private static GameObject CreateTransparentButton(GameObject parent, string name, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent.transform, false);
        Image img = obj.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // transparent
        SetAnchor(obj, AnchorType.StretchAll, Vector2.zero, size);
        return obj;
    }

    private static Color HexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color col))
            return col;
        return Color.white;
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

    private static void SaveAndDestroy(GameObject obj, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
    }
}
#endif
