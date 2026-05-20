# INVENTORY_SYSTEM.md

## Purpose
Template for the future item, equipment, and salvage system.

## Main Scripts
- `Assets/Scripts/GameState_SaveSystem.cs` for the current `playerInventory` and salvage storage fields
- Future: `InventoryManager`
- Future: `ItemData`
- Future: `EquipmentData`
- Future: `InventoryUI`
- Future: `EquipmentUI`

## Dependencies
- `GameManager`
- `SaveSystem`
- `HeroInstance` equipment slots
- `DroppedGearZone`
- Future `Addressables` or content registry

## Data Flow
```mermaid
flowchart LR
    A[Loot source] --> B[Inventory data]
    B --> C[Equipment or stash]
    C --> D[Runtime stat changes]
    D --> E[SaveSystem]
    E --> F[UI refresh]
```

## Runtime Lifecycle
1. Item is generated or looted
2. Item is added to the inventory store
3. Player inspects, equips, uses, or salvages the item
4. Runtime bonuses are applied to the relevant hero or squad
5. Inventory state is saved

## Related Managers
- `GameManager`
- `SaveSystem`
- `FacilityManager` if item sinks are added later
- `QuestSystem` for collection milestones

## Common Bugs
- Duplicate item IDs
- Direct object references that become invalid after reload
- Inventory capacity rules not matching UI
- Equipment bonuses applied twice, once in runtime and once in display code

## Important Warnings
- Keep item definitions as data, not scene logic
- Prefer stable IDs and generated lookup tables
- Do not store prefab instances directly in save data

## AI Editing Precautions
- Do not implement inventory as ad hoc lists inside UI scripts
- Keep all item balance rules in data or manager logic
- When adding equipment, update `HeroInstance`, save data, and UI at the same time

## Future Expansion Plans
- Stackable materials
- Crafting
- Rarity tiers
- Set bonuses
- Salvage and dismantle loops
- Addressables-backed item content

