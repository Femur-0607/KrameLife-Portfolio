# Project TownLife Architecture

AI-facing system map. Product truth lives in `ProductSpecs/`; prefer targeted reads.

## Source Order

1. `ProductSpecs/ShopRPG_GDD_v0.14.md` — loop, fantasy, v1 DoD.
2. `ProductSpecs/00_공통규약.md` — ID/SO/events/save/common rules.
3. `ProductSpecs/01_기능목록.md` — v1 feature list and dependencies.
4. `ProductSpecs/08_코어_시스템_세부기능.md` — Core APIs and boundaries.

## Core Loop

```text
procure(dungeon/gather/mine)
-> craft
-> sell/deliver
-> gain gold/fame/affinity/unlocks
-> grow(shop/skills/recipes)
-> sleep/save
-> next day
```

v1 loop: dungeon materials -> craft -> sell for gold -> upgrade shop once.

## Modules

- Core: `IdentifiableSO`, `GameDatabase`, SO event channels, save/load, scene/map transition, shared interfaces.
- Player: movement, facing, interaction input, `IInteractable`.
- Time: 07:00-24:00 day, date, season state, sleep/faint day end.
- Resources: stamina = action budget; HP = combat damage resource.
- Inventory: general inventory, magic-book inventory, item count changes.
- Shop: display, pricing, customer purchase, gold changes.
- Crafting: recipes, craft execution, craft result events.
- DungeonCombat: dungeon generation, real-time combat, monsters, drops, death/return.
- Gathering: mining, gathering, fishing, farming/livestock later.
- CollectionUnlock: codex, delivery, unlocks, tech tree later.
- UI: HUD, time/gold/stamina/HP, interaction prompt.

## Dependency Rules

- Put gameplay logic in C# components.
- Use Scene/Prefab/Inspector for placement, wiring, presentation, and tuning.
- Use ScriptableObject for data sources and event channels.
- Use string IDs for saves and event payloads; avoid runtime object/SO refs in save data.
- Core provides common foundations only; feature modules own their detailed logic.
- Avoid direct module-to-module references; prefer SO event channels or Core interfaces.
- Feature modules own setup validation and normal-flow decisions for their behavior.
- Shared/Core APIs should expose clear `Try...`, `Can...`, or value-object results instead of forcing callers to repeat defensive checks.
- Domain-level empty states should be represented as explicit values, such as `ItemStack.Empty`, not `null`.
- Ask before adding/changing asmdefs.

## Core-First Build Order

1. ID/SO/GameDatabase.
2. SO event channels.
3. General inventory and magic-book inventory.
4. `SaveData` and save/load.
5. Time, stamina, HP.
6. Scene/map transition.
7. Player movement/interaction.
8. HUD and gold.
9. Dungeon procurement, crafting, selling loop.
