using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class RosterUIStyler : EditorWindow
{
    // Web Prototype Colors
    private static readonly Color32 Col_BgDark = new Color32(0x0A, 0x0A, 0x0C, 0xFF);
    private static readonly Color32 Col_PanelGlass = new Color32(0x14, 0x14, 0x19, 0xD8); // rgba(20, 20, 25, 0.85)
    private static readonly Color32 Col_GoldAccent = new Color32(0xC5, 0xA0, 0x59, 0xFF);
    private static readonly Color32 Col_TextMain = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
    private static readonly Color32 Col_DangerRed = new Color32(0xAA, 0x22, 0x22, 0xFF);

    [MenuItem("Tools/PickMeUp UI/Apply Prototype Styles")]
    public static void ApplyStyles()
    {
        Debug.Log("Starting UI Styler...");
        int modifiedCount = 0;

        // 1. Style components in Scene
        var rosterUIs = FindObjectsByType<RosterUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var roster in rosterUIs)
        {
            modifiedCount += StyleRosterUI(roster);
        }

        var heroCards = FindObjectsByType<HeroCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var card in heroCards)
        {
            StyleHeroCard(card.gameObject);
            modifiedCount++;
        }

        var detailPanels = FindObjectsByType<DetailPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in detailPanels)
        {
            StyleDetailPanel(panel);
            modifiedCount++;
        }

        // 2. Style components in Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var roster = prefab.GetComponent<RosterUI>();
                if (roster != null)
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        var rosterInstance = editingScope.prefabContentsRoot.GetComponent<RosterUI>();
                        modifiedCount += StyleRosterUI(rosterInstance);
                    }
                }
                
                var heroCard = prefab.GetComponent<HeroCardUI>();
                if (heroCard != null)
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        StyleHeroCard(editingScope.prefabContentsRoot);
                        modifiedCount++;
                    }
                }

                var detailPanel = prefab.GetComponent<DetailPanelUI>();
                if (detailPanel != null)
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        StyleDetailPanel(editingScope.prefabContentsRoot.GetComponent<DetailPanelUI>());
                        modifiedCount++;
                    }
                }
            }
        }

        Debug.Log($"UI Styling Complete. Modified {modifiedCount} components (Including prefabs and scene objects).");
    }

    private static int StyleRosterUI(RosterUI roster)
    {
        if (roster == null) return 0;
        int mods = 0;
        Undo.RecordObject(roster.gameObject, "Style RosterUI");
            
        // Try to set main background
        var img = roster.GetComponent<Image>();
        if (img != null) { img.color = Col_BgDark; mods++; }

        // Header text
        if (roster.transform.Find("Header/Title") != null)
        {
            var txt = roster.transform.Find("Header/Title").GetComponent<TMP_Text>();
            if (txt != null)
            {
                Undo.RecordObject(txt, "Style Roster Title");
                txt.color = Col_GoldAccent;
                mods++;
            }
        }
            
        // Apply layout to Grid
        if (roster.heroGridContent != null)
        {
            var gridLayout = roster.heroGridContent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                Undo.RecordObject(gridLayout, "Style Roster Grid");
                gridLayout.cellSize = new Vector2(180, 280);
                gridLayout.spacing = new Vector2(15, 15);
                mods++;
            }
        }

        // Style the HeroCardPrefab directly if linked
        if (roster.heroCardPrefab != null)
        {
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(roster.heroCardPrefab);
            if (string.IsNullOrEmpty(assetPath)) assetPath = AssetDatabase.GetAssetPath(roster.heroCardPrefab);

            if (!string.IsNullOrEmpty(assetPath))
            {
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(assetPath))
                {
                    var prefabRoot = editingScope.prefabContentsRoot;
                    StyleHeroCard(prefabRoot);
                } // Automatically saves the prefab
                Debug.Log($"Styled linked HeroCard Prefab at: {assetPath}");
            }
        }
        
        return mods > 0 ? 1 : 0;
    }

    private static void StyleHeroCard(GameObject cardObj)
    {
        var cardUI = cardObj.GetComponent<HeroCardUI>();
        if (cardUI == null) return;

        Undo.RecordObject(cardObj, "Style Hero Card");

        // Base card background
        var bgImage = cardObj.GetComponent<Image>();
        if (bgImage != null)
        {
            Undo.RecordObject(bgImage, "Style Hero Card BG");
            bgImage.color = new Color32(0x11, 0x11, 0x11, 0xFF); // Dark base
        }

        // Deployment Badge
        if (cardUI.DeployBadge != null)
        {
            var badgeImg = cardUI.DeployBadge.GetComponent<Image>();
            if (badgeImg != null)
            {
                Undo.RecordObject(badgeImg, "Style Deploy Badge");
                badgeImg.color = Col_DangerRed;
            }
        }

        // Name
        if (cardUI.txt_Name != null)
        {
            Undo.RecordObject(cardUI.txt_Name, "Style Hero Name");
            cardUI.txt_Name.color = Col_TextMain;
        }

        EditorUtility.SetDirty(cardObj);
    }
    
    private static void StyleDetailPanel(DetailPanelUI panel)
    {
        if (panel == null) return;
        Undo.RecordObject(panel.gameObject, "Style Detail Panel");
            
        var img = panel.GetComponent<Image>();
        if (img != null) img.color = Col_PanelGlass;

        if (panel.txt_NameText != null)
        {
            Undo.RecordObject(panel.txt_NameText, "Style Detail Name");
            panel.txt_NameText.color = Col_GoldAccent;
        }
        EditorUtility.SetDirty(panel.gameObject);
    }
}
