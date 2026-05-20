# TODO.md

## Immediate Priorities
1. Add a real `Assets/Resources/Skills` content set and validate skill IDs end to end.
2. Split combat code into a dedicated `Assets/Scripts/Combat/` home if the codebase grows further.
3. Add inventory and equipment runtime models.
4. Add a save version field and migration layer.
5. Replace ad hoc content lookup with cached registries or Addressables-ready abstractions.

## Planned Systems
- Daily rewards and login flow
- Equipment, item inventory, and salvage
- Addressables migration
- Content importer from Google Sheets or JSON
- More tower floors and enemy wave variation
- Skill trees and passive/reaction expansion
- Hero duplication, rarity, and promotion tuning

## Documentation Maintenance
- Add a system doc whenever a new gameplay system appears
- Update the matching system doc when public APIs change
- Add a short note to `CHANGELOG.md` whenever a task lands

