# DATA_PIPELINE.md

## Purpose
Defines the future-proof content pipeline for hero, skill, enemy, floor, item, and balance data.

## Canonical Pipeline
```text
Google Sheets
   ->
JSON Export
   ->
Unity Importer
   ->
ScriptableObjects
   ->
Runtime Database
   ->
Game Systems
```

## Pipeline Goals
- keep design data editable outside Unity
- keep runtime data generated from stable source files
- keep balancing changes easy to review
- keep references deterministic and validation friendly

## Data Classes In Scope
- `HeroData`
- `SkillData`
- `EnemyData`
- `TowerFloor`
- future `ItemData`
- future `EquipmentData`

## Image Reference Handling
- Store image references as stable source keys in the sheet
- The importer resolves keys to imported sprites, atlas entries, or addressable keys
- Never depend on manual drag-and-drop for large content sets
- Keep portrait and icon naming aligned with the asset ID

## Icon Loading
- Current path model can use `Resources` during development
- Future path model should migrate to Addressables or a generated database
- The importer should verify that every referenced icon exists before writing assets

## Prefab Reference Handling
- Store prefab references by stable identifier or addressable key
- Do not store live scene references in imported content
- Use a registry file to map item or effect IDs to prefab addresses

## Validation System
Validate before generating or updating assets:
- duplicate IDs
- missing assets
- invalid star or rarity values
- null enemy pools
- invalid skill links
- broken image or prefab references
- out-of-range balance values

## Balancing Workflow
1. Design updates the sheet
2. Export JSON
3. Importer generates a preview diff
4. Validator reports broken references
5. Unity assets regenerate
6. Designers playtest
7. Balance values are adjusted and locked

## Runtime Database Plan
- Build a cached lookup table from imported assets at startup
- Key everything by stable IDs
- Keep read-only access for UI and gameplay systems
- Prefer an explicit database object over repeated folder scans

## Related Systems
- `Docs/HERO_SYSTEM.md`
- `Docs/SKILL_SYSTEM.md`
- `Docs/COMBAT_SYSTEM.md`
- `Docs/SAVE_SYSTEM.md`
- `Docs/INVENTORY_SYSTEM.md`

## AI Editing Precautions
- Do not add new content types without updating validation rules
- Update both the importer docs and the gameplay docs when a schema changes
- Keep the source-of-truth hierarchy explicit

## Future Expansion Plans
- Google Sheets importer tool
- JSON schema versioning
- localized text export
- icon and portrait CDN support
- addressable prefab registry

