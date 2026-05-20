using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroCardUI : MonoBehaviour
{
    [Header("Identity")]
    public RawImage Portrait;
    public GameObject DeployBadge;
    public TMP_Text txt_Deploy;
    public Image LockIcon;
    public Image ConditionOrb;
    public Image icon_Eye;
    public GameObject CPBadge;
    public TMP_Text txt_CP;

    [Header("Card Body")]
    public TMP_Text txt_Name;
    public Image[] img_Stars; // 5 stars row
    public Image ClassTag;
    public TMP_Text txt_Class;
    public TMP_Text txt_Level;
    
    [Header("Fallen Overrides")]
    public TMP_Text EpitaphLine; // e.g. "Fell on Floor X · Day Y"

    private HeroInstance _hero;
    private Coroutine _pulseCoroutine;

    public void Populate(HeroInstance hero)
    {
        _hero = hero;
        
        // Stop any running pulse
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        // Portrait
        var data = hero.data;
        if (Portrait != null)
        {
            if (data != null && data.portrait != null)
            {
                Portrait.texture = data.portrait.texture;
            }
            Portrait.color = hero.status == HeroStatus.Dead ? new Color32(0x60, 0x60, 0x60, 0xFF) : Color.white;
        }

        // Deploy Badge & Lock Icon
        if (DeployBadge != null)
            DeployBadge.SetActive(hero.isDeployed);
        if (txt_Deploy != null)
            txt_Deploy.text = "(*) ON MISSION";
        if (LockIcon != null)
            LockIcon.gameObject.SetActive(hero.isDeployed);

        // CP Badge
        if (txt_CP != null)
        {
            if (hero.status == HeroStatus.Dead)
            {
                txt_CP.text = "—";
                txt_CP.color = new Color32(0x2A, 0x20, 0x20, 0xFF);
            }
            else
            {
                txt_CP.text = $"CP {HeroUtils.CalculateCP(hero)}";
                txt_CP.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF); // Parchment color default
            }
        }

        // Card Body Text
        if (txt_Name != null)
        {
            txt_Name.text = hero.DisplayName;
            if (hero.status == HeroStatus.Dead)
            {
                txt_Name.color = new Color32(0x4A, 0x40, 0x40, 0xFF);
                txt_Name.fontStyle = FontStyles.Strikethrough;
            }
            else
            {
                txt_Name.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF); // Parchment
                txt_Name.fontStyle = FontStyles.Normal;
            }
        }

        if (txt_Level != null)
        {
            txt_Level.text = $"Lv {hero.level}";
            if (hero.status == HeroStatus.Dead)
            {
                txt_Level.color = new Color32(0x4A, 0x40, 0x40, 0xFF);
            }
            else
            {
                txt_Level.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF);
            }
        }

        // Stars Row (fill starRating gold, remainder dark)
        if (img_Stars != null)
        {
            for (int i = 0; i < img_Stars.Length; i++)
            {
                if (img_Stars[i] != null)
                {
                    if (i < hero.starRating)
                    {
                        img_Stars[i].color = new Color32(0xC9, 0xA8, 0x4C, 0xFF); // GOLD_BRIGHT
                    }
                    else
                    {
                        img_Stars[i].color = new Color32(0x2A, 0x20, 0x10, 0xFF); // BORDER_DIM / Dark star
                    }
                }
            }
        }

        // Class Tag
        if (txt_Class != null)
        {
            txt_Class.text = HeroUtils.GetClassLabel(hero.heroClass);
        }

        ApplyClassTagColors(hero.heroClass);

        // Condition Orb
        if (ConditionOrb != null)
        {
            ApplyConditionOrb(ConditionOrb, HeroUtils.GetFatigueState(hero), hero.status);
        }

        // Epitaph Line for Fallen state
        if (EpitaphLine != null)
        {
            if (hero.status == HeroStatus.Dead)
            {
                EpitaphLine.gameObject.SetActive(true);
                EpitaphLine.text = $"Fell on Floor {hero.deathFloor} · Day {hero.deathDay}";
            }
            else
            {
                EpitaphLine.gameObject.SetActive(false);
            }
        }

        // Wire Button Click to Open Detail Panel
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnCardTapped);
        }
    }

    private void ApplyClassTagColors(HeroClass c)
    {
        if (ClassTag == null || txt_Class == null) return;

        int tier = HeroUtils.GetClassTier(c);
        Color borderCol;
        Color textCol;

        switch (tier)
        {
            case 0: // Novice
                borderCol = new Color32(0x4A, 0x40, 0x30, 0xFF);
                textCol = new Color32(0x7A, 0x6A, 0x50, 0xFF);
                break;
            case 1: // Base
                borderCol = new Color32(0x2D, 0x50, 0x40, 0xFF);
                textCol = new Color32(0x5A, 0x90, 0x70, 0xFF);
                break;
            case 2: // Spec
                borderCol = new Color32(0x3A, 0x2D, 0x60, 0xFF);
                textCol = new Color32(0x80, 0x70, 0xC0, 0xFF);
                break;
            case 3: // Master
                borderCol = new Color32(0x6B, 0x30, 0x10, 0xFF);
                textCol = new Color32(0xC0, 0x70, 0x40, 0xFF);
                break;
            default:
                borderCol = new Color32(0x4A, 0x40, 0x30, 0xFF);
                textCol = new Color32(0x7A, 0x6A, 0x50, 0xFF);
                break;
        }

        ClassTag.color = borderCol;
        txt_Class.color = textCol;
    }

    private void ApplyConditionOrb(Image orb, HeroUtils.FatigueState state, HeroStatus status)
    {
        if (status == HeroStatus.Dead)
        {
            orb.color = new Color32(0x11, 0x10, 0x10, 0xFF);
            return;
        }

        switch (state)
        {
            case HeroUtils.FatigueState.Fresh:
                orb.color = new Color32(0x09, 0x18, 0x10, 0xFF);
                break;
            case HeroUtils.FatigueState.Strained:
                orb.color = new Color32(0x18, 0x14, 0x0A, 0xFF);
                break;
            case HeroUtils.FatigueState.Breaking:
                orb.color = new Color32(0x18, 0x0A, 0x0A, 0xFF);
                // Pulse loop (alpha 1 -> 0.35 -> 1 over 1.2s)
                _pulseCoroutine = StartCoroutine(PulseOrb(orb));
                break;
        }
    }

    private IEnumerator PulseOrb(Image orb)
    {
        float duration = 0.6f; // half loop
        while (true)
        {
            // Dim
            float elapsed = 0f;
            Color baseColor = orb.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1.0f, 0.35f, elapsed / duration);
                orb.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            // Brighten
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.35f, 1.0f, elapsed / duration);
                orb.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }
    }

    public void OnCardTapped()
    {
        if (RosterUI.Instance != null)
        {
            RosterUI.Instance.OpenDetailPanel(_hero);
        }
    }
}
