using UnityEngine;
using TMPro;

public class InfoCell : MonoBehaviour
{
    public TMP_Text txt_Label;
    public TMP_Text txt_Val;

    public void Set(object val)
    {
        if (txt_Val != null)
        {
            txt_Val.text = val?.ToString() ?? "—";
        }
    }
}
