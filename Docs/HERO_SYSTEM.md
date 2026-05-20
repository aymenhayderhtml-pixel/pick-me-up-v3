# HERO_SYSTEM.md

## Purpose
Defines the template hero data and the runtime hero instance model.

## Main Scripts
- `Assets/Scripts/HeroData.cs`
- `Assets/Scripts/HeroInstance.cs`
- `Assets/Scripts/HeroDatabase.cs`
- `Assets/Scripts/HeroUtils.cs`
- `Assets/Scripts/GameManager.cs`

## Dependencies
- `SkillData` for skill links
- `GameManager` for hero registry and roster access
- `HeroDatabase` for stable ID lookup and template validation
- `SaveSystem` for persistence
- `MoraleSystem` for morale and status transitions
- `TraitSystem` for trait effects in combat
- `Resources/Heroes` for current fallback asset loading path

## Data Flow
```mermaid
flowchart LR
    A[HeroData SO] --> B[HeroInstance]
    A --> G[HeroDatabase lookup]
    G --> B
    B --> C[GameState roster]
    C --> D[SaveSystem JSON]
    D --> E[Load on boot]
    E --> F[UI, combat, morale, quests]
```

## Runtime Lifecycle
1. `GameManager` loads hero templates
2. `HeroDatabase` indexes templates by stable `heroId`
3. A summon creates a `HeroInstance`
4. The hero is added to the roster
5. Combat reads the runtime snapshot through `CombatUnit`
6. Leveling, morale, death, and titles mutate the instance
7. Save writes the instance back to JSON

## Related Managers
- `GameManager`
- `MoraleSystem`
- `FacilityManager`
- `QuestSystem`
- `CombatManager`
- `SynthesisSystem`

## Common Bugs
- Hero asset names and runtime IDs drift apart
- Missing or duplicate `heroId` values break database consistency
- Missing `Resources/Heroes` assets break registry lookup
- Stat calculations are touched in more than one place
- Trait logic can be duplicated between combat and morale systems

## Important Warnings
- `HeroData` should stay read-only
- `HeroInstance` is the canonical place for mutable hero state
- `HeroDatabase` is the lookup layer, not the source of truth
- Do not store scene object references on hero data or save state
- Do not add UI-only fields into the template asset unless they are true gameplay data

## AI Editing Precautions
- Read `HeroData`, `HeroInstance`, and the matching system doc before editing hero logic
- If a field changes here, update save data, UI display, and combat consumers together
- Prefer changing helper methods over rewriting raw stat math
- Do not rename hero IDs unless migration is planned
- Validate `heroId` uniqueness before shipping new hero content

## Future Expansion Plans
- Equipment slots and loadout bonuses
- Affinity and relationship systems
- Hero branching evolutions
- Full addressable-backed hero content
- Duplicate conversion and shard systems
- JSON import pipeline for hero templates
