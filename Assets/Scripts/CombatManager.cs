using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Fully auto battle simulation.
/// Attach to a GameObject in your Battle scene.
/// Call StartBattle() from your Battle UI controller.
/// Subscribe to events for UI updates.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // ── Speed control (set by UI speed buttons) ───────────
    [HideInInspector] public float battleSpeed = 1f; // 1x, 2x, 3x

    // ── Events (Battle UI subscribes to these) ────────────
    public static event Action<CombatUnit, CombatUnit, int, bool> OnAttack;
    // (attacker, target, damage, isCrit)

    public static event Action<CombatUnit>          OnUnitDied;
    public static event Action<CombatUnit>          OnUnitLowHP;        // HP < 30%
    public static event Action<string>              OnCombatLog;        // text line for log
    public static event Action<BattleResult>        OnBattleEnd;
    public static event Action<int>                 OnTurnStart;        // turn number

    // ── Internal state ────────────────────────────────────
    List<CombatUnit> _heroes  = new List<CombatUnit>();
    List<CombatUnit> _enemies = new List<CombatUnit>();
    List<CombatUnit> _allUnits = new List<CombatUnit>();
    List<CombatUnit> _fallenAllies = new List<CombatUnit>();
    int _turn = 0;
    bool _battleRunning = false;
    int _currentFloor;

    // ── Timing ────────────────────────────────────────────
    const float BASE_TURN_DELAY    = 1.0f;
    const float ATTACK_ANIM_DELAY  = 0.5f;
    const float DEATH_PAUSE        = 1.0f;   // pause after a hero dies (grief moment)
    const float RESULT_DELAY       = 1.5f;   // dramatic pause before showing results

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────
    // PUBLIC ENTRY POINT
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Call this from BattleUIController after squad and enemies are set up.
    /// </summary>
    public void StartBattle(List<HeroInstance> squad, List<EnemyData> enemies, int floor)
    {
        if (_battleRunning) return;
        _currentFloor = floor;
        _turn = 0;
        _fallenAllies.Clear();

        // Flat deployment fee (deduct 10% Morale instantly on tower deployment)
        foreach (var h in squad)
        {
            if (GameManager.Instance.MoraleSystem != null)
                GameManager.Instance.MoraleSystem.ApplyMoraleDelta(h, -10, "Squad deployment fee.");
        }

        BuildUnits(squad, enemies);
        StartCoroutine(BattleLoop());
    }

    // ─────────────────────────────────────────────────────
    // UNIT SETUP
    // ─────────────────────────────────────────────────────
    void BuildUnits(List<HeroInstance> squad, List<EnemyData> enemies)
    {
        _heroes.Clear();
        _enemies.Clear();
        _allUnits.Clear();

        foreach (var h in squad)
        {
            // Skip rebellious heroes who refuse to fight
            if (h.IsRebelling())
            {
                Log($"{h.DisplayName} refuses to enter battle! (Morale too low)");
                continue;
            }

            var unit = new CombatUnit
            {
                instanceId  = h.instanceId,
                unitName    = h.DisplayName,
                isHero      = true,
                heroRef     = h,
                maxHP       = h.maxHP,
                currentHP   = h.currentHP,
                atk         = h.atk,
                def         = h.def,
                spd         = h.spd,
                critChance  = h.critChance,
                critMult    = h.critMult,
                trait       = h.trait
            };
            _heroes.Add(unit);
        }

        int enemyIndex = 0;
        foreach (var e in enemies)
        {
            var unit = new CombatUnit
            {
                instanceId  = "enemy_" + enemyIndex++,
                unitName    = e.enemyName,
                isHero      = false,
                maxHP       = e.hp,
                currentHP   = e.hp,
                atk         = e.atk,
                def         = e.def,
                spd         = e.spd,
                critChance  = e.critChance,
                critMult    = e.critMult
            };
            _enemies.Add(unit);
        }

        // All units together for turn ordering
        _allUnits.AddRange(_heroes);
        _allUnits.AddRange(_enemies);
    }

    // ─────────────────────────────────────────────────────
    // BATTLE LOOP
    // ─────────────────────────────────────────────────────
    IEnumerator BattleLoop()
    {
        _battleRunning = true;
        Log("⚔ Battle begins!");

        while (!IsBattleOver())
        {
            _turn++;
            OnTurnStart?.Invoke(_turn);

            // Sort all living units by SPD (highest goes first)
            var turnOrder = _allUnits
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.spd)
                .ToList();

            foreach (var attacker in turnOrder)
            {
                if (!attacker.IsAlive) continue;
                if (IsBattleOver()) break;

                yield return new WaitForSeconds(BASE_TURN_DELAY / battleSpeed);

                // Tick cooldowns for this attacker
                if (attacker.heroRef != null && attacker.heroRef.skills != null)
                {
                    foreach (var s in attacker.heroRef.skills)
                    {
                        if (s.cooldownTurnsRemaining > 0)
                            s.cooldownTurnsRemaining--;
                    }
                }

                // Autonomous Skill Evaluation (like the manhwa!)
                SkillInstance skillToCast = null;
                if (attacker.heroRef != null)
                {
                    var alliesSnapshot = attacker.isHero ? _heroes : _enemies;
                    var enemiesSnapshot = attacker.isHero ? _enemies : _heroes;
                    skillToCast = CombatSkillEvaluator.EvaluateActiveSkill(attacker, alliesSnapshot, enemiesSnapshot, _turn);
                }

                CombatUnit target = null;
                if (skillToCast != null && skillToCast.Data != null)
                {
                    // Cast active skill!
                    ExecuteCombatSkill(attacker, skillToCast);
                    skillToCast.cooldownTurnsRemaining = skillToCast.Data.baseCooldownTurns;
                    
                    // Gain skill experience by using it (manhwa-accurate refinement!)
                    skillToCast.GainExp(10);
                    
                    // Delay for animation
                    yield return new WaitForSeconds(ATTACK_ANIM_DELAY / battleSpeed);
                }
                else
                {
                    // Standard Attack
                    target = PickTarget(attacker);
                    if (target == null) break;

                    int damage = CalculateDamage(attacker, target);
                    bool isCrit = WasCrit(attacker);

                    target.currentHP = Mathf.Max(0, target.currentHP - damage);

                    // Fire attack event (UI plays hit animation)
                    AudioManager.PlayCombatAttack();
                    OnAttack?.Invoke(attacker, target, damage, isCrit);

                    string critText = isCrit ? " [CRITICAL HIT]" : "";
                    Log($"{attacker.unitName} attacks {target.unitName} for {damage} damage.{critText}");

                    yield return new WaitForSeconds(ATTACK_ANIM_DELAY / battleSpeed);
                }

                // Check HP warning targets
                if (target == null && skillToCast != null && skillToCast.Data != null)
                {
                    var enemiesSnapshot = attacker.isHero ? _enemies : _heroes;
                    target = enemiesSnapshot.FirstOrDefault(e => e.IsAlive);
                }

                // Low HP warning
                if (target.IsAlive && target.HPPercent < 0.3f)
                {
                    OnUnitLowHP?.Invoke(target);
                    if (target.isHero)
                        Log($"⚠ {target.unitName} is critically wounded! ({target.currentHP} HP remaining)");
                }

                // Death check
                if (!target.IsAlive)
                {
                    yield return StartCoroutine(HandleDeath(target));
                }
            }
        }

        yield return new WaitForSeconds(RESULT_DELAY / battleSpeed);
        EndBattle();
    }

    // ─────────────────────────────────────────────────────
    // DEATH HANDLING
    // ─────────────────────────────────────────────────────
    IEnumerator HandleDeath(CombatUnit unit)
    {
        AudioManager.PlayDeath();
        OnUnitDied?.Invoke(unit);

        if (unit.isHero)
        {
            _fallenAllies.Add(unit);

            // Apply death reaction HP changes to surviving allies
            foreach (var survivor in _heroes.Where(h => h.IsAlive))
            {
                int hpDelta = TraitSystem.GetDeathReactionHpRestore(survivor, unit);
                if (hpDelta != 0)
                {
                    survivor.Hp = Mathf.Clamp(survivor.Hp + hpDelta, 0, survivor.MaxHp);
                    if (hpDelta > 0)
                        Log($"✨ {survivor.unitName}'s resolve restores {hpDelta} HP!");
                    else
                        Log($"💔 {survivor.unitName}'s compassion causes them to lose {-hpDelta} HP from grief!");
                }
            }

            // Dramatic pause — the game mourns with you
            Log($"💀 {unit.unitName} has fallen. Their sacrifice will not be forgotten.");
            yield return new WaitForSeconds(DEATH_PAUSE / battleSpeed);

            // Phase 5: Gear Stripping Safeguard (strip weapon/armor/rings and drop at salvage zone)
            if (unit.heroRef != null)
            {
                List<string> droppedGear = new List<string>();
                if (!string.IsNullOrEmpty(unit.heroRef.equippedWeaponId)) droppedGear.Add(unit.heroRef.equippedWeaponId);
                if (!string.IsNullOrEmpty(unit.heroRef.equippedArmorId))  droppedGear.Add(unit.heroRef.equippedArmorId);
                if (!string.IsNullOrEmpty(unit.heroRef.equippedRingId))   droppedGear.Add(unit.heroRef.equippedRingId);

                if (droppedGear.Count > 0)
                {
                    var zone = new DroppedGearZone
                    {
                        zoneId = System.Guid.NewGuid().ToString(),
                        fallenHeroName = unit.heroRef.DisplayName,
                        floorIndex = _currentFloor,
                        gearIds = droppedGear,
                        instantRetrieved = false
                    };
                    GameManager.Instance.State.activeSalvageZones.Add(zone);
                    Log($"📦 {unit.unitName}'s gear dropped at site! Clear Floor {_currentFloor + 1} to salvage, or retrieve instantly in the Shop.");

                    // Strip gear from hero
                    unit.heroRef.equippedWeaponId = "";
                    unit.heroRef.equippedArmorId = "";
                    unit.heroRef.equippedRingId = "";
                }
            }

            // Witnessed permadeath mass trauma (-50% Morale, Stoics are immune)
            foreach (var hero in _heroes.Where(h => h.IsAlive))
            {
                int moraleHit = -50;
                if (hero.trait == PersonalityTrait.Stoic) moraleHit = 0;
                
                if (GameManager.Instance.MoraleSystem != null && hero.heroRef != null)
                    GameManager.Instance.MoraleSystem.ApplyMoraleDelta(hero.heroRef, moraleHit, "Witnessed squadmate's permadeath.");
            }

            // Write permadeath to GameManager
            if (unit.heroRef != null)
                GameManager.Instance.KillHero(unit.heroRef);
        }
        else
        {
            Log($"{unit.unitName} has been slain.");
            yield return new WaitForSeconds(0.2f / battleSpeed);

            // Give XP to surviving heroes
            int xpReward = 10 * (_currentFloor + 1);
            foreach (var hero in _heroes.Where(h => h.IsAlive))
            {
                GameManager.Instance.LevelUpHero(hero.heroRef, xpReward);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // DAMAGE FORMULA
    // From your design doc:
    // ATK * (1 + CRIT_CHANCE * CRIT_MULT) - DEF * 0.5
    // ─────────────────────────────────────────────────────
    bool _lastHitWasCrit = false;

    int CalculateDamage(CombatUnit attacker, CombatUnit target)
    {
        _lastHitWasCrit = UnityEngine.Random.value < attacker.critChance;

        float atkMult = _lastHitWasCrit ? attacker.critMult : 1f;
        float defMult = 1f;

        var context = new CombatTraitContext
        {
            AllUnits = _allUnits,
            FallenAllies = _fallenAllies,
            IsPlayerUnit = attacker.IsPlayerUnit
        };

        TraitSystem.ApplyOffensiveTraits(attacker, target, context, ref atkMult);
        TraitSystem.ApplyDefensiveTraits(target, context, ref defMult);

        float raw = attacker.atk * atkMult;
        float mitigation = target.def * defMult * 0.5f;
        int   damage = Mathf.Max(1, Mathf.RoundToInt(raw - mitigation));

        return damage;
    }

    bool WasCrit(CombatUnit attacker) => _lastHitWasCrit;

    // ─────────────────────────────────────────────────────
    // TARGET SELECTION
    // Enemies target lowest HP hero (focus fire feels dangerous)
    // Heroes target random enemy (less predictable)
    // ─────────────────────────────────────────────────────
    CombatUnit PickTarget(CombatUnit attacker)
    {
        if (attacker.isHero)
        {
            var aliveEnemies = _enemies.Where(e => e.IsAlive).ToList();
            if (aliveEnemies.Count == 0) return null;
            return aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
        }
        else
        {
            var aliveHeroes = _heroes.Where(h => h.IsAlive).ToList();
            if (aliveHeroes.Count == 0) return null;
            // Enemies focus the lowest HP hero
            return aliveHeroes.OrderBy(h => h.currentHP).First();
        }
    }

    // ─────────────────────────────────────────────────────
    // WIN / LOSE CONDITIONS
    // ─────────────────────────────────────────────────────
    bool IsBattleOver()
    {
        bool heroesWiped   = _heroes.All(h => !h.IsAlive);
        bool enemiesWiped  = _enemies.All(e => !e.IsAlive);
        return heroesWiped || enemiesWiped;
    }

    void EndBattle()
    {
        _battleRunning = false;

        bool victory = _enemies.All(e => !e.IsAlive);
        var survivors = _heroes.Where(h => h.IsAlive).Select(h => h.heroRef).ToList();
        var fallen    = _heroes.Where(h => !h.IsAlive).Select(h => h.heroRef).ToList();

        // Update survivor HP from their combat unit HP snapshot
        foreach (var h in _heroes)
        {
            if (h.heroRef != null && h.IsAlive)
            {
                h.heroRef.currentHP = h.currentHP;
            }
        }

        // Update hero history
        foreach (var h in _heroes)
        {
            if (h.heroRef == null) continue;
            h.heroRef.missionsCompleted++;
            if (victory) h.heroRef.floorsCleared++;
            h.heroRef.CheckAndAssignTitle();
        }

        // Tower progress
        if (victory)
            GameManager.Instance.CompleteFloor(_currentFloor);

        // Build result
        var result = new BattleResult
        {
            victory       = victory,
            floor         = _currentFloor,
            survivors     = survivors,
            fallen        = fallen,
            goldEarned    = victory ? CalculateGoldReward() : CalculateGoldReward() / 4,
            xpEarned      = victory ? 50 * (_currentFloor + 1) : 10
        };

        // Post-battle morale resolution & wounded checks
        if (GameManager.Instance.MoraleSystem != null)
        {
            GameManager.Instance.MoraleSystem.ResolveBattleMorale(result);
            foreach (var s in survivors)
            {
                if (s != null)
                    GameManager.Instance.MoraleSystem.MarkWoundedIfNeeded(s, s.HPPercent);
            }
        }

        if (victory)
        {
            Log($"✨ Victory! Floor {_currentFloor} cleared. {survivors.Count} heroes survived.");
            GameManager.Instance.AddGold(result.goldEarned);
        }
        else
        {
            Log($"💔 Defeat. Your party has been annihilated on Floor {_currentFloor}.");
        }

        // Track Quests
        if (GameManager.Instance.QuestSystem != null)
        {
            int squadSize = BattleSceneSetup.PendingSquad != null ? BattleSceneSetup.PendingSquad.Count : 0;
            GameManager.Instance.QuestSystem.TrackBattleResult(result, squadSize);
            GameManager.Instance.QuestSystem.TrackFloorReached(BattleSceneSetup.PendingFloor);
        }

        // Release squad
        GameManager.Instance.ClearSquad();

        OnBattleEnd?.Invoke(result);
    }

    int CalculateGoldReward()
    {
        int base_gold = 500 + (_currentFloor * 200);
        // Deathless bonus: all heroes survived
        if (_heroes.All(h => h.IsAlive))
            base_gold = Mathf.RoundToInt(base_gold * 1.5f);
        return base_gold;
    }

    // ─────────────────────────────────────────────────────
    // SPEED CONTROL (called by UI buttons)
    // ─────────────────────────────────────────────────────
    public void SetSpeed(float speed) => battleSpeed = Mathf.Clamp(speed, 1f, 3f);

    // ─────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────
    void Log(string message) => OnCombatLog?.Invoke(message);

    // ── Pick Me Up Skill Execution ────────────────────────
    void ExecuteCombatSkill(CombatUnit caster, SkillInstance skill)
    {
        var data = skill.Data;
        float powerMult = data.baseValueMultiplier * (1f + (skill.level - 1) * 0.1f); // 10% boost per skill level!

        Log($"✨ {caster.unitName} unleashes [{data.skillName}] (Level {skill.level})!");

        // Determine targets based on SkillTargetType
        List<CombatUnit> targets = new List<CombatUnit>();
        var allies = caster.isHero ? _heroes : _enemies;
        var enemies = caster.isHero ? _enemies : _heroes;

        switch (data.targetType)
        {
            case SkillTargetType.Self:
                targets.Add(caster);
                break;
            case SkillTargetType.SingleEnemy:
                CombatUnit singleEnemy = PickTarget(caster);
                if (singleEnemy != null) targets.Add(singleEnemy);
                break;
            case SkillTargetType.AllEnemies:
                targets.AddRange(enemies.Where(e => e.IsAlive));
                break;
            case SkillTargetType.LowestHpAlly:
                CombatUnit lowestAlly = allies.Where(a => a.IsAlive).OrderBy(a => a.currentHP).FirstOrDefault();
                if (lowestAlly != null) targets.Add(lowestAlly);
                break;
            case SkillTargetType.AllAllies:
                targets.AddRange(allies.Where(a => a.IsAlive));
                break;
        }

        if (targets.Count == 0) return;

        foreach (var target in targets)
        {
            if (data.skillId == "holy_heal" || data.skillId == "rejuvenation")
            {
                // Heal Skill
                int baseValue = caster.atk; // heal scales with caster ATK
                int healAmt = Mathf.RoundToInt(baseValue * powerMult);
                target.currentHP = Mathf.Min(target.maxHP, target.currentHP + healAmt);
                
                AudioManager.PlayCombatAttack();
                Log($"💚 {data.skillName} restores {healAmt} HP to {target.unitName}!");
            }
            else if (data.skillId == "shield_wall" || data.skillId == "iron_fortress")
            {
                // Shield / Defense Buff Skill
                int buffAmt = Mathf.RoundToInt(caster.def * powerMult * 0.5f);
                target.def += buffAmt;
                Log($"🛡 {data.skillName} increases {target.unitName}'s DEF by {buffAmt}!");
            }
            else
            {
                // Damage Skill
                int baseDmg = caster.atk;
                int damage = Mathf.RoundToInt(baseDmg * powerMult - target.def * 0.5f);
                damage = Mathf.Max(1, damage);

                target.currentHP = Mathf.Max(0, target.currentHP - damage);
                
                AudioManager.PlayCombatAttack();
                OnAttack?.Invoke(caster, target, damage, false);
                Log($"💥 {data.skillName} deals {damage} damage to {target.unitName}!");
            }

            // Reaction skill updates (Pain Tolerance check on target if HP drops low)
            if (target.IsAlive && target.HPPercent < 0.25f)
            {
                CombatSkillEvaluator.EvaluateReactionSkills(target, SkillType.Reaction, caster, "low_hp");
            }
        }
    }
}

// ─────────────────────────────────────────────────────────
// COMBAT UNIT
// Snapshot of a hero or enemy for the duration of one battle.
// Decoupled from HeroInstance so combat can run independently.
// ─────────────────────────────────────────────────────────
public class CombatUnit
{
    public string          instanceId;
    public string          unitName;
    public bool            isHero;
    public HeroInstance    heroRef;     // null for enemies

    public int   maxHP;
    public int   currentHP;
    public int   atk;
    public int   def;
    public int   spd;
    public float critChance;
    public float critMult;
    public PersonalityTrait trait;

    public bool  IsAlive    => currentHP > 0;
    public float HPPercent  => maxHP > 0 ? (float)currentHP / maxHP : 0f;

    // TraitSystem compatibility layer
    public bool IsPlayerUnit => isHero;
    public float HpPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public HeroInstance SourceHero => heroRef;
    public int Hp { get => currentHP; set => currentHP = value; }
    public int MaxHp { get => maxHP; set => maxHP = value; }
    public int Atk { get => atk; set => atk = value; }
    public int Def { get => def; set => def = value; }
    public int Spd { get => spd; set => spd = value; }
}

// ─────────────────────────────────────────────────────────
// BATTLE RESULT — passed to UI via OnBattleEnd event
// ─────────────────────────────────────────────────────────
public class BattleResult
{
    public bool              victory;
    public int               floor;
    public List<HeroInstance> survivors;
    public List<HeroInstance> fallen;
    public int               goldEarned;
    public int               xpEarned;
}
