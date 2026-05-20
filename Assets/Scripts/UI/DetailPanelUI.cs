using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum DetailPanelMode { Normal, DeployedLocked, Memorial }

public class DetailPanelUI : MonoBehaviour
{
    [Header("Memorial Banner")]
    public GameObject MemorialBanner; // hidden unless isDead
    public TMP_Text txt_MemLabel;
    public TMP_Text txt_LockLabel;

    [Header("Hero Header")]
    public RawImage Portrait;
    public Image[] CornerDecals; // 4x gold corner brackets
    public GameObject DeployBadge;
    public TMP_Text txt_Deploy;
    public TMP_Text txt_EarnedTitle;
    public Image[] detail_Stars; // 5x stars row
    
    [Header("Class Row")]
    public TMP_Text txt_PrevClass; // shown only if classRank > 0
    public Image img_Arrow;        // shown only if classRank > 0
    public TMP_Text txt_CurrentClass;
    public TMP_Text txt_LevelXP;

    [Header("Status Row")]
    public Image seal_Status;
    public TMP_Text txt_StatusText;
    public GameObject seal_Party; // shown only if isDeployed
    public TMP_Text txt_PartyText;

    [Header("Epitaph Block (Memorial Only)")]
    public GameObject EpitaphBlock; // hidden unless isDead
    public TMP_Text txt_EpitaphDivider;
    public TMP_Text txt_CauseOfDeath;
    public InfoCell cell_EpiFloor;
    public InfoCell cell_EpiKills;
    public InfoCell cell_EpiMissions;
    public InfoCell cell_EpiStars;
    public TMP_Text txt_DayLine;

    [Header("Section Attributes")]
    public StatBox stat_STR;
    public StatBox stat_HP;
    public StatBox stat_INT;
    public StatBox stat_AGI;
    public Slider HPBar;

    [Header("Section Soul Fatigue")]
    public GameObject Section_SoulFatigue;
    public Slider FatigueBar;
    public TMP_Text txt_FatigueVal;
    public GameObject FatigueStateTag; // Image + TMP

    [Header("Section Possessed Skill")]
    public Image img_SkillIcon;
    public TMP_Text txt_SkillName;
    public TMP_Text txt_SkillDesc;

    [Header("Section Traits / Traumas")]
    public Transform TraitList;
    public GameObject traitRowPrefab;

    [Header("Section Gear")]
    public GearSlotUI weaponSlot;
    public GearSlotUI armorSlot;
    public GearSlotUI ringSlot;

    [Header("Section Legacy")]
    public InfoCell cell_Kills;
    public InfoCell cell_Floor;
    public InfoCell cell_Missions;
    public InfoCell cell_Stars;
    public Image img_MedalIcon;
    public TMP_Text txt_EarnedTitle_Legacy;

    [Header("Interactive Elements")]
    public Image rootImage; // Image for applying the grayscale shader
    public Button btn_Close;
    public List<Button> allInteractiveButtons; // buttons disabled in locked / memorial modes

    private Material _grayscaleMaterialInstance;

    private void Awake()
    {
        AutoWireReferences();
    }

    private void OnValidate()
    {
        AutoWireReferences();
    }

    private void AutoWireReferences()
    {
        if (txt_NameText == null) txt_NameText = FindText("txt_NameText") ?? FindText("NameText");
        if (txt_EarnedTitle == null) txt_EarnedTitle = FindText("txt_EarnedTitle");
        if (txt_LevelXP == null) txt_LevelXP = FindText("txt_LevelXP");
        if (txt_CurrentClass == null) txt_CurrentClass = FindText("txt_CurrentClass");
        if (txt_PrevClass == null) txt_PrevClass = FindText("txt_PrevClass");
        if (txt_StatusText == null) txt_StatusText = FindText("txt_StatusText");
        if (txt_CauseOfDeath == null) txt_CauseOfDeath = FindText("txt_CauseOfDeath");
        if (txt_DayLine == null) txt_DayLine = FindText("txt_DayLine");
        if (txt_SkillName == null) txt_SkillName = FindText("txt_SkillName");
        if (txt_SkillDesc == null) txt_SkillDesc = FindText("txt_SkillDesc");
        if (Portrait == null) Portrait = FindDeepChild(transform, "Portrait")?.GetComponent<RawImage>();
        if (btn_Close == null) btn_Close = FindButton("btn_Close");
    }

