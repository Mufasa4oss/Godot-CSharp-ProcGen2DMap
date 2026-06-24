# L1 Economy — M0 + M1 Review Checklist (2026-06-23)

How to use this: every edit to an **existing** file is wrapped in
`//CHANGES 23-Jun-26//` … `//END CHANGES 23-Jun-26//` — search that string to jump to each
change. Tick a box once you've read it and agree; cross out / note anything you want changed.
New files in `Systems/Economy/` have no markers (they're entirely new).

---

## 1. New economy classes — `Systems/Economy/`

### Good.cs
- [ ] `enum Good { Grain, Ore, Tools }` — confirm these are the 3 L1 goods you want.

### Building.cs
- [ ] `enum BuildingType { Farm, Mine, Workshop }`.
- [ ] Recipes via `Building.Of()`: Farm `[]→[Grain]` r2, Mine `[]→[Ore]` r2, Workshop `[Ore]→[Tools]` r1.
- [ ] Rates are **placeholders** to balance later (M7) — sanity-check the starting values.
- [ ] `Building` is **data only** — no production logic yet (that's M2). Confirm shape: `Type`, `Inputs[]`, `Outputs[]`, `Rate`.

### Province.cs
- [ ] Fields: `Capital`, `Owner`, `Tiles`, `Pop`, `Stockpile`, `Price`, `Deposits`, `Buildings`.
- [ ] `Stockpile`/`Price`/`Deposits`/`Buildings` auto-init empty; `Capital` + `Tiles` set in ctor.
- [ ] `Owner` is an `int` (0 = you). Confirm `int` vs a future `enum`/nation-id scheme.

### Economy.cs
- [ ] Builds provinces by inverting `provincesDictionary` (tile→capital) in **one pass**.
- [ ] Seeds each province: `Owner=0`, `Pop=100`, every `Good` into `Stockpile`(0)/`Price`(10), a `Farm`.
- [ ] Ore: seeded `RandomNumberGenerator`, `OreDepositChance=0.4` → adds `Ore` deposit + a `Mine`.
- [ ] **Deposits are per-province, not per-tile** (minimal choice). Confirm OK; per-tile is the later upgrade.
- [ ] `OnTick()` increments a counter + loops provinces doing nothing yet — M2 fills this.
- [ ] Tunable constants in one place: `BasePop=100`, `BasePrice=10`, `OreDepositChance=0.4`.

---

## 2. Removed — the 7-raw-resource system
- [ ] Deleted `ResourceManager.cs`, `ResourceItem.cs`, `ResourceItemConverter.cs`, `DictionaryVector2I_ResourceItemConverter.cs` (+ their `.uid`).
- [ ] Confirm you didn't want any of the 7 names kept (Wood/Stone/Iron/Gold/Grain/Livestock/Coal).
- [ ] `Systems/Resources/` is now an **empty folder** — delete it if you like.

---

## 3. Edited existing files (search `//CHANGES 23-Jun-26//`)

### Main.cs — `CreateObjs()`
- [ ] `Economy` built with seed `((FastNoiseLite)noise).Seed`.
- [ ] Prints `Economy: N provinces, X land tiles`.
- [ ] `economy` passed into `TickManager`, `MapManager`, `GameContext`.

### GameContext.cs
- [ ] `resourceManager` + `tilesResources` fields/params replaced by `economy`.
- [ ] Confirm nothing else relied on `tilesResources` (it didn't).

### TickManager.cs (rewritten)
- [ ] ctor is now `(Economy economy)` only.
- [ ] `tickInterval` changed **1/30 → 1.0s** — confirm you want 1-second ticks.
- [ ] `Update()` accumulates `delta`, fires `OnTick()` + heartbeat each second.
- [ ] `currentTick` / `tickInterval` / `tickTimer` kept (save still reads them).

### MapManager.cs
- [ ] `economy` ctor param + `_economy` field added.
- [ ] Click-inspect print is guarded (`_economy != null` + `TryGetValue`).
- [ ] Existing province-highlight behavior left untouched.
- [ ] Added `using System.Linq` (for the buildings join).

### GameData.cs / Serialization.cs / InitSaveAndLoad.cs
- [ ] `capitalsResources` removed from `GameData` (property + ctor param + assignment).
- [ ] Two `ResourceItem` converters removed from `Serialization` options.
- [ ] `InitSaveAndLoad`: resource capture removed; **width bug fixed** (was `mapSizeY` twice → `mapSizeX, mapSizeY`).

### UIManager.cs / TradeNetworkVisualizer.cs
- [ ] Unused `ResourceManager rm` field removed from `UIManager`.
- [ ] Stale comment updated in `TradeNetworkVisualizer` (no functional change — the 7-color array is still there, purely decorative until M5).

---

## 4. Decisions to ratify (your call — easy to change)
- [ ] 1-second tick rate.
- [ ] Per-province (not per-tile) ore deposits.
- [ ] Numbers: 40% ore chance, Pop 100, base price 10, rates 2 / 2 / 1.
- [ ] `Owner` as `int`, single state (`Owner=0`) for all provinces in L1.
- [ ] `Economy` / `Province` / `Building` are **plain C# classes**, not Godot `Node`s — confirm that's fine (means they won't show in the scene tree / can't use `_Process` etc.).

---

## 5. Known debt / deferred (intentionally NOT done today)
- [ ] **Save/load LOAD path is broken & unbuilt** — `initializeLoad()` is empty; `Serialization.Load()` would crash on the `user://` path, the converters, and Vector2I key parsing. Its own future step once the model settles.
- [ ] `GameContext` is a dense god-object with Law-of-Demeter reach-throughs (e.g. `gameContext.voronoiDiagram.placedCords`) — flagged for later slimming.
- [ ] `TradeNetworkVisualizer` packets are still random/decorative — real flows arrive in M5.
- [ ] Province economic state is **not persisted** — it's rebuilt from the seed each run.

---

## 6. Runtime verification — PASSED (you ran it)
- [x] Boots; `Economy: N provinces…` prints.
- [x] Click a province → owner/tiles/pop/deposits/buildings prints.
- [x] `tick N | provinces=N` heartbeat climbs.
- [x] Map / roads / F5-save still work.

---

## 7. Next domino
- [ ] **M2 — production:** in `Economy.OnTick()`, each building consumes its `Inputs` from the
  province `Stockpile` and adds `Outputs × Rate`. Alive check: a province's Grain (then Ore,
  then Tools where a Workshop has Ore) climbs in the console each tick.
