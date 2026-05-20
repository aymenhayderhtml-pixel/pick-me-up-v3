using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearSlotUI : MonoBehaviour
{
    public Image img_GearIcon;
    public TMP_Text txt_GearType;
    public TMP_Text txt_GearName;
    public TMP_Text txt_GearRarity;
    public Button btn_Equip;

    public void Populate(string gearId, string gearType, bool locked)
    {
        if (txt_GearType != null)
        {
            txt_GearType.text = gearType.ToUpper();
        }

        if (string.IsNullOrEmpty(gearId) || gearId == "None")
        {
            if (txt_GearName != null)
            {
                txt_GearName.text = "— Empty —";
                txt_GearName.color = new Color32(0x3A, 0x38, 0x35, 0xFF); // `#3A3835` grey
            }
            if (txt_GearRarity != null)
            {
                txt_GearRarity.gameObject.SetActive(false);
            }
            if (img_GearIcon != null)
            {
                img_GearIcon.color = new Color(1f, 1f, 1f, 0.1f);
            }
        }
        else
        {
            if (txt_GearName != null)
            {
                txt_GearName.text = gearId;
                txt_GearName.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF); // Parchment
            }
            if (txt_GearRarity != null)
            {
                txt_GearRarity.gameObject.SetActive(true);
                txt_GearRarity.text = "RARE"; // default display
                txt_GearRarity.color = new Color32(0xC9, 0xA8, 0x4C, 0xFF); // GOLD_BRIGHT
            }
            if (img_GearIcon != null)
            {
                img_GearIcon.color = Color.white;
            }
        }

        if (btn_Equip != null)
        {
            btn_Equip.interactable = !locked;
        }
    }
}