    private TMP_Text FindText(string name) => FindDeepChild(transform, name)?.GetComponent<TMP_Text>();
    private Button FindButton(string name) => FindDeepChild(transform, name)?.GetComponent<Button>();
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

    private static void ApplyPortrait(RawImage target, Sprite portrait)
    {
        if (target == null)
            return;

        if (portrait == null || portrait.texture == null)
        {
            target.texture = null;
            target.uvRect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        target.texture = portrait.texture;

        var texRect = portrait.textureRect;
        var tex = portrait.texture;
        if (tex != null && tex.width > 0f && tex.height > 0f)
        {
            target.uvRect = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height
            );
        }
        else
        {
            target.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    public void Populate(HeroInstance hero, DetailPanelMode mode)
    {
        // --- Identity ---
        if (txt_PrevClass != null) txt_PrevClass.fontStyle = FontStyles.Normal; // reset

        if (txt_NameText != null)
        {
            txt_NameText.text = hero.heroName;
        }

        if (txt_EarnedTitle != null)
        {
            txt_EarnedTitle.gameObject.SetActive(hero.earnedTitle != null);
            txt_EarnedTitle.text = hero.earnedTitle != null ? hero.earnedTitle : "";
        }

        if (txt_LevelXP != null)
        {
            txt_LevelXP.text = $"Lv {hero.level} - EXP {hero.currentXP} / {hero.xpToNextLevel}";
        }

        if (Portrait != null)
        {
            ApplyPortrait(Portrait, hero.data != null ? hero.data.portrait : null);
            Portrait.color = Color.white;
        }

        ApplyStars(hero.starRating);
        ApplyClassRow(hero.heroClass, hero.classRank);

        // --- Attributes ---
        if (stat_STR != null) stat_STR.Set(hero.currentSTR, hero.maxSTR);
        if (stat_HP != null) stat_HP.Set(hero.currentHP, hero.maxHP);
        if (stat_INT != null) stat_INT.Set(hero.currentINT, hero.maxINT);
        if (stat_AGI != null) stat_AGI.Set(hero.currentAGI, hero.maxAGI);
        
        if (HPBar != null)
        {
            HPBar.value = hero.maxHP > 0 ? (float)hero.currentHP / hero.maxHP : 0f;
        }

        // --- Fatigue ---
        if (FatigueBar != null)
        {
            FatigueBar.value = hero.fatigue / 100f;
        }
        if (txt_FatigueVal != null)
        {
            txt_FatigueVal.text = $"{hero.fatigue} / 100";
        }
        ApplyFatigueStateTag(HeroUtils.GetFatigueState(hero));

        // --- Skill ---
        // Let's get the skill data from resources or a fallback skill name
        if (txt_SkillName != null) txt_SkillName.text = "Blade Rush"; // default
        if (txt_SkillDesc != null) txt_SkillDesc.text = "Unleashes a rapid flurry of strikes against a single target.";

        // If a possessed skill ID exists, we can display it!
        if (!string.IsNullOrEmpty(hero.possessedSkillId))
        {
            // Simple display of the skill ID
            if (txt_SkillName != null) txt_SkillName.text = hero.possessedSkillId.ToUpper().Replace("_", " ");
            if (txt_SkillDesc != null)
            {
                if (mode == DetailPanelMode.Memorial)
                {
                    txt_SkillDesc.text = "A high technique etched into history. [Memorial Postscript: Rest in glory.]";
                }
                else
                {
                    txt_SkillDesc.text = "A powerful, manhwa-accurate technical skill utilized in combat.";
                }
            }
        }

        // --- Traits / Traumas ---
        PopulateTraitList(hero.traits, hero.traumas);

        // --- Gear ---
        PopulateGearSlots(hero, mode);

        // --- Legacy ---
        if (cell_Kills != null) cell_Kills.Set(hero.kills);
        if (cell_Floor != null) cell_Floor.Set(hero.floorsCleared);
        if (cell_Missions != null) cell_Missions.Set(hero.missionsCompleted);
        if (cell_Stars != null) cell_Stars.Set($"STAR {hero.starRating}");
        if (txt_EarnedTitle_Legacy != null) txt_EarnedTitle_Legacy.text = hero.earnedTitle ?? "-";

        // --- Mode-specific ---
        ApplyMode(hero, mode);

        // --- Trauma Stat Capping display overrides ---
        ApplyTraumaVisuals(hero);
    }

    [Header("Hero Name Direct Ref")]
    public TMP_Text txt_NameText; // Specific ref for strikethrough name styling

    void ApplyMode(HeroInstance hero, DetailPanelMode mode)
    {
        if (MemorialBanner != null) MemorialBanner.SetActive(mode == DetailPanelMode.Memorial);
        if (EpitaphBlock != null) EpitaphBlock.SetActive(mode == DetailPanelMode.Memorial);
        if (seal_Party != null) seal_Party.SetActive(hero.isDeployed);

        // Fatigue and soul fatigue section are disabled in Memorial mode (hero is dead, fatigue is irrelevant)
        if (Section_SoulFatigue != null)
        {
            Section_SoulFatigue.SetActive(mode != DetailPanelMode.Memorial);
        }

        bool locked = mode != DetailPanelMode.Normal;
        if (allInteractiveButtons != null)
        {
            foreach (var btn in allInteractiveButtons)
            {
                if (btn != null && btn != btn_Close)
                {
                    btn.interactable = !locked;
                }
            }
        }

        if (mode == DetailPanelMode.Memorial)
        {
            // Desaturate entire panel
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0.85f;
            }
            ApplyMemorialDesaturation(true);

            // Epitaph
            if (txt_CauseOfDeath != null) txt_CauseOfDeath.text = !string.IsNullOrEmpty(hero.causeOfDeath) ? hero.causeOfDeath : "Killed in action during tower escalation.";
            if (cell_EpiFloor != null) cell_EpiFloor.Set(hero.deathFloor);
            if (cell_EpiKills != null) cell_EpiKills.Set(hero.kills);
            if (cell_EpiMissions != null) cell_EpiMissions.Set(hero.missionsCompleted);
        if (cell_EpiStars != null) cell_EpiStars.Set($"STAR {hero.starRating}");
            if (txt_DayLine != null) txt_DayLine.text = $"Day {hero.deathDay} of the Expedition";

            // Name strikethrough
            if (txt_NameText != null)
            {
                txt_NameText.fontStyle = FontStyles.Strikethrough;
                txt_NameText.color = new Color32(0x4A, 0x40, 0x40, 0xFF);
            }

            // HP shows 0
            if (stat_HP != null) stat_HP.SetDead();
        }
        else
        {
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1.0f;
            }
            ApplyMemorialDesaturation(false);
            if (txt_NameText != null)
            {
                txt_NameText.fontStyle = FontStyles.Normal;
                txt_NameText.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF); // Parchment
            }
        }
    }

    void ApplyClassRow(HeroClass heroClass, int classRank)
    {
        if (txt_PrevClass != null && img_Arrow != null)
        {
            if (classRank > 0)
            {
                txt_PrevClass.gameObject.SetActive(true);
                img_Arrow.gameObject.SetActive(true);
                txt_PrevClass.text = "NOVICE";
                txt_PrevClass.fontStyle = FontStyles.Strikethrough;
            }
            else
            {
                txt_PrevClass.gameObject.SetActive(false);
                img_Arrow.gameObject.SetActive(false);
            }
        }

        if (txt_CurrentClass != null)
        {
            txt_CurrentClass.text = HeroUtils.GetClassLabel(heroClass);
        }
    }

    void ApplyStars(int starRating)
    {
        if (detail_Stars == null) return;
        for (int i = 0; i < detail_Stars.Length; i++)
        {
            if (detail_Stars[i] != null)
            {
                if (i < starRating)
                {
                    detail_Stars[i].color = new Color32(0xC9, 0xA8, 0x4C, 0xFF); // GOLD_BRIGHT
                }
                else
                {
                    detail_Stars[i].color = new Color32(0x2A, 0x20, 0x10, 0xFF); // BORDER_DIM
                }
            }
        }
    }

    void ApplyFatigueStateTag(HeroUtils.FatigueState state)
    {
        if (FatigueStateTag == null) return;
        var tagImg = FatigueStateTag.GetComponent<Image>();
        var tagTxt = FatigueStateTag.GetComponentInChildren<TMP_Text>();

        switch (state)
        {
            case HeroUtils.FatigueState.Fresh:
                if (tagImg != null) tagImg.color = new Color32(0x09, 0x18, 0x10, 0xFF);
                if (tagTxt != null) { tagTxt.text = "FRESH"; tagTxt.color = new Color32(0x4A, 0x90, 0x60, 0xFF); }
                break;
            case HeroUtils.FatigueState.Strained:
                if (tagImg != null) tagImg.color = new Color32(0x18, 0x14, 0x0A, 0xFF);
                if (tagTxt != null) { tagTxt.text = "STRAINED"; tagTxt.color = new Color32(0xB0, 0x90, 0x30, 0xFF); }
                break;
            case HeroUtils.FatigueState.Breaking:
                if (tagImg != null) tagImg.color = new Color32(0x18, 0x0A, 0x0A, 0xFF);
                if (tagTxt != null) { tagTxt.text = "BREAKING POINT"; tagTxt.color = new Color32(0xB0, 0x30, 0x30, 0xFF); }
                break;
        }
    }

    void PopulateTraitList(List<TraitData> traits, List<TraitData> traumas)
    {
        if (TraitList == null || traitRowPrefab == null) return;

        // Clear children
        foreach (Transform child in TraitList)
        {
            Destroy(child.gameObject);
        }

        // Add positive traits
        if (traits != null)
        {
            foreach (var trait in traits)
            {
                var go = Instantiate(traitRowPrefab, TraitList);
                var row = go.GetComponent<TraitRowUI>();
                if (row != null) row.Populate(trait);
            }
        }

        // Add traumas
        if (traumas != null)
        {
            foreach (var trauma in traumas)
            {
                var go = Instantiate(traitRowPrefab, TraitList);
                var row = go.GetComponent<TraitRowUI>();
                if (row != null) row.Populate(trauma);
            }
        }
    }

    void PopulateGearSlots(HeroInstance hero, DetailPanelMode mode)
    {
        bool locked = (mode == DetailPanelMode.DeployedLocked || mode == DetailPanelMode.Memorial);
        
        if (weaponSlot != null)
            weaponSlot.Populate(hero.equippedWeaponId, "WEAPON", locked);

        if (armorSlot != null)
            armorSlot.Populate(hero.equippedArmorId, "ARMOR", locked);

        if (ringSlot != null)
            ringSlot.Populate(hero.equippedRingId, "RING", locked);
    }

    void ApplyTraumaVisuals(HeroInstance hero)
    {
        if (hero.traumas == null) return;

        foreach (var trauma in hero.traumas)
        {
            if (string.IsNullOrEmpty(trauma.statCapField)) continue;

            StatBox box = GetStatBox(trauma.statCapField);
            if (box == null) continue;

            int trueMax = hero.GetBaseMax(trauma.statCapField);
            int cappedMax = Mathf.RoundToInt(trueMax * trauma.capPercent);

            if (box.txt_Current != null)
                box.txt_Current.color = new Color32(0xB0, 0x50, 0x50, 0xFF); // Trauma red
            if (box.txt_Max != null)
            {
                box.txt_Max.color = new Color32(0x4A, 0x3C, 0x20, 0xFF); // Dim
                box.txt_Max.fontStyle = FontStyles.Strikethrough;
            }
            if (box.txt_CapWarning != null)
            {
                box.txt_CapWarning.gameObject.SetActive(true);
                box.txt_CapWarning.text = $"▼{Mathf.RoundToInt((1f - trauma.capPercent) * 100)}%";
            }
        }
    }

    StatBox GetStatBox(string fieldName)
    {
        switch (fieldName)
        {
            case "maxSTR":
            case "currentSTR":
                return stat_STR;
            case "maxHP":
            case "currentHP":
                return stat_HP;
            case "maxINT":
            case "currentINT":
                return stat_INT;
            case "maxAGI":
            case "currentAGI":
                return stat_AGI;
            default:
                return null;
        }
    }

    void ApplyMemorialDesaturation(bool on)
    {
        if (rootImage == null || rootImage.material == null) return;

        // Clone the material dynamically to avoid changing the shared asset on disk
        if (_grayscaleMaterialInstance == null && rootImage.material.shader.name == "UI/Grayscale")
        {
            _grayscaleMaterialInstance = Instantiate(rootImage.material);
            rootImage.material = _grayscaleMaterialInstance;
        }

        if (_grayscaleMaterialInstance != null)
        {
            _grayscaleMaterialInstance.SetFloat("_GrayscaleAmount", on ? 0.85f : 0f);
        }
    }

    private void OnDestroy()
    {
        if (_grayscaleMaterialInstance != null)
        {
            Destroy(_grayscaleMaterialInstance);
        }
    }
}
