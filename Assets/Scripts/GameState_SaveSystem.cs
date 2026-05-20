using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ─────────────────────────────────────────────────────────
// GAME STATE
// Everything that needs to persist between sessions.
// Serialized to JSON by SaveSystem.
// ─────────────────────────────────────────────────────────
[Serializable]
public class GameState
{
    // Resources
    public int gold      = 1000;
    public int gems      = 50;
    public int stamina   = 50;
    public int maxStamina = 50;
    public int essence   = 0;

    // Progression
    public int playerLevel         = 1;
    public int playerXP            = 0;
    public int highestFloorCleared = 0;
    public int totalSummons        = 0;
    public int pityCounter         = 0;     // resets at 50 (hard pity)

    // Roster — all heroes ever summoned (alive + dead)
    public List<HeroInstance> roster = new List<HeroInstance>();

    // Current squad (list of instanceIds)
    public List<string> currentSquad = new List<string>();

    // Meta
    public string masterName   = "Master";
    public string lastLoginDate = "";

    // Quests
    public QuestSaveData questSaveData;

    // ─── Pick Me Up Specs (Expansion) ──────────────────────────
    public int currentRosterCapacity = 100;
    public int dailyStaminaRefillsCount = 0;
    public string lastStaminaRefillResetDate = "";

    public bool isSubscriptionActive = false;
    public string lastSubscriptionClaimDate = "";

    public List<DroppedGearZone> activeSalvageZones = new List<DroppedGearZone>();
    public List<string> playerInventory = new List<string>();
}

// ─────────────────────────────────────────────────────────
// SAVE SYSTEM
// JSON file in Application.persistentDataPath.
// ─────────────────────────────────────────────────────────
public static class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Initialize()
    {
        // Create save directory if it doesn't exist
        var dir = Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void Save(GameState state)
    {
        try
        {
            string json = JsonUtility.ToJson(state, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
        }
    }

    public static GameState Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] No save found — starting fresh.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameState>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Corrupt save — resetting. Error: {e.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static bool HasSave() => File.Exists(SavePath);
}
