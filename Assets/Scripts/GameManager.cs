using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton. Persists across all scenes via DontDestroyOnLoad.
/// Owns the live game state: roster, resources, tower progress.
/// All other systems (GachaSystem, CombatManager, etc.) read/write through here.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Hero data registry (assign ALL HeroData SOs in Inspector) ──
    [Header("Hero Registry")]
    public HeroData[] allHeroData;      // drag all SO_Hero_* assets here

    // ── Live State ────────────────────────────────────────
    [HideInInspector] public GameState State = new GameState();

    public MoraleSystem MoraleSystem { get; private set; }
    public QuestSystem QuestSystem { get; private set; }
    public FacilityManager FacilityManager { get; private set; }

    // ── Events (UI subscribes to these) ───────────────────
    public static event System.Action<HeroInstance> OnHeroSummoned;
    public static event System.Action<HeroInstance> OnHeroDied;
    public static event System.Action<HeroInstance> OnHeroLevelUp;
    public static event System.Action OnResourcesChanged;
    public static event System.Action OnRosterChanged;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MoraleSystem = GetComponent<MoraleSystem>();
        QuestSystem = GetComponent<QuestSystem>();
        FacilityManager = GetComponent<FacilityManager>();

        // Auto-discover HeroData assets in Resources/Heroes/ if registry is unassigned/empty
        if (allHeroData == null || allHeroData.Length == 0)
        {
            allHeroData = Resources.LoadAll<HeroData>("Heroes");
            Debug.Log($"[GameManager] Automatically registered {allHeroData.Length} HeroData assets from Resources/Heroes/.");
        }

        SaveSystem.Initialize();
        LoadGame();

        // Dev Sandbox Testing Grant: guarantee the player has plenty of Gold and Gems to test pulls and facilities!
        if (State.gold < 100000) State.gold = 100000;
        if (State.gems < 50000) State.gems = 50000;

        // Auto-populate 4 beautiful mock heroes on a fresh game for immediate visual verification!
        if (State.roster.Count == 0)
        {
            // Fallback templates if registry is empty
            HeroData t0 = allHeroData != null && allHeroData.Length > 0 ? allHeroData[0] : null;
            HeroData t1 = allHeroData != null && allHeroData.Length > 1 ? allHeroData[1] : t0;
            HeroData t2 = allHeroData != null && allHeroData.Length > 2 ? allHeroData[2] : t0;
            HeroData t3 = allHeroData != null && allHeroData.Length > 3 ? allHeroData[3] : t0;

            // Check if Islan Han was created
            HeroData islanHanData = GetHeroData("SO_Hero_IslanHan");
            if (islanHanData != null) t0 = islanHanData; // Inject him as the first hero!
            
            // 1. Fresh Vanguard (Melee Branch) or Islan Han
            var h1 = new HeroInstance(t0);
            h1.heroName = t0 != null ? t0.heroName : "Arthur Pendragon";
            h1.heroClass = t0 != null ? t0.heroClass : HeroClass.Vanguard;
            h1.level = 1;
            h1.starRating = 4;
            h1.fatigue = 20; // Fresh
            h1.currentSTR = 22; h1.maxSTR = 22;
            h1.currentHP = 120; h1.maxHP = 120;
            h1.status = HeroStatus.Active;
            State.roster.Add(h1);
            
            // 2. Strained Elementalist (Magic Branch)
            var h2 = new HeroInstance(t1);
            h2.heroName = "Rin the Conjurer";
            h2.heroClass = HeroClass.Elementalist;
            h2.level = 22;
            h2.starRating = 5;
            h2.fatigue = 60; // Strained (Orange condition orb)
            h2.currentINT = 35; h2.maxINT = 35;
            h2.status = HeroStatus.Active;
            State.roster.Add(h2);

            // 3. Breaking Point Assassin (Agility Branch)
            var h3 = new HeroInstance(t2);
            h3.heroName = "Kaelen Swiftblade";
            h3.heroClass = HeroClass.Assassin;
            h3.level = 8;
            h3.starRating = 3;
            h3.fatigue = 85; // Breaking Point (Pulsing Red condition orb)
            h3.currentAGI = 18; h3.maxAGI = 18;
            h3.status = HeroStatus.Active;
            State.roster.Add(h3);

            // 4. Fallen Shadow Reaper (Fallen / Dead Valhalla Mode)
            var h4 = new HeroInstance(t3);
            h4.heroName = "Valerie the Lost";
            h4.heroClass = HeroClass.ShadowReaper;
            h4.level = 30;
            h4.starRating = 5;
            h4.status = HeroStatus.Dead;
            h4.causeOfDeath = "Incinerated by Ash Dragon";
            h4.deathFloor = 14;
            h4.deathDay = 45;
            h4.kills = 382;
            h4.floorsCleared = 13;
            h4.missionsCompleted = 42;
            State.roster.Add(h4);
            
            Debug.Log("[GameManager] Sandbox Mode: Populated 4 beautiful mock heroes for instant UI visual verification!");
        }

        // Change Camera Background to Dark Gothic
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color32(0x05, 0x05, 0x05, 0xFF);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }

        SaveGame();
    }

    // ─────────────────────────────────────────────────────
    // HERO REGISTRY LOOKUP
    // ─────────────────────────────────────────────────────
    public HeroData GetHeroData(string heroDataId)
    {
        return allHeroData.FirstOrDefault(h => h.name == heroDataId);
    }

    public HeroData[] GetPoolByStars(int stars)
    {
        return allHeroData.Where(h => h.starRating == stars).ToArray();
    }

    // ─────────────────────────────────────────────────────
    // ROSTER MANAGEMENT
    // ─────────────────────────────────────────────────────
    public List<HeroInstance> GetActiveRoster()
    {
        return State.roster.Where(h => h.status != HeroStatus.Dead).ToList();
    }

    public List<HeroInstance> GetMemorialHeroes()
    {
        return State.roster.Where(h => h.status == HeroStatus.Dead).ToList();
    }

    public List<HeroInstance> GetFallenHeroes() => GetMemorialHeroes();

    public void AddHeroToRoster(HeroInstance hero)
    {
        State.roster.Add(hero);
        OnHeroSummoned?.Invoke(hero);
        OnRosterChanged?.Invoke();
        SaveGame();
    }

    public bool RemoveHeroFromRoster(string instanceId)
    {
        var hero = GetHeroByInstanceId(instanceId);
        if (hero == null) return false;

        RemoveFromSquad(instanceId);
        State.roster.Remove(hero);
        OnRosterChanged?.Invoke();
        SaveGame();
        return true;
    }

    /// <summary>
    /// Called by CombatManager when a hero reaches 0 HP.
    /// Moves hero to Memorial — does NOT remove from roster list.
    /// </summary>
    public void KillHero(HeroInstance hero)
    {
        hero.Die();
        OnHeroDied?.Invoke(hero);
        OnRosterChanged?.Invoke();

        // Morale hit to all surviving deployed heroes (Compassionate trait amplifies)
        foreach (var h in GetActiveRoster().Where(h => h.isDeployed))
        {
            int moraleHit = -10;
            if (h.trait == PersonalityTrait.Compassionate) moraleHit = -20;
            if (h.trait == PersonalityTrait.Stoic) moraleHit = 0;
            h.ModifyMorale(moraleHit);
        }

        SaveGame();
    }

    public void LevelUpHero(HeroInstance hero, int xpAmount)
    {
        var data = GetHeroData(hero.heroDataId);
        if (data == null) return;

        bool leveledUp = hero.AddXP(xpAmount, data);
        if (leveledUp)
        {
            OnHeroLevelUp?.Invoke(hero);
        }
        SaveGame();
    }

    // ─────────────────────────────────────────────────────
    // SQUAD
    // ─────────────────────────────────────────────────────
    /// <summary>Max 5 heroes. Returns false if slot taken or hero unavailable.</summary>
    public bool AddToSquad(string instanceId)
    {
        if (State.currentSquad.Count >= 5) return false;
        if (State.currentSquad.Contains(instanceId)) return false;

        var hero = GetHeroByInstanceId(instanceId);
        if (hero == null || hero.status == HeroStatus.Dead || hero.isDeployed) return false;

        State.currentSquad.Add(instanceId);
        hero.isDeployed = true;
        return true;
    }

    public void RemoveFromSquad(string instanceId)
    {
        State.currentSquad.Remove(instanceId);
        var hero = GetHeroByInstanceId(instanceId);
        if (hero != null) hero.isDeployed = false;
    }

    public void ClearSquad()
    {
        foreach (var id in State.currentSquad)
        {
            var hero = GetHeroByInstanceId(id);
            if (hero != null) hero.isDeployed = false;
        }
        State.currentSquad.Clear();
    }

    public List<HeroInstance> GetCurrentSquad()
    {
        return State.currentSquad
            .Select(id => GetHeroByInstanceId(id))
            .Where(h => h != null)
            .ToList();
    }

    public int GetSquadPower()
    {
        return GetCurrentSquad().Sum(h => h.Power);
    }

    public HeroInstance GetHeroByInstanceId(string id)
    {
        return State.roster.FirstOrDefault(h => h.instanceId == id);
    }

    public HeroInstance GetHeroById(string id) => GetHeroByInstanceId(id);

    public List<HeroInstance> GetFullRoster() => new List<HeroInstance>(State.roster);

    // ─────────────────────────────────────────────────────
    // RESOURCES
    // ─────────────────────────────────────────────────────
    public bool SpendGold(int amount)
    {
        if (State.gold < amount) return false;
        State.gold -= amount;
        OnResourcesChanged?.Invoke();
        SaveGame();
        return true;
    }

    public bool SpendGems(int amount)
    {
        if (State.gems < amount) return false;
        State.gems -= amount;
        OnResourcesChanged?.Invoke();
        SaveGame();
        return true;
    }

    public bool SpendStamina(int amount)
    {
        if (State.stamina < amount) return false;
        State.stamina -= amount;
        OnResourcesChanged?.Invoke();
        SaveGame();
        return true;
    }

    public void AddGold(int amount) { State.gold += amount; OnResourcesChanged?.Invoke(); SaveGame(); }
    public void AddGems(int amount) { State.gems += amount; OnResourcesChanged?.Invoke(); SaveGame(); }
    public void AddStamina(int amount) { State.stamina = Mathf.Min(State.stamina + amount, State.maxStamina); OnResourcesChanged?.Invoke(); SaveGame(); }
    public void AddEssence(int amount) { State.essence += amount; OnResourcesChanged?.Invoke(); SaveGame(); }

    public void SpendEssence(int amount)
    {
        State.essence = Mathf.Max(0, State.essence - amount);
        OnResourcesChanged?.Invoke();
        SaveGame();
    }

    public int Essence => State.essence;

    public void SendToMemorial(HeroInstance hero)
    {
        RemoveFromSquad(hero.instanceId);
        OnRosterChanged?.Invoke();
        SaveGame();
    }

    public void FireRosterChanged() => OnRosterChanged?.Invoke();

    // ─────────────────────────────────────────────────────
    // TOWER PROGRESS
    // ─────────────────────────────────────────────────────
    // ─── Roster Expansion ──────────────────────────────────
    public bool ExpandRosterCapacity()
    {
        if (State.currentRosterCapacity >= 500) return false; // Hard Cap!


        if (SpendGems(50)) // 50 Gems per expansion
        {
            State.currentRosterCapacity += 5;
            SaveGame();
            FireRosterChanged();
            return true;
        }
        return false;
    }

    // ─── Progressive Stamina Refills ────────────────────────
    public bool RefillStaminaWithGems()
    {
        if (State.dailyStaminaRefillsCount >= 3) return false; // Capped!

        int cost = State.dailyStaminaRefillsCount switch
        {
            0 => 50,
            1 => 150,
            2 => 400,
            _ => 400
        };

        if (SpendGems(cost))
        {
            AddStamina(50); // Adds 50 stamina (respecting maxStamina limits)
            State.dailyStaminaRefillsCount++;
            SaveGame();
            return true;
        }
        return false;
    }

    public void CompleteFloor(int floor)
    {
        if (floor > State.highestFloorCleared)
        {
            State.highestFloorCleared = floor;
            SaveGame();
        }
        // Phase 5: Auto-retrieve salvage gear from this floor
        RetrieveDroppedGear(floor);
    }

    public bool RetrieveDroppedGear(string zoneId, bool isInstant)
    {
        var zone = State.activeSalvageZones.FirstOrDefault(z => z.zoneId == zoneId);
        if (zone == null || zone.instantRetrieved) return false;

        if (isInstant)
        {
            if (!SpendGems(500)) return false; // 500 Gems flat fee
            zone.instantRetrieved = true;
        }

        // Return gear to player inventory
        foreach (var gearId in zone.gearIds)
        {
            State.playerInventory.Add(gearId);
        }

        // Remove from active zones
        State.activeSalvageZones.Remove(zone);
        SaveGame();
        return true;
    }

    /// <summary>
    /// Automatic floor clearance salvage recovery.
    /// </summary>
    public void RetrieveDroppedGear(int floorIndex)
    {
        if (State.activeSalvageZones == null || State.activeSalvageZones.Count == 0) return;

        var matchingZones = State.activeSalvageZones.Where(z => z.floorIndex == floorIndex).ToList();
        if (matchingZones.Count == 0) return;

        foreach (var zone in matchingZones)
        {
            // Recover for free
            foreach (var gearId in zone.gearIds)
            {
                State.playerInventory.Add(gearId);
            }
            State.activeSalvageZones.Remove(zone);
            Debug.Log($"[Salvage] Automatically recovered gear from fallen hero '{zone.fallenHeroName}' on floor clearance.");
        }
        SaveGame();
    }

    void Start()
    {
        if (QuestSystem != null)
        {
            QuestSystem.Initialize(State?.questSaveData);
        }
    }

    // ─────────────────────────────────────────────────────
    // SAVE / LOAD
    // ─────────────────────────────────────────────────────
    public void SaveGame()
    {
        if (QuestSystem != null)
        {
            State.questSaveData = QuestSystem.GetSaveData();
        }
        SaveSystem.Save(State);
    }

    public void LoadGame() => State = SaveSystem.Load() ?? new GameState();
}
