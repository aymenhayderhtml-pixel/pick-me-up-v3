#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RosterWiringTool
{
    private const string HeroCardPrefabPath = "Assets/Prefabs/UI/HeroCard.prefab";

    [MenuItem("Tools/Pick Me Up/Wire Hero Card Prefab")]
    public static void WireHeroCardPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCardPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[RosterWiringTool] Missing prefab at {HeroCardPrefabPath}");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(HeroCardPrefabPath);
        try
        {
            var cardUi = contents.GetComponent<HeroCardUI>() ?? contents.AddComponent<HeroCardUI>();
            AutoWireHeroCard(cardUi, contents.transform);
            PrefabUtility.SaveAsPrefabAsset(contents, HeroCardPrefabPath);
            Debug.Log("[RosterWiringTool] HeroCard prefab wired.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    [MenuItem("Tools/Pick Me Up/Wire Active Roster Scene")]
    public static void WireActiveRosterScene()
    {
        var rosterUIs = Object.FindObjectsByType<RosterUI>(FindObjectsSortMode.None);
        if (rosterUIs == null || rosterUIs.Length == 0)
        {
            Debug.LogWarning("[RosterWiringTool] No RosterUI found in the active scene.");
            return;
        }

        var heroCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroCardPrefabPath);

        foreach (var rosterUI in rosterUIs)
        {
            if (rosterUI == null) continue;

            rosterUI.heroCardPrefab = heroCardPrefab;
            rosterUI.heroGridContent = FindDeepChild(rosterUI.transform, "Content")
                ?? FindDeepChild(rosterUI.transform, "HeroGridContent")
                ?? FindDeepChild(rosterUI.transform, "heroGridContent");

            rosterUI.detailPanel = FindDeepChild(rosterUI.transform, "DetailPanel")?.gameObject
                ?? FindDeepChild(rosterUI.transform, "detailPanel")?.gameObject;

            rosterUI.txt_Gold = FindText(rosterUI.transform, "txt_Gold");
            rosterUI.txt_Gems = FindText(rosterUI.transform, "txt_Gems");
            rosterUI.btn_Back = FindButton(rosterUI.transform, "btn_Back");

            rosterUI.btn_FilterAll = FindButton(rosterUI.transform, "btn_FilterAll");
            rosterUI.btn_FilterActive = FindButton(rosterUI.transform, "btn_FilterActive");
            rosterUI.btn_FilterFallen = FindButton(rosterUI.transform, "btn_FilterFallen");

            EditorUtility.SetDirty(rosterUI);
        }

        EditorSceneManager.MarkSceneDirty(rosterUIs[0].gameObject.scene);
        Debug.Log("[RosterWiringTool] Active roster scene wired.");
    }

    private static void AutoWireHeroCard(HeroCardUI cardUi, Transform root)
    {
        if (cardUi == null || root == null) return;

        cardUi.Portrait = FindDeepChild(root, "Portrait")?.GetComponent<RawImage>();
        cardUi.DeployBadge = FindDeepChild(root, "DeployBadge")?.gameObject;
        cardUi.txt_Deploy = FindText(root, "txt_Deploy");
        cardUi.LockIcon = FindDeepChild(root, "LockIcon")?.GetComponent<Image>();
        cardUi.ConditionOrb = FindDeepChild(root, "ConditionOrb")?.GetComponent<Image>();
        cardUi.icon_Eye = FindDeepChild(root, "icon_Eye")?.GetComponent<Image>();
        cardUi.CPBadge = FindDeepChild(root, "CPBadge")?.gameObject;
        cardUi.txt_CP = FindText(root, "txt_CP");
        cardUi.txt_Name = FindText(root, "NameText") ?? FindText(root, "txt_Name");
        cardUi.txt_Stars = FindText(root, "StarText") ?? FindText(root, "txt_Stars");
        cardUi.txt_Class = FindText(root, "ClassText") ?? FindText(root, "txt_Class");
        cardUi.txt_Level = FindText(root, "LevelText") ?? FindText(root, "txt_Level");
        cardUi.EpitaphLine = FindText(root, "StatusText") ?? FindText(root, "EpitaphLine");

        EditorUtility.SetDirty(cardUi);
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
}
#endif
