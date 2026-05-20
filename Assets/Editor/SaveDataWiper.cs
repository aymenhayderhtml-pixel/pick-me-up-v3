using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveDataWiper : EditorWindow
{
    [MenuItem("Tools/PickMeUp UI/Clear Save Data (Fix Mock Data)")]
    public static void ClearSaveData()
    {
        string savePath = Application.persistentDataPath + "/savegame.json";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Deleted JSON Save Data at " + savePath);
        }
        else
        {
            Debug.Log("No JSON save data found at " + savePath);
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Deleted all PlayerPrefs.");

        Debug.Log("Save Data Cleared! Press Play to spawn the correct 4 mock heroes.");
    }
}
