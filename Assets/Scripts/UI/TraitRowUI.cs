using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TraitRowUI : MonoBehaviour
{
    public Image img_Dot; // Green=trait, Red=trauma
    public TMP_Text txt_TraitName;
    public TMP_Text txt_TraitDesc;
    public TMP_Text txt_TraumaWarning; // active only if isTrauma

    public void Populate(TraitData trait)
    {
        if (txt_TraitName != null)
            txt_TraitName.text = trait.traitName;
        if (txt_TraitDesc != null)
            txt_TraitDesc.text = trait.description;

        if (img_Dot != null)
        {
            // green trait, red trauma
            if (trait.isTrauma)
            {
                img_Dot.color = new Color32(0x8A, 0x10, 0x10, 0xFF); // TRAUMA_RED
            }
            else
            {
                img_Dot.color = new Color32(0x4A, 0x90, 0x60, 0xFF); // STATUS_GREEN
            }
        }

        if (txt_TraumaWarning != null)
        {
            txt_TraumaWarning.gameObject.SetActive(trait.isTrauma);
            txt_TraumaWarning.text = "TRAUMA WARNING: CAPPED STATS";
        }
    }
}
