using UnityEngine;
using TMPro;

public class StatBox : MonoBehaviour
{
    public TMP_Text txt_Name;
    public TMP_Text txt_Current;
    public TMP_Text txt_Sep;
    public TMP_Text txt_Max;
    public TMP_Text txt_CapWarning; // e.g. "▼15%"

    public void Set(int current, int max)
    {
        if (txt_Current != null)
        {
            txt_Current.text = current.ToString();
            txt_Current.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF); // Parchment color default
        }
        if (txt_Max != null)
        {
            txt_Max.text = max.ToString();
            txt_Max.color = new Color32(0xF0, 0xE8, 0xD8, 0xFF);
            txt_Max.fontStyle = FontStyles.Normal;
        }
        if (txt_CapWarning != null)
        {
            txt_CapWarning.gameObject.SetActive(false);
        }
    }

    public void SetDead()
    {
        if (txt_Current != null)
        {
            txt_Current.text = "0";
            txt_Current.color = new Color32(0x2A, 0x20, 0x20, 0xFF); // STATUS_DEAD style dim color
        }
        if (txt_Max != null)
        {
            txt_Max.color = new Color32(0x2A, 0x20, 0x20, 0xFF);
        }
        if (txt_CapWarning != null)
        {
            txt_CapWarning.gameObject.SetActive(false);
        }
    }
}
