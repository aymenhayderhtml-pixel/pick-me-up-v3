using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateIslanHanTool : EditorWindow
{
    [MenuItem("Tools/PickMeUp UI/Create Islan Han")]
    public static void CreateHero()
    {
        string sourceImagePath = @"C:\Users\hp\Desktop\ISLAT HAN.jpg";
        string targetImagePath = "Assets/Resources/Heroes/ISLAT_HAN.jpg";
        string targetAssetPath = "Assets/Resources/Heroes/SO_Hero_IslanHan.asset";

        if (!File.Exists(sourceImagePath))
        {
            Debug.LogError($"Could not find image at: {sourceImagePath}");
            return;
        }

        // 1. Copy image to Unity
        if (!Directory.Exists("Assets/Resources/Heroes"))
        {
            Directory.CreateDirectory("Assets/Resources/Heroes");
        }
        File.Copy(sourceImagePath, targetImagePath, true);
        
        // 2. Import and setup as Sprite
        AssetDatabase.ImportAsset(targetImagePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(targetImagePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(targetImagePath, ImportAssetOptions.ForceUpdate);
        }

        // 3. Create Hero Data
        HeroData newHero = ScriptableObject.CreateInstance<HeroData>();
        newHero.heroName = "ISLAN HAN";
        newHero.heroClass = HeroClass.Novice;
        newHero.starRating = 1;
        newHero.baseStats = new BaseStats { hp = 12, maxHp = 12, atk = 14, def = 10, spd = 12 };
        
        // Load the newly created sprite
        Sprite heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetImagePath);
        newHero.portrait = heroSprite;

        // Save Scriptable Object
        AssetDatabase.CreateAsset(newHero, targetAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ISLAN HAN created successfully! Press Play to see him in the roster.");
    }
}
