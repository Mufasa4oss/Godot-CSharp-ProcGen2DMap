# Tile Refactor (pre-M2) — Review Checklist (2026-06-23)

Moves resources & buildings from province-bound to **tile-bound**, makes deposits multi-tile with
rate + reserve, collapses to a **single world seed**, and adds colored deposit dots. Edits to
existing files are tagged `//CHANGES 23-Jun-26 tiles//` … `//END CHANGES 23-Jun-26 tiles//` —
search that to find every change (distinct from the earlier `//CHANGES 23-Jun-26//` M0/M1 batch).

---

## 1. New / changed economy classes — `Systems/Economy/`

### Deposit.cs (new)
- [ ] `Good`, `Tiles` (HashSet), `Reserve` (depletes, M2), `Rate`, `Size => Tiles.Count`.
- [ ] `Reserve = Size * ReservePerTile`, `Rate = Size * RatePerTile` — confirm "bigger = more rate **and** reserve" is what you wanted.

### Province.cs (slimmed)
- [ ] Now only `Capital`, `Owner`, `Tiles`. **Removed** `Pop`, `Stockpile`, `Price`, `Buildings`, `Deposits`.
- [ ] Confirm you're OK losing the per-province market for now (returns at M3/M4). This is the "nothing province-bound" change.

### Building.cs (unchanged class, now tile-keyed)
- [ ] Same recipes (Farm/Mine/Workshop); no longer stored on the province.

### Economy.cs (rewritten)
- [ ] `Provinces` (thin), `Deposits` (list), `TileDeposits` (tile→deposit), `TileBuildings` (tile→building).
- [ ] **Tunables** in one block — confirm starting values: `DepositCountTarget=300`, `MinSize=1`, `MaxSize=5`, `OreShare=0.5`, `ReservePerTile=100`, `RatePerTile=1`.
- [ ] Placement = flood-grow clusters over land tiles (8-neighbour), no overlap (`used` set).
- [ ] Auto-places a **Mine** on each ore deposit, a **Farm** on each grain deposit. Workshop = player-placed later (M6).
- [ ] `DeriveSeed()` mixes the world seed → distinct deposit seed (see §3).

### MyGlobalClass.cs
- [ ] `GetSurroundingTiles` made **public** (was private) so Economy can reuse it for flood-grow.

---

## 2. Deposit dots — `Systems/Map/DepositOverlay.cs` (new)
- [ ] `Node2D`, static `_Draw` (built once in `Initialize`, no per-frame cost).
- [ ] **gray** dot = ore, **neon-yellow** dot = grain; radius 10px. Every tile of a multi-tile deposit dotted.
- [ ] Wired in `Main._Ready` right after the trade visualizer.
- [ ] Tune `DotRadius` / colors if they're hard to see at your zoom.

---

## 3. Single seed (search `//CHANGES 23-Jun-26 tiles//`)
- [ ] `Main` now has one `worldSeed` field; set once, pushed to the noise AND passed to `Economy`.
- [ ] Deposits derive a **mixed** sub-seed from it (`DeriveSeed`) — not the identical value, so deposit layout isn't pattern-correlated with terrain, but still reproducible from one seed.
- [ ] Still random each run (no entry UI yet — that comes with save/load).

---

## 4. Verification prints (Main.CreateObjs)
- [ ] `Economy: N provinces, X land tiles`.
- [ ] `tiles check: sum(province tiles)=X (OK/MISMATCH)` — **this proves the 239,878 land-tile count** (sum of province tiles must equal it).
- [ ] `Deposits: D deposits over T tiles (ore=…, grain=…)`.
- [ ] Tile click-inspect (`MapManager`) prints deposit good/size/reserve/rate + building + owner.

---

## 5. Runtime checks (you run it)
- [ ] Boots; the three prints above appear; `tiles check` says **OK**.
- [ ] Dots render — gray ore + neon-yellow grain clusters scattered on land.
- [ ] Click a dotted tile → deposit details; click a bare tile → "no deposit".
- [ ] (Optional) hardcode `worldSeed`, run twice → identical dots = determinism confirmed.
- [ ] No regressions: map/roads render, F5 save still writes.

---

## 6. Known debt / deferred
- [ ] Save/load + `GameContext` slimming — **the agreed next task**.
- [ ] Per-province market (stockpile/price) deferred to M4; pop to M3.
- [ ] Deposit count (300) is sparse on a 512² map — bump `DepositCountTarget` if you want denser ore/grain.

## 7. Next
- [ ] **Save/load working + GameContext cleanup** (next), then **M2 production** (consume inputs→outputs, deplete `Reserve`).
